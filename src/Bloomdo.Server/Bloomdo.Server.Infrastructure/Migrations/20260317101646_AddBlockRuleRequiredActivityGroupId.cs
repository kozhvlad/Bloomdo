using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bloomdo.Server.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockRuleRequiredActivityGroupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RequiredActivityGroupId",
                table: "BlockRules",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredActivityGroupId",
                table: "BlockRules");
        }
    }
}
