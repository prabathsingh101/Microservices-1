using Suppliers.Infrastructure.Persistence;
using Suppliers.Application.Features.Suppliers.Handlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Infrastructure.Repositories;
using Microsoft.IdentityModel.Tokens;
using Suppliers.API.Middlewares;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Scalar.AspNetCore;
using System.Security.Claims;
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

builder.Services.AddDbContext<SupplierDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SuppliersDb"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        })
    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

// MediatR register karna (Application Layer ke handlers ko dhundne ke liye)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateSupplierHandler).Assembly));

// Repository Inject karna
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IFinanceRepository, FinanceRepository>();
builder.Services.AddScoped<ICurrentUserService, Suppliers.API.Services.CurrentUserService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(o => o.AddPolicy("AllowAngularDev", p =>
{
    p.AllowAnyHeader()
     .AllowAnyMethod()
     .AllowAnyOrigin()
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.UseCors("AllowAngularDev");
app.UseMiddleware<ExceptionMiddleware>();
app.MapHealthChecks("/health");

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
        var context = services.GetRequiredService<SupplierDbContext>();
        if (context.Database.CanConnect())
        {
            Log.Information("Supplier Database connected.");
            // context.Database.Migrate(); // Disabled
        }
        else
        {
            Log.Warning("Supplier Database not found.");
            // context.Database.EnsureCreated(); // Disabled
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the Supplier database.");
    }
}
*/

app.MapControllers();

try
{
    Log.Information("Starting Suppliers.API Service...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Suppliers.API Service failed to start");
}
finally
{
    Log.CloseAndFlush();
}
