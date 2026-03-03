using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class createbansyncmodels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BanSyncGuilds",
                columns: table => new
                {
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    LogChannelId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Enable = table.Column<bool>(type: "boolean", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanSyncGuilds", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "BanSyncGuildSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<string>(type: "text", nullable: false),
                    Enable = table.Column<bool>(type: "boolean", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanSyncGuildSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BanSyncRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Ghost = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanSyncRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BanSyncGuilds_GuildId_State_Enable",
                table: "BanSyncGuilds",
                columns: new[] { "GuildId", "State", "Enable" });

            migrationBuilder.CreateIndex(
                name: "IX_BanSyncGuildSnapshots_Timestamp_GuildId",
                table: "BanSyncGuildSnapshots",
                columns: new[] { "Timestamp", "GuildId" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_BanSyncRecords_CreatedAt",
                table: "BanSyncRecords",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_BanSyncRecords_GuildId_Ghost",
                table: "BanSyncRecords",
                columns: new[] { "GuildId", "Ghost" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_BanSyncRecords_UserId_Ghost",
                table: "BanSyncRecords",
                columns: new[] { "UserId", "Ghost" },
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BanSyncGuilds");

            migrationBuilder.DropTable(
                name: "BanSyncGuildSnapshots");

            migrationBuilder.DropTable(
                name: "BanSyncRecords");
        }
    }
}
