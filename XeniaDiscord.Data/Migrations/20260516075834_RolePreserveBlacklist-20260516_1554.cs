using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class RolePreserveBlacklist20260516_1554 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:discord_snapshot_source", "unknown,member_joined,member_updated,user_updated,user_left,user_banned,user_unballed,role_created,role_updated,role_deleted,joined_guild,left_guild,guild_updated")
                .Annotation("Npgsql:Enum:role_preserve_audit_action", "unknown,blacklist_add,blacklist_remove,enable,disable,applied_roles")
                .Annotation("Npgsql:Enum:role_preserve_audit_applied_role_action", "failure_unknown,success_grant,skipped_role_does_not_exist,skipped_blacklisted,failure_missing_permissions,failure_missing_permissions_hierarchy")
                .OldAnnotation("Npgsql:Enum:discord_snapshot_source", "unknown,member_joined,member_updated,user_updated,user_left,user_banned,user_unballed,role_created,role_updated,role_deleted,joined_guild,left_guild,guild_updated");

            migrationBuilder.CreateTable(
                name: "Cache_GuildRole",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    RecordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cache_GuildRole", x => x.RoleId);
                    table.ForeignKey(
                        name: "FK_Cache_GuildRole_Snapshot_GuildRole_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshot_GuildRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePreserveAudit_AppliedRoles",
                columns: table => new
                {
                    RolePreserveAuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    ExceptionText = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePreserveAudit_AppliedRoles", x => new { x.RolePreserveAuditId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_RolePreserveAudit_AppliedRoles_RolePreserveAudit_RolePreser~",
                        column: x => x.RolePreserveAuditId,
                        principalTable: "RolePreserveAudit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cache_GuildRole_GuildId",
                table: "Cache_GuildRole",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Cache_GuildRole_Position",
                table: "Cache_GuildRole",
                column: "Position");

            migrationBuilder.CreateIndex(
                name: "IX_Cache_GuildRole_SnapshotId",
                table: "Cache_GuildRole",
                column: "SnapshotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cache_GuildRole");

            migrationBuilder.DropTable(
                name: "RolePreserveAudit_AppliedRoles");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:discord_snapshot_source", "unknown,member_joined,member_updated,user_updated,user_left,user_banned,user_unballed,role_created,role_updated,role_deleted,joined_guild,left_guild,guild_updated")
                .OldAnnotation("Npgsql:Enum:discord_snapshot_source", "unknown,member_joined,member_updated,user_updated,user_left,user_banned,user_unballed,role_created,role_updated,role_deleted,joined_guild,left_guild,guild_updated")
                .OldAnnotation("Npgsql:Enum:role_preserve_audit_action", "unknown,blacklist_add,blacklist_remove,enable,disable,applied_roles")
                .OldAnnotation("Npgsql:Enum:role_preserve_audit_applied_role_action", "failure_unknown,success_grant,skipped_role_does_not_exist,skipped_blacklisted,failure_missing_permissions,failure_missing_permissions_hierarchy");
        }
    }
}
