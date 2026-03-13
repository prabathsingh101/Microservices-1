using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Suppliers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedBySuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // These tables may already exist in the database (e.g., when __EFMigrationsHistory was reset).
            // Guard against failure by creating only if missing.
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[SupplierLedgers]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SupplierLedgers](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [SupplierId] INT NOT NULL,
        [TransactionType] NVARCHAR(50) NOT NULL,
        [ReferenceId] NVARCHAR(MAX) NOT NULL,
        [Debit] DECIMAL(18,2) NOT NULL,
        [Credit] DECIMAL(18,2) NOT NULL,
        [Balance] DECIMAL(18,2) NOT NULL,
        [TransactionDate] DATETIME2 NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [CreatedDate] DATETIME2 NOT NULL,
        CONSTRAINT [PK_SupplierLedgers] PRIMARY KEY ([Id])
    );
END");

            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[SupplierPayments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SupplierPayments](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [SupplierId] INT NOT NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [PaymentDate] DATETIME2 NOT NULL,
        [PaymentMode] NVARCHAR(50) NOT NULL,
        [ReferenceNumber] NVARCHAR(MAX) NULL,
        [Remarks] NVARCHAR(MAX) NULL,
        [CreatedBy] NVARCHAR(MAX) NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL,
        CONSTRAINT [PK_SupplierPayments] PRIMARY KEY ([Id])
    );
END");

            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Suppliers]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Suppliers](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Phone] NVARCHAR(15) NOT NULL,
        [GstIn] NVARCHAR(MAX) NULL,
        [Address] NVARCHAR(MAX) NULL,
        [Email] NVARCHAR(MAX) NULL,
        [DefaultPriceListId] UNIQUEIDENTIFIER NULL,
        [IsActive] BIT NOT NULL,
        [CreatedBy] NVARCHAR(MAX) NULL,
        [CreatedDate] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(MAX) NULL,
        [UpdatedDate] DATETIME2 NULL,
        CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
    );
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[SupplierLedgers]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[SupplierLedgers];
END");

            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[SupplierPayments]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[SupplierPayments];
END");

            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Suppliers]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Suppliers];
END");
        }
    }
}
