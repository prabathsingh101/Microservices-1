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
using MassTransit;

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

// MassTransit & RabbitMQ Setup
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<Inventory.API.Consumers.TestEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.ReceiveEndpoint("test-event-queue", e =>
        {
            e.ConfigureConsumer<Inventory.API.Consumers.TestEventConsumer>(context);
        });
    });
});

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
     .SetIsOriginAllowed(origin => true)                    
     .AllowCredentials()
     .WithExposedHeaders("Content-Disposition"); // <-- important
}));

builder.Services.AddSignalR();

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

// Safe Database Schema migration for Home Delivery and Expense RCM columns
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    try
    {
        var sql = @"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SaleOrders]') AND name = N'DeliveryType')
        BEGIN
            ALTER TABLE [dbo].[SaleOrders] ADD [DeliveryType] VARCHAR(50) NULL;
            ALTER TABLE [dbo].[SaleOrders] ADD [DeliveryAddress] NVARCHAR(500) NULL;
            ALTER TABLE [dbo].[SaleOrders] ADD [DeliverySlot] VARCHAR(100) NULL;
            ALTER TABLE [dbo].[SaleOrders] ADD [DeliveryBoyId] VARCHAR(100) NULL;
            ALTER TABLE [dbo].[SaleOrders] ADD [DeliveryBoyName] NVARCHAR(200) NULL;
            ALTER TABLE [dbo].[SaleOrders] ADD [DeliveryCharges] DECIMAL(18,2) NOT NULL DEFAULT 0.00;
            ALTER TABLE [dbo].[SaleOrders] ADD [DeliveryStatus] VARCHAR(50) NULL;
            ALTER TABLE [dbo].[SaleOrders] ADD [CodCollectedAmount] DECIMAL(18,2) NULL;
            ALTER TABLE [dbo].[SaleOrders] ADD [CodPaymentMode] VARCHAR(50) NULL;
            ALTER TABLE [dbo].[SaleOrders] ADD [CashSettled] BIT NOT NULL DEFAULT 0;
            ALTER TABLE [dbo].[SaleOrders] ADD [CashSettledDate] DATETIME2 NULL;
            ALTER TABLE [dbo].[SaleOrders] ADD [CashSettledBy] NVARCHAR(200) NULL;
        END";
        
        context.Database.ExecuteSqlRaw(sql);
        Log.Information("Home Delivery DB Schema verified/migrated successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to run Home Delivery DB Schema migration. It might already exist or DB is offline.");
    }

    try
    {
        var sqlConfigs = @"
        IF OBJECT_ID(N'[dbo].[Configurations]', 'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[Configurations] (
                [Id] INT IDENTITY(1,1) NOT NULL,
                [ConfigKey] NVARCHAR(250) NOT NULL,
                [ConfigValue] NVARCHAR(MAX) NOT NULL,
                [IsActive] BIT NOT NULL DEFAULT 1,
                CONSTRAINT [PK_Configurations] PRIMARY KEY CLUSTERED ([Id] ASC)
            );
        END";
        context.Database.ExecuteSqlRaw(sqlConfigs);
        Log.Information("Configurations DB table verified/created successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to run Configurations DB table migration.");
    }

    try
    {
        var sqlVariants = @"
        IF OBJECT_ID(N'[dbo].[ProductVariants]', 'U') IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariants]') AND name = N'CreatedBy')
                ALTER TABLE [dbo].[ProductVariants] ADD [CreatedBy] NVARCHAR(MAX) NULL;

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariants]') AND name = N'CreatedOn')
                ALTER TABLE [dbo].[ProductVariants] ADD [CreatedOn] DATETIME2 NULL;

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariants]') AND name = N'ModifiedBy')
                ALTER TABLE [dbo].[ProductVariants] ADD [ModifiedBy] NVARCHAR(MAX) NULL;

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariants]') AND name = N'ModifiedOn')
                ALTER TABLE [dbo].[ProductVariants] ADD [ModifiedOn] DATETIME2 NULL;
        END";
        
        context.Database.ExecuteSqlRaw(sqlVariants);
        Log.Information("ProductVariants DB Schema verified/migrated successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to run ProductVariants DB Schema migration.");
    }

    try
    {
        var sqlPurchaseOrderItems = @"
        IF OBJECT_ID(N'[dbo].[PurchaseOrderItems]', 'U') IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseOrderItems]') AND name = N'ProductVariantId')
                ALTER TABLE [dbo].[PurchaseOrderItems] ADD [ProductVariantId] UNIQUEIDENTIFIER NULL;
        END";
        
        context.Database.ExecuteSqlRaw(sqlPurchaseOrderItems);
        Log.Information("PurchaseOrderItems DB Schema verified/migrated successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to run PurchaseOrderItems DB Schema migration.");
    }

    try
    {
        var sqlSaleOrderItems = @"
        IF OBJECT_ID(N'[dbo].[SaleOrderItems]', 'U') IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SaleOrderItems]') AND name = N'ProductVariantId')
                ALTER TABLE [dbo].[SaleOrderItems] ADD [ProductVariantId] UNIQUEIDENTIFIER NULL;
        END";
        
        context.Database.ExecuteSqlRaw(sqlSaleOrderItems);
        Log.Information("SaleOrderItems DB Schema verified/migrated successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to run SaleOrderItems DB Schema migration.");
    }

    try
    {
        var sqlInventoryTx = @"
        IF OBJECT_ID(N'[dbo].[InventoryTransactions]', 'U') IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InventoryTransactions]') AND name = N'ProductVariantId')
                ALTER TABLE [dbo].[InventoryTransactions] ADD [ProductVariantId] UNIQUEIDENTIFIER NULL;
        END";
        context.Database.ExecuteSqlRaw(sqlInventoryTx);
        Log.Information("InventoryTransactions DB Schema verified/migrated successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to run InventoryTransactions DB Schema migration.");
    }

    try
    {
        var sqlInvoiceAndReturnItems = @"
        IF OBJECT_ID(N'[dbo].[SalesInvoiceItems]', 'U') IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalesInvoiceItems]') AND name = N'ProductVariantId')
                ALTER TABLE [dbo].[SalesInvoiceItems] ADD [ProductVariantId] UNIQUEIDENTIFIER NULL;
        END
        IF OBJECT_ID(N'[dbo].[SaleReturnItems]', 'U') IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SaleReturnItems]') AND name = N'ProductVariantId')
                ALTER TABLE [dbo].[SaleReturnItems] ADD [ProductVariantId] UNIQUEIDENTIFIER NULL;
        END
        IF OBJECT_ID(N'[dbo].[SaleExchangeItems]', 'U') IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SaleExchangeItems]') AND name = N'ProductVariantId')
                ALTER TABLE [dbo].[SaleExchangeItems] ADD [ProductVariantId] UNIQUEIDENTIFIER NULL;
        END
        IF OBJECT_ID(N'[dbo].[DeliveryChallanItems]', 'U') IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DeliveryChallanItems]') AND name = N'ProductVariantId')
                ALTER TABLE [dbo].[DeliveryChallanItems] ADD [ProductVariantId] UNIQUEIDENTIFIER NULL;
        END
        IF OBJECT_ID(N'[dbo].[PurchaseReturnItems]', 'U') IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseReturnItems]') AND name = N'ProductVariantId')
                ALTER TABLE [dbo].[PurchaseReturnItems] ADD [ProductVariantId] UNIQUEIDENTIFIER NULL;
        END";
        
        context.Database.ExecuteSqlRaw(sqlInvoiceAndReturnItems);
        Log.Information("SalesInvoiceItems/SaleReturnItems/SaleExchangeItems/DeliveryChallanItems/PurchaseReturnItems variant schema verified/migrated successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to run SalesInvoiceItems/SaleReturnItems/SaleExchangeItems/DeliveryChallanItems/PurchaseReturnItems schema migration.");
    }

    try
    {
        var sqlRcm = @"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExpenseEntries]') AND name = N'IsRcm')
            ALTER TABLE [dbo].[ExpenseEntries] ADD [IsRcm] BIT NOT NULL DEFAULT 0;

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExpenseEntries]') AND name = N'RcmGstRate')
            ALTER TABLE [dbo].[ExpenseEntries] ADD [RcmGstRate] DECIMAL(18,2) NULL;

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExpenseEntries]') AND name = N'RcmTaxableValue')
            ALTER TABLE [dbo].[ExpenseEntries] ADD [RcmTaxableValue] DECIMAL(18,2) NULL;

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExpenseEntries]') AND name = N'RcmTaxAmount')
            ALTER TABLE [dbo].[ExpenseEntries] ADD [RcmTaxAmount] DECIMAL(18,2) NULL;

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExpenseEntries]') AND name = N'RcmCgstAmount')
            ALTER TABLE [dbo].[ExpenseEntries] ADD [RcmCgstAmount] DECIMAL(18,2) NULL;

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExpenseEntries]') AND name = N'RcmSgstAmount')
            ALTER TABLE [dbo].[ExpenseEntries] ADD [RcmSgstAmount] DECIMAL(18,2) NULL;

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExpenseEntries]') AND name = N'RcmIgstAmount')
            ALTER TABLE [dbo].[ExpenseEntries] ADD [RcmIgstAmount] DECIMAL(18,2) NULL;

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExpenseEntries]') AND name = N'RcmPaid')
            ALTER TABLE [dbo].[ExpenseEntries] ADD [RcmPaid] BIT NOT NULL DEFAULT 0;

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExpenseEntries]') AND name = N'RcmPaidDate')
            ALTER TABLE [dbo].[ExpenseEntries] ADD [RcmPaidDate] DATETIME2 NULL;

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExpenseEntries]') AND name = N'SupplierName')
            ALTER TABLE [dbo].[ExpenseEntries] ADD [SupplierName] NVARCHAR(200) NULL;

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExpenseEntries]') AND name = N'SupplierGstin')
            ALTER TABLE [dbo].[ExpenseEntries] ADD [SupplierGstin] VARCHAR(50) NULL;";
        
        context.Database.ExecuteSqlRaw(sqlRcm);
        Log.Information("Expense RCM DB Schema verified/migrated successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to run Expense RCM DB Schema migration. It might already exist or DB is offline.");
    }
}

app.MapControllers();
app.MapHub<Inventory.API.Hubs.DeliveryHub>("/api/hubs/delivery");


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
