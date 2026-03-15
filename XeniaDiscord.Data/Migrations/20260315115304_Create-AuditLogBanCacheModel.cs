using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateAuditLogBanCacheModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuditLogBanEntryId",
                table: "BanSyncRecords",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cache_AuditLogEntry_Ban",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TargetUserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    PerformedByUserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    JsonData = table.Column<string>(type: "text", nullable: true),
                    JsonDataType = table.Column<string>(type: "text", nullable: true),
                    RecordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cache_AuditLogEntry_Ban", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cache_AuditLogEntry_Ban_Id_GuildId_CreatedAt_Action",
                table: "Cache_AuditLogEntry_Ban",
                columns: new[] { "Id", "GuildId", "CreatedAt", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_Cache_AuditLogEntry_Ban_Id_GuildId_CreatedAt_Action_Perform~",
                table: "Cache_AuditLogEntry_Ban",
                columns: new[] { "Id", "GuildId", "CreatedAt", "Action", "PerformedByUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cache_AuditLogEntry_Ban");

            migrationBuilder.DropColumn(
                name: "AuditLogBanEntryId",
                table: "BanSyncRecords");
        }
    }
}
