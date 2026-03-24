using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToPurchaseReturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PurchaseReturns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "PurchaseReturns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "PurchaseReturns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "PurchaseReturns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PurchaseReturnItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "PurchaseReturnItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "PurchaseReturnItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "PurchaseReturnItems",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PurchaseReturnItems");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "PurchaseReturnItems");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "PurchaseReturnItems");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "PurchaseReturnItems");
        }
    }
}
