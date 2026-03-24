using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRackToSaleReturnItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RackId",
                table: "SaleReturnItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "SaleReturnItems",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RackId",
                table: "SaleReturnItems");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "SaleReturnItems");
        }
    }
}
