using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Company.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDualReturnPolicyDynamic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReturnWindowValue",
                table: "CompanyProfiles",
                newName: "SaleReturnWindowValue");

            migrationBuilder.RenameColumn(
                name: "ReturnWindowUnit",
                table: "CompanyProfiles",
                newName: "SaleReturnWindowUnit");

            migrationBuilder.RenameColumn(
                name: "ReturnPolicyDisclaimer",
                table: "CompanyProfiles",
                newName: "SaleReturnPolicyDisclaimer");

            migrationBuilder.AddColumn<string>(
                name: "PurchaseReturnPolicyDisclaimer",
                table: "CompanyProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseReturnWindowUnit",
                table: "CompanyProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PurchaseReturnWindowValue",
                table: "CompanyProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchaseReturnPolicyDisclaimer",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "PurchaseReturnWindowUnit",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "PurchaseReturnWindowValue",
                table: "CompanyProfiles");

            migrationBuilder.RenameColumn(
                name: "SaleReturnWindowValue",
                table: "CompanyProfiles",
                newName: "ReturnWindowValue");

            migrationBuilder.RenameColumn(
                name: "SaleReturnWindowUnit",
                table: "CompanyProfiles",
                newName: "ReturnWindowUnit");

            migrationBuilder.RenameColumn(
                name: "SaleReturnPolicyDisclaimer",
                table: "CompanyProfiles",
                newName: "ReturnPolicyDisclaimer");
        }
    }
}
