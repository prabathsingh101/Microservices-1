using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Suppliers.Application.Interfaces;
using Suppliers.Infrastructure.Repositories;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<SupplierDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SupplierDb"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

// MediatR register karna (Application Layer ke handlers ko dhundne ke liye)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateSupplierHandler).Assembly));

// Repository Inject karna
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IFinanceRepository, FinanceRepository>();

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
app.MapHealthChecks("/health");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SupplierDbContext>();
    db.Database.Migrate(); // applies migrations, creates DB if not exists
}

app.MapControllers();

app.Run();
