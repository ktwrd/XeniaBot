using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class _20260313 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BanSyncRecordModelGuildMemberCacheModel",
                columns: table => new
                {
                    RelatedBanSyncRecordsId = table.Column<Guid>(type: "uuid", nullable: false),
                    CachedGuildMembersByUserGuildId = table.Column<string>(type: "character varying(40)", nullable: false),
                    CachedGuildMembersByUserUserId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanSyncRecordModelGuildMemberCacheModel", x => new { x.RelatedBanSyncRecordsId, x.CachedGuildMembersByUserGuildId, x.CachedGuildMembersByUserUserId });
                    table.ForeignKey(
                        name: "FK_BanSyncRecordModelGuildMemberCacheModel_BanSyncRecords_Rela~",
                        column: x => x.RelatedBanSyncRecordsId,
                        principalTable: "BanSyncRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BanSyncRecordModelGuildMemberCacheModel_Cache_GuildMember_C~",
                        columns: x => new { x.CachedGuildMembersByUserGuildId, x.CachedGuildMembersByUserUserId },
                        principalTable: "Cache_GuildMember",
                        principalColumns: new[] { "GuildId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BanSyncRecordModelGuildMemberCacheModel_CachedGuildMembersB~",
                table: "BanSyncRecordModelGuildMemberCacheModel",
                columns: new[] { "CachedGuildMembersByUserGuildId", "CachedGuildMembersByUserUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BanSyncRecordModelGuildMemberCacheModel");
        }
    }
}
