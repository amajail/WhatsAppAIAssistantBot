using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsAppAIAssistantBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguageCodeToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "Users",
                type: "TEXT",
                maxLength: 5,
                nullable: false,
                defaultValue: "es");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "Users");
        }
    }
}
