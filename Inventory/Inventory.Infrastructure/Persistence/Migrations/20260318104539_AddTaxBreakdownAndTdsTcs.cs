using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxBreakdownAndTdsTcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CgstAmount",
                table: "SaleOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IgstAmount",
                table: "SaleOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SgstAmount",
                table: "SaleOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxType",
                table: "SaleOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TcsAmount",
                table: "SaleOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TcsPercent",
                table: "SaleOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TdsAmount",
                table: "SaleOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TdsPercent",
                table: "SaleOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CgstAmount",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IgstAmount",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SgstAmount",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxType",
                table: "PurchaseOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TcsAmount",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TcsPercent",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TdsAmount",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TdsPercent",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CgstAmount",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "IgstAmount",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "SgstAmount",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "TaxType",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "TcsAmount",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "TcsPercent",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "TdsAmount",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "TdsPercent",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "CgstAmount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "IgstAmount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SgstAmount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TaxType",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TcsAmount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TcsPercent",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TdsAmount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TdsPercent",
                table: "PurchaseOrders");
        }
    }
}
