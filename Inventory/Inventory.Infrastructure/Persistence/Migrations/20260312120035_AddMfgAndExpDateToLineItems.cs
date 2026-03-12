using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMfgAndExpDateToLineItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpDate",
                table: "SaleReturnItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MfgDate",
                table: "SaleReturnItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpDate",
                table: "SaleOrderItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MfgDate",
                table: "SaleOrderItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpDate",
                table: "PurchaseReturnItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MfgDate",
                table: "PurchaseReturnItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpDate",
                table: "PurchaseOrderItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MfgDate",
                table: "PurchaseOrderItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpDate",
                table: "InventoryTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MfgDate",
                table: "InventoryTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpDate",
                table: "GRNDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MfgDate",
                table: "GRNDetails",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpDate",
                table: "SaleReturnItems");

            migrationBuilder.DropColumn(
                name: "MfgDate",
                table: "SaleReturnItems");

            migrationBuilder.DropColumn(
                name: "ExpDate",
                table: "SaleOrderItems");

            migrationBuilder.DropColumn(
                name: "MfgDate",
                table: "SaleOrderItems");

            migrationBuilder.DropColumn(
                name: "ExpDate",
                table: "PurchaseReturnItems");

            migrationBuilder.DropColumn(
                name: "MfgDate",
                table: "PurchaseReturnItems");

            migrationBuilder.DropColumn(
                name: "ExpDate",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "MfgDate",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "ExpDate",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "MfgDate",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "ExpDate",
                table: "GRNDetails");

            migrationBuilder.DropColumn(
                name: "MfgDate",
                table: "GRNDetails");
        }
    }
}
