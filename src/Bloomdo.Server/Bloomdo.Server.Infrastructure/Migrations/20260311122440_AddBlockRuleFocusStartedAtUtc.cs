using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bloomdo.Server.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockRuleFocusStartedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FocusStartedAtUtc",
                table: "BlockRules",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FocusStartedAtUtc",
                table: "BlockRules");
        }
    }
}
