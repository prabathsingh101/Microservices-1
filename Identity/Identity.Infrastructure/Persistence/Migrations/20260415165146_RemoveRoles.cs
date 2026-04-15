using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "RoleName",
                value: "Default Admin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "RoleName",
                value: "Admin");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CompanyId", "RoleName" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000002"), null, "User" },
                    { new Guid("00000000-0000-0000-0000-000000000003"), null, "Employee" },
                    { new Guid("00000000-0000-0000-0000-000000000004"), null, "Warehouse" },
                    { new Guid("00000000-0000-0000-0000-000000000005"), null, "Super Admin" },
                    { new Guid("00000000-0000-0000-0000-000000000006"), null, "Manager" },
                    { new Guid("00000000-0000-0000-0000-000000000007"), null, "Customer" }
                });
        }
    }
}
