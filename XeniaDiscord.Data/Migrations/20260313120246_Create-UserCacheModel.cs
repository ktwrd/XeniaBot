using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserCacheModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "Cache_Guild",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscoverySplashUrl",
                table: "Cache_Guild",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IconUrl",
                table: "Cache_Guild",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SplashUrl",
                table: "Cache_Guild",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cache_User",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: true),
                    GlobalName = table.Column<string>(type: "text", nullable: true),
                    DisplayAvatarUrl = table.Column<string>(type: "text", nullable: true),
                    RecordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cache_User", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cache_User");

            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "Cache_Guild");

            migrationBuilder.DropColumn(
                name: "DiscoverySplashUrl",
                table: "Cache_Guild");

            migrationBuilder.DropColumn(
                name: "IconUrl",
                table: "Cache_Guild");

            migrationBuilder.DropColumn(
                name: "SplashUrl",
                table: "Cache_Guild");
        }
    }
}
