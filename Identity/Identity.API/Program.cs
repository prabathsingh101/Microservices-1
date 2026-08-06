using Microsoft.AspNetCore.Mvc;
using FluentValidation.AspNetCore;
using Identity.API.Extensions;
using Identity.API.Helpers;
using Identity.Application.Interfaces;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Identity.API.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Claims;
using Serilog;
using Identity.Application.Services;
using Identity.Domain.Users;
using Identity.Domain;
using Identity.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
try
{
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();
    builder.Host.UseSerilog();
}
catch (Exception ex)
{
    Console.WriteLine($"Serilog initialization warning: {ex.Message}");
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateLogger();
}

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new StringOrNumberConverter());
    })
    .AddFluentValidation()
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

builder.Services.AddHttpClient();

builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();

builder.Services.AddCors(o => o.AddPolicy("AllowAngularDev", p =>
{
    p.AllowAnyHeader()
     .AllowAnyMethod()
     .SetIsOriginAllowed(origin => true)
     .AllowCredentials()
     .WithExposedHeaders("Content-Disposition");
}));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],            
            IssuerSigningKey = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(jwt["Key"]!)),
            ClockSkew = TimeSpan.Zero,
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "JWT Authentication failed for {Path}", context.HttpContext.Request.Path);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT Challenge triggered for {Path}. Error: {Error}, ErrorDescription: {ErrorDescription}", 
                    context.HttpContext.Request.Path, context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<IdentityDbContext>();
                var userIdString = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var tokenSessionId = context.Principal?.FindFirst("SessionId")?.Value;
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("OnTokenValidated called for {Path}. UserId: {UserId}, SessionId: {SessionId}", 
                    context.HttpContext.Request.Path, userIdString, tokenSessionId);

                if (Guid.TryParse(userIdString, out var userId))
                {
                    var user = await db.Users
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(u => u.Id == userId, context.HttpContext.RequestAborted);

                    if (user == null)
                    {
                        logger.LogWarning("JWT Token validation failed: User {UserId} not found in database.", userId);
                        context.Fail("User not found.");
                    }
                    else if (user.CurrentSessionId != tokenSessionId)
                    {
                        logger.LogWarning("JWT Token validation failed: SessionId mismatch. DB: {DbSessionId}, Token: {TokenSessionId}", 
                            user.CurrentSessionId, tokenSessionId);
                        context.Fail("Session has expired or logged in from another device.");
                    }
                    else
                    {
                        logger.LogInformation("JWT Token validation succeeded for User: {UserId}", userId);
                    }
                }
            },
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                
                logger.LogInformation("OnMessageReceived called for {Path}. Query token present: {HasToken}", 
                    path, !string.IsNullOrEmpty(accessToken));

                if (!string.IsNullOrEmpty(accessToken) && path.Value?.StartsWith("/hubs") == true)
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();

var signalRBuilder = builder.Services.AddSignalR();
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    signalRBuilder.AddStackExchangeRedis(redisConnection);
}

var app = builder.Build();

// Middleware Pipeline
app.UseCors("AllowAngularDev");

var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/api/uploads"
});
app.UseStaticFiles();

// Global Exception Handler - Catch everything!
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();

// Safe Database Initialization / Index Management
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<IdentityDbContext>();
        if (context.Database.CanConnect())
        {
            Log.Information("Identity Database connected. Ensuring deprecated unique index on UserName is removed...");
            context.Database.ExecuteSqlRaw("DROP INDEX IF EXISTS [IX_Users_UserName_CompanyId] ON [Users];");
            Log.Information("Deprecated index check completed successfully.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while managing database indexes.");
    }
}

app.MapControllers();
app.MapHub<Identity.API.Hubs.AuthHub>("/hubs/auth");
app.MapHub<Identity.API.Hubs.ChatHub>("/hubs/chat");

try
{
    Log.Information("Starting Identity.API Service...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Identity.API Service failed to start");
}
finally
{
    Log.CloseAndFlush();
}
