using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalMTIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "SaleReturnItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "SaleReturnHeaders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "SaleOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "SaleOrderItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "PurchaseReturns",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "PurchaseReturnItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "PurchaseOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "PurchaseOrderItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DefaultGst",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "PriceListItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "InventoryTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "GRNHeaders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "GRNDetails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "GatePasses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "ExpenseEntries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "ExpenseCategories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "AppNotifications",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MT_Warehouse_CompanyId",
                table: "Warehouses",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_UnitMaster_CompanyId",
                table: "Units",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_Subcategory_CompanyId",
                table: "Subcategories",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_SaleReturnItem_CompanyId",
                table: "SaleReturnItems",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_SaleReturnHeader_CompanyId",
                table: "SaleReturnHeaders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_SaleOrder_CompanyId",
                table: "SaleOrders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_SaleOrderItem_CompanyId",
                table: "SaleOrderItems",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_Rack_CompanyId",
                table: "Racks",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_PurchaseReturn_CompanyId",
                table: "PurchaseReturns",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_PurchaseReturnItem_CompanyId",
                table: "PurchaseReturnItems",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_PurchaseOrder_CompanyId",
                table: "PurchaseOrders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_PurchaseOrderItem_CompanyId",
                table: "PurchaseOrderItems",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_Product_CompanyId",
                table: "Products",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_PriceList_CompanyId",
                table: "PriceLists",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_PriceListItem_CompanyId",
                table: "PriceListItems",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_InventoryTransaction_CompanyId",
                table: "InventoryTransactions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_GRNHeader_CompanyId",
                table: "GRNHeaders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_GRNDetail_CompanyId",
                table: "GRNDetails",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_GatePass_CompanyId",
                table: "GatePasses",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_ExpenseEntry_CompanyId",
                table: "ExpenseEntries",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_ExpenseCategory_CompanyId",
                table: "ExpenseCategories",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_Category_CompanyId",
                table: "Categories",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MT_AppNotification_CompanyId",
                table: "AppNotifications",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MT_Warehouse_CompanyId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_MT_UnitMaster_CompanyId",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_MT_Subcategory_CompanyId",
                table: "Subcategories");

            migrationBuilder.DropIndex(
                name: "IX_MT_SaleReturnItem_CompanyId",
                table: "SaleReturnItems");

            migrationBuilder.DropIndex(
                name: "IX_MT_SaleReturnHeader_CompanyId",
                table: "SaleReturnHeaders");

            migrationBuilder.DropIndex(
                name: "IX_MT_SaleOrder_CompanyId",
                table: "SaleOrders");

            migrationBuilder.DropIndex(
                name: "IX_MT_SaleOrderItem_CompanyId",
                table: "SaleOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_MT_Rack_CompanyId",
                table: "Racks");

            migrationBuilder.DropIndex(
                name: "IX_MT_PurchaseReturn_CompanyId",
                table: "PurchaseReturns");

            migrationBuilder.DropIndex(
                name: "IX_MT_PurchaseReturnItem_CompanyId",
                table: "PurchaseReturnItems");

            migrationBuilder.DropIndex(
                name: "IX_MT_PurchaseOrder_CompanyId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_MT_PurchaseOrderItem_CompanyId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_MT_Product_CompanyId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_MT_PriceList_CompanyId",
                table: "PriceLists");

            migrationBuilder.DropIndex(
                name: "IX_MT_PriceListItem_CompanyId",
                table: "PriceListItems");

            migrationBuilder.DropIndex(
                name: "IX_MT_InventoryTransaction_CompanyId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_MT_GRNHeader_CompanyId",
                table: "GRNHeaders");

            migrationBuilder.DropIndex(
                name: "IX_MT_GRNDetail_CompanyId",
                table: "GRNDetails");

            migrationBuilder.DropIndex(
                name: "IX_MT_GatePass_CompanyId",
                table: "GatePasses");

            migrationBuilder.DropIndex(
                name: "IX_MT_ExpenseEntry_CompanyId",
                table: "ExpenseEntries");

            migrationBuilder.DropIndex(
                name: "IX_MT_ExpenseCategory_CompanyId",
                table: "ExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_MT_Category_CompanyId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_MT_AppNotification_CompanyId",
                table: "AppNotifications");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "SaleReturnItems",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "SaleReturnHeaders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "SaleOrders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "SaleOrderItems",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "PurchaseReturns",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "PurchaseReturnItems",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "PurchaseOrders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "PurchaseOrderItems",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<decimal>(
                name: "DefaultGst",
                table: "Products",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "PriceListItems",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "InventoryTransactions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "GRNHeaders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "GRNDetails",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "GatePasses",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "ExpenseEntries",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "ExpenseCategories",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "AppNotifications",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }
    }
}
