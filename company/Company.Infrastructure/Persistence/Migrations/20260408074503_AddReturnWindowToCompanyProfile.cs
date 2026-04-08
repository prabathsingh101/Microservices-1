using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Company.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnWindowToCompanyProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReturnWindowUnit",
                table: "CompanyProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReturnWindowValue",
                table: "CompanyProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnWindowUnit",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "ReturnWindowValue",
                table: "CompanyProfiles");
        }
    }
}
