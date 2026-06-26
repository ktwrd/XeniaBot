using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class GuildRoleCache_Add_IsDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TYPE discord_snapshot_source RENAME VALUE 'user_unballed' TO 'user_unbanned';");
         
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Cache_GuildRole",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Cache_GuildRole",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TYPE discord_snapshot_source RENAME VALUE 'user_unbanned' TO 'user_unballed';");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Cache_GuildRole");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Cache_GuildRole");
        }
    }
}
