using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserCacheModelAddPropsIsBot_IsWebhook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBot",
                table: "Cache_User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWebhook",
                table: "Cache_User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBot",
                table: "Cache_GuildMember",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWebhook",
                table: "Cache_GuildMember",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBot",
                table: "Cache_User");

            migrationBuilder.DropColumn(
                name: "IsWebhook",
                table: "Cache_User");

            migrationBuilder.DropColumn(
                name: "IsBot",
                table: "Cache_GuildMember");

            migrationBuilder.DropColumn(
                name: "IsWebhook",
                table: "Cache_GuildMember");
        }
    }
}
