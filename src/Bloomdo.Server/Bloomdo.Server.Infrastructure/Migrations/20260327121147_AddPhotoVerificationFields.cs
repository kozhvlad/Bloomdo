using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bloomdo.Server.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomVerificationCriteria",
                table: "ActivityItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationTemplateId",
                table: "ActivityItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomVerificationCriteria",
                table: "ActivityItems");

            migrationBuilder.DropColumn(
                name: "VerificationTemplateId",
                table: "ActivityItems");
        }
    }
}
