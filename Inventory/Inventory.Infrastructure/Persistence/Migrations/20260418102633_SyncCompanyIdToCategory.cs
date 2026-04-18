using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncCompanyIdToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Column already exists and is handled manually in DB
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed for manual schema change
        }
    }
}
