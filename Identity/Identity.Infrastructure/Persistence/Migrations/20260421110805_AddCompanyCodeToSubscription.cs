using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyCodeToSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "CompanyCode",
                table: "Subscriptions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // 🚀 SEED DATA: Give existing companies a code based on their name
            migrationBuilder.Sql("UPDATE Subscriptions SET CompanyCode = UPPER(LEFT(REPLACE(CompanyName, ' ', ''), 6)) + RIGHT(CAST(CompanyId AS NVARCHAR(36)), 4) WHERE CompanyCode = '' OR CompanyCode IS NULL");
            
            // Special fix for common ones if needed
            migrationBuilder.Sql("UPDATE Subscriptions SET CompanyCode = 'ADMIN' WHERE CompanyName LIKE '%Admin%'");
            migrationBuilder.Sql("UPDATE Subscriptions SET CompanyCode = 'CHANDAN' WHERE CompanyName LIKE '%Chandan%'");
            migrationBuilder.Sql("UPDATE Subscriptions SET CompanyCode = 'SONU' WHERE CompanyName LIKE '%Sonu%'");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_CompanyId",
                table: "Users",
                columns: new[] { "Email", "CompanyId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName_CompanyId",
                table: "Users",
                columns: new[] { "UserName", "CompanyId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CompanyCode",
                table: "Subscriptions",
                column: "CompanyCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email_CompanyId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_UserName_CompanyId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CompanyCode",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CompanyCode",
                table: "Subscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }
    }
}
