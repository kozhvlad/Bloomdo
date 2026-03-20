using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bloomdo.Server.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetCount",
                table: "ActivityItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaskType",
                table: "ActivityItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CountValue",
                table: "ActivityCompletions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetCount",
                table: "ActivityItems");

            migrationBuilder.DropColumn(
                name: "TaskType",
                table: "ActivityItems");

            migrationBuilder.DropColumn(
                name: "CountValue",
                table: "ActivityCompletions");
        }
    }
}
