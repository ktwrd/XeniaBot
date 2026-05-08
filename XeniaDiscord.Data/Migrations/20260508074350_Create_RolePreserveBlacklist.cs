using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class Create_RolePreserveBlacklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RolePreserveAudit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    TargetUserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    TargetRoleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePreserveAudit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePreserveAudit_RolePreserveGuilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "RolePreserveGuilds",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePreserveBlacklistedRoles",
                columns: table => new
                {
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RoleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePreserveBlacklistedRoles", x => new { x.GuildId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_RolePreserveBlacklistedRoles_RolePreserveGuilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "RolePreserveGuilds",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RolePreserveAudit_GuildId",
                table: "RolePreserveAudit",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePreserveAudit_RecordCreatedAt_GuildId",
                table: "RolePreserveAudit",
                columns: new[] { "RecordCreatedAt", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePreserveAudit_RecordCreatedAt_GuildId_UserId_TargetUser~",
                table: "RolePreserveAudit",
                columns: new[] { "RecordCreatedAt", "GuildId", "UserId", "TargetUserId", "TargetRoleId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePreserveAudit");

            migrationBuilder.DropTable(
                name: "RolePreserveBlacklistedRoles");
        }
    }
}
