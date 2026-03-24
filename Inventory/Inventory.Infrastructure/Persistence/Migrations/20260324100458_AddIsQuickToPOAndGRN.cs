using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsQuickToPOAndGRN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsQuick",
                table: "PurchaseOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsQuick",
                table: "GRNHeaders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsQuick",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "IsQuick",
                table: "GRNHeaders");
        }
    }
}
