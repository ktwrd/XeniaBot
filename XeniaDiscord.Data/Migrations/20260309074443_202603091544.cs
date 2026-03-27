using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class _202603091544 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannedByUserId",
                table: "BanSyncRecords",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cache_Guild",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    OwnerUserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cache_Guild", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildPartialSnapshot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildPartialSnapshot", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cache_GuildMember",
                columns: table => new
                {
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsMember = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FirstJoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cache_GuildMember", x => new { x.GuildId, x.UserId });
                    table.ForeignKey(
                        name: "FK_Cache_GuildMember_Cache_Guild_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Cache_Guild",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BanSyncRecords_GuildId_UserId",
                table: "BanSyncRecords",
                columns: new[] { "GuildId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cache_GuildMember_GuildId_UserId_IsMember",
                table: "Cache_GuildMember",
                columns: new[] { "GuildId", "UserId", "IsMember" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildPartialSnapshot_CreatedAt_GuildId",
                table: "GuildPartialSnapshot",
                columns: new[] { "CreatedAt", "GuildId" },
                descending: new bool[0]);

            migrationBuilder.AddForeignKey(
                name: "FK_BanSyncRecords_BanSyncGuilds_GuildId",
                table: "BanSyncRecords",
                column: "GuildId",
                principalTable: "BanSyncGuilds",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BanSyncRecords_BanSyncGuilds_GuildId",
                table: "BanSyncRecords");

            migrationBuilder.DropTable(
                name: "Cache_GuildMember");

            migrationBuilder.DropTable(
                name: "GuildPartialSnapshot");

            migrationBuilder.DropTable(
                name: "Cache_Guild");

            migrationBuilder.DropIndex(
                name: "IX_BanSyncRecords_GuildId_UserId",
                table: "BanSyncRecords");

            migrationBuilder.DropColumn(
                name: "BannedByUserId",
                table: "BanSyncRecords");
        }
    }
}
