using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Company.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveCompanyIdToDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyProfiles_Addresses_AddressId",
                table: "CompanyProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyProfiles_BankDetails_BankDetailId",
                table: "CompanyProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CompanyProfiles_AddressId",
                table: "CompanyProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CompanyProfiles_BankDetailId",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "BankDetailId",
                table: "CompanyProfiles");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyProfileId",
                table: "BankDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyProfileId",
                table: "Addresses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankDetails_CompanyProfileId",
                table: "BankDetails",
                column: "CompanyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_CompanyProfileId",
                table: "Addresses",
                column: "CompanyProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_CompanyProfiles_CompanyProfileId",
                table: "Addresses",
                column: "CompanyProfileId",
                principalTable: "CompanyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BankDetails_CompanyProfiles_CompanyProfileId",
                table: "BankDetails",
                column: "CompanyProfileId",
                principalTable: "CompanyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_CompanyProfiles_CompanyProfileId",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_BankDetails_CompanyProfiles_CompanyProfileId",
                table: "BankDetails");

            migrationBuilder.DropIndex(
                name: "IX_BankDetails_CompanyProfileId",
                table: "BankDetails");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_CompanyProfileId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "CompanyProfileId",
                table: "BankDetails");

            migrationBuilder.DropColumn(
                name: "CompanyProfileId",
                table: "Addresses");

            migrationBuilder.AddColumn<int>(
                name: "AddressId",
                table: "CompanyProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BankDetailId",
                table: "CompanyProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_AddressId",
                table: "CompanyProfiles",
                column: "AddressId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_BankDetailId",
                table: "CompanyProfiles",
                column: "BankDetailId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyProfiles_Addresses_AddressId",
                table: "CompanyProfiles",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyProfiles_BankDetails_BankDetailId",
                table: "CompanyProfiles",
                column: "BankDetailId",
                principalTable: "BankDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
