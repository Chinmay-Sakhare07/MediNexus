using HospitalManagement.API.Repositories;
using HospitalManagement.API.Repositories.Interfaces;
using Dapper;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// Container hosts (Render, Cloud Run, etc.) inject the listen port via $PORT.
// Locally this is unset, so launchSettings.json still applies.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register repositories
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IBillingRepository, BillingRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IInsuranceRepository, InsuranceRepository>();

// Configure CORS.
// Extra origins can be added without a code change via the env var
//   Cors__AllowedOrigins = "https://foo.com,https://bar.com"
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

// Configure the HTTP request pipeline
app.UseCors("AllowReactApp");
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "Group Six Multispeciality Hospital API is running!");

// Health check: verifies the API is up AND the database answers.
// One request here keeps Render awake and counts as activity on Aiven,
// so the scheduled GitHub Action only needs to hit this single URL.
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
        // Log the real reason, but never leak connection details to callers.
        logger.LogError(ex, "Health check failed to reach the database");
        return Results.Json(
            new { status = "degraded", db = "unreachable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();
