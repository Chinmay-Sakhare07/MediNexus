using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Dapper;
using MySqlConnector;
using HospitalManagement.API.Json;
using HospitalManagement.API.Middleware;
using HospitalManagement.API.Repositories;
using HospitalManagement.API.Repositories.Interfaces;
using HospitalManagement.API.Services;
using HospitalManagement.API.Validation;

var builder = WebApplication.CreateBuilder(args);

// Container hosts (Render, Cloud Run, etc.) inject the listen port via $PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Render terminates TLS at its proxy; honor X-Forwarded-* so RemoteIpAddress
// is the real client (rate limiting partitions on it) and scheme is https.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Controllers + validation filter + D6 UTC JSON convention at the boundary.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeConverter());
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();

// Swagger (Development only) with a Bearer scheme so protected endpoints are testable.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "MediNexus API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token from POST /api/auth/login"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Register repositories & services
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IBillingRepository, BillingRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IInsuranceRepository, InsuranceRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();

// ---- Authentication: JWT bearer -------------------------------------------
var jwtSecret = JwtConfig.ResolveSecret(builder.Configuration, builder.Environment);
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? JwtConfig.DefaultIssuer;
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? JwtConfig.DefaultAudience;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>().CreateLogger("Auth");
                logger.LogWarning("TOKEN_REJECTED reason={Reason} path={Path}",
                    ctx.Exception.GetType().Name, ctx.HttpContext.Request.Path);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ---- Rate limiting: protect the login endpoint -----------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.OnRejected = (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>().CreateLogger("Auth");
        logger.LogWarning("LOGIN_RATE_LIMITED ip={Ip}",
            context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        return ValueTask.CompletedTask;
    };
});

// CORS: extra origins via env var Cors__AllowedOrigins = "https://a.com,https://b.com"
var defaultOrigins = new[]
{
    "http://localhost:5173",
    "http://localhost:3000",
    "http://localhost:5174",
    "https://medinexushealth.netlify.app"
};
var extraOrigins = builder.Configuration["Cors:AllowedOrigins"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? Array.Empty<string>();
var allowedOrigins = defaultOrigins.Concat(extraOrigins).Distinct().ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

var app = builder.Build();

// Pipeline (order matters): exceptions outermost -> forwarded headers ->
// CORS -> rate limiter -> authN -> authZ -> endpoints.
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseForwardedHeaders();
app.UseCors("AllowReactApp");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.MapGet("/", () => "Group Six Multispeciality Hospital API is running!");

// Health check: verifies the API is up AND the database answers.
app.MapGet("/health", async (IConfiguration config, ILogger<Program> logger) =>
{
    var connectionString = config.GetConnectionString("HospitalDb");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Json(
            new { status = "degraded", db = "not configured" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await connection.ExecuteScalarAsync<int>("SELECT 1;");
        return Results.Ok(new { status = "ok", db = "up", timeUtc = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Health check failed to reach the database");
        return Results.Json(
            new { status = "degraded", db = "unreachable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();
