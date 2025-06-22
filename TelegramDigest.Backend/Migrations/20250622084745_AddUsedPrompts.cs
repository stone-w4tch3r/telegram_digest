using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramDigest.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUsedPrompts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsedPrompts",
                table: "Digests",
                type: "json",
                maxLength: 4096,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsedPrompts",
                table: "Digests");
        }
    }
}
