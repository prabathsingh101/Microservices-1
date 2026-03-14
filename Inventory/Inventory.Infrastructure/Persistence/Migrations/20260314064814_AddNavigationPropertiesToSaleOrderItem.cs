using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationPropertiesToSaleOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SaleOrderItems_RackId",
                table: "SaleOrderItems",
                column: "RackId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrderItems_WarehouseId",
                table: "SaleOrderItems",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleOrderItems_Racks_RackId",
                table: "SaleOrderItems",
                column: "RackId",
                principalTable: "Racks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleOrderItems_Warehouses_WarehouseId",
                table: "SaleOrderItems",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleOrderItems_Racks_RackId",
                table: "SaleOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleOrderItems_Warehouses_WarehouseId",
                table: "SaleOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_SaleOrderItems_RackId",
                table: "SaleOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_SaleOrderItems_WarehouseId",
                table: "SaleOrderItems");
        }
    }
}
