using Company.Infrastructure;
using Company.Application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Services Configuration ---

// Redundant calls ko merge kiya: Controllers + JSON Options
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        // Frontend (Angular) ke liye CamelCase naming policy zaroori hai
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles; // Circular reference se bachne ke liye
    });

// OpenAPI/Scalar setup for documentation
builder.Services.AddOpenApi();

// Custom Dependency Injections
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

app.UseHttpsRedirection();

// Security Middleware
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CompanyDbContext>();
    db.Database.Migrate(); // applies migrations, creates DB if not exists
}

app.MapControllers();

app.Run();