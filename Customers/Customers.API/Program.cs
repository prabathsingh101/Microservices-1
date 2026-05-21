using Microsoft.AspNetCore.Mvc;
using Customers.Application;
using Customers.Infrastructure;
using Customers.Infrastructure.Persistence;
using Customers.API.Middlewares;
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

builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddApplication();

// Infrastructure (DB)
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<Customers.Application.Common.Interfaces.ICurrentUserService, Customers.API.Services.CurrentUserService>();

builder.Services.AddControllers()
    .AddJsonOptions(options => {
        // Taaki dates aur complex objects sahi se serialize hon
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
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

builder.Services.AddCors(o => o.AddPolicy("AllowAngularDev", p =>
{
    p.AllowAnyHeader()
     .AllowAnyMethod()
     .SetIsOriginAllowed(origin => true)
     .AllowCredentials()
     .WithExposedHeaders("Content-Disposition"); // <-- important
}));

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
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.MapHealthChecks("/health");
app.UseCors("AllowAngularDev");
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

/* 
// Safe Database Initialization - Disabled for Manual Schema Management
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CustomerDbContext>();
        if (context.Database.CanConnect())
        {
            Log.Information("Customer Database connected.");
            // context.Database.Migrate(); // Disabled
        }
        else
        {
            Log.Warning("Customer Database not found.");
            // context.Database.EnsureCreated(); // Disabled
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the Customer database.");
    }
}
*/

app.MapControllers();

try
{
    Log.Information("Starting Customers.API Service...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Customers.API Service failed to start");
}
finally
{
    Log.CloseAndFlush();
}
