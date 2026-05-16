using Microsoft.AspNetCore.Mvc;
using employeepayroll.Infrastructure;
using employeepayroll.Infrastructure.Persistence;
using employeepayroll.Application;
using employeepayroll.API.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Claims;
using System.Text.Json;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// --- Services Configuration ---

// Redundant calls ko merge kiya: Controllers + JSON Options
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        // Frontend (Angular) ke liye CamelCase naming policy zaroori hai
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles; // Circular reference se bachne ke liye
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState);
            var errors = string.Join("; ", problemDetails.Errors.SelectMany(x => x.Value));
            
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ExceptionMiddleware>>();
            logger.LogError("Model validation failed on {Path}: {Errors}", context.HttpContext.Request.Path, errors);
            
            return new BadRequestObjectResult(problemDetails);
        };
    });

// OpenAPI/Scalar setup for documentation
builder.Services.AddOpenApi();

// Custom Dependency Injections
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<employeepayroll.Application.Common.Interfaces.ICurrentUserService, employeepayroll.API.Services.CurrentUserService>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// CORS Policy: Angular dev environment ke liye configuration
builder.Services.AddCors(o => o.AddPolicy("AllowAngularDev", p =>
{
    p.AllowAnyHeader()
     .AllowAnyMethod()
     .AllowAnyOrigin()
     .WithExposedHeaders("Content-Disposition"); // File download/upload ke liye important
}));

// JWT Authentication setup
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

var app = builder.Build();

// --- Middleware Pipeline Configuration ---

// Documentation setup
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Static Files: wwwroot/uploads/logos access karne ke liye sabse pehle
app.UseStaticFiles();

// CORS hamesha Auth se pehle hona chahiye
app.MapHealthChecks("/health");
app.UseCors("AllowAngularDev");
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

// Security Middleware
app.UseAuthentication();
app.UseAuthorization();

/* 
// Safe Database Initialization - Disabled for Manual Schema Management
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<EmployeePayrollDBContext>();
        if (context.Database.CanConnect())
        {
            Log.Information("EmployeePayroll Database connected.");
            // context.Database.Migrate(); // Disabled
        }
        else
        {
            Log.Warning("EmployeePayroll Database not found.");
            // context.Database.EnsureCreated(); // Disabled
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the EmployeePayroll database.");
    }
}
*/

app.MapControllers();

try
{
    Log.Information("Starting EmployeePayroll.API Service...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EmployeePayroll.API Service failed to start");
}
finally
{
    Log.CloseAndFlush();
}
