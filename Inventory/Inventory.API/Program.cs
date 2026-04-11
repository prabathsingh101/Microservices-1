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
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
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

app.UseHttpsRedirection();
app.MapHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.Migrate(); // applies migrations, creates DB if not exists
}

app.MapControllers();

app.Run();
