using HospitalManagement.API.Repositories;
using HospitalManagement.API.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

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

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
    "http://localhost:5173",
    "http://localhost:3000",
    "http://localhost:5174",
    "https://medinexushealth.netlify.app"
    )
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

app.Run();