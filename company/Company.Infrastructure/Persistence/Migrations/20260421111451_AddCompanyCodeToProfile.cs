using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Company.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyCodeToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyCode",
                table: "CompanyProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // 🚀 SEED DATA: Sync with Identity codes
            migrationBuilder.Sql("UPDATE CompanyProfiles SET CompanyCode = UPPER(LEFT(REPLACE(Name, ' ', ''), 6)) + RIGHT(CAST(Id AS NVARCHAR(36)), 4) WHERE CompanyCode IS NULL");
            migrationBuilder.Sql("UPDATE CompanyProfiles SET CompanyCode = 'ADMIN' WHERE Name LIKE '%Admin%'");
            migrationBuilder.Sql("UPDATE CompanyProfiles SET CompanyCode = 'CHANDAN' WHERE Name LIKE '%Chandan%'");
            migrationBuilder.Sql("UPDATE CompanyProfiles SET CompanyCode = 'SONU' WHERE Name LIKE '%Sonu%'");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_CompanyCode",
                table: "CompanyProfiles",
                column: "CompanyCode",
                unique: true,
                filter: "[CompanyCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanyProfiles_CompanyCode",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyCode",
                table: "CompanyProfiles");
        }
    }
}
