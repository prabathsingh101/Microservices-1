using Microsoft.AspNetCore.Mvc;
using Inventory.API.Helper;
using Inventory.Application;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddApplication();

// Infrastructure (DB)
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpClient("CustomerService", client =>
{
    var url = builder.Configuration["ServiceUrls:CustomerApi"] ?? "https://localhost:7173/";
    client.BaseAddress = new Uri(url);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient("SupplierServiceClient", client =>
{
    var url = builder.Configuration["ServiceUrls:SupplierApi"] ?? "https://localhost:7224/";
    client.BaseAddress = new Uri(url);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient("CompanyService", client =>
{
    var url = builder.Configuration["ServiceUrls:CompanyApi"] ?? "https://localhost:7065/";
    client.BaseAddress = new Uri(url);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddScoped<Inventory.Application.Clients.ICompanyClient, Inventory.Application.Clients.CompanyClient>();

builder.Services.AddControllers()
    .AddJsonOptions(options => {
        // Taaki dates aur complex objects sahi se serialize hon
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new StringOrNumberConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState);
            var errors = string.Join("; ", problemDetails.Errors.SelectMany(x => x.Value));
            
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Inventory.API.Middleware.ExceptionHandlingMiddleware>>();
            logger.LogError("Model validation failed on {Path}: {Errors}", context.HttpContext.Request.Path, errors);
            
            return new BadRequestObjectResult(problemDetails);
        };
    });

// Add services to the container.
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

PDFHelper.CustomAssemblyLoadContext.LoadNativeLibrary();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor(); // Required for IHttpContextAccessor

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.UseCors("AllowAngularDev");

app.UseMiddleware<Inventory.API.Middleware.ExceptionHandlingMiddleware>();

// app.UseHttpsRedirection();
app.MapHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();

/* 
// Safe Database Initialization - Disabled for Manual Schema Management
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<InventoryDbContext>();
        if (context.Database.CanConnect())
        {
            Log.Information("Inventory Database connected.");
            // context.Database.Migrate(); // Disabled
        }
        else
        {
            Log.Warning("Inventory Database not found.");
            // context.Database.EnsureCreated(); // Disabled
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the Inventory database. App will still start.");
    }
}
*/

app.MapControllers();

try
{
    Log.Information("Starting Inventory.API Service...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Inventory.API Service failed to start");
}
finally
{
    Log.CloseAndFlush();
}
