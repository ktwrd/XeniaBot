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
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:discord_snapshot_source", "unknown,member_joined,member_updated,user_updated,user_left,user_banned,user_unbanned,role_created,role_updated,role_deleted,joined_guild,left_guild,guild_updated")
                .Annotation("Npgsql:Enum:role_preserve_audit_action", "unknown,blacklist_add,blacklist_remove,enable,disable,applied_roles")
                .Annotation("Npgsql:Enum:role_preserve_audit_applied_role_action", "failure_unknown,success_grant,skipped_role_does_not_exist,skipped_blacklisted,failure_missing_permissions,failure_missing_permissions_hierarchy")
                .OldAnnotation("Npgsql:Enum:discord_snapshot_source", "unknown,member_joined,member_updated,user_updated,user_left,user_banned,user_unballed,role_created,role_updated,role_deleted,joined_guild,left_guild,guild_updated")
                .OldAnnotation("Npgsql:Enum:role_preserve_audit_action", "unknown,blacklist_add,blacklist_remove,enable,disable,applied_roles")
                .OldAnnotation("Npgsql:Enum:role_preserve_audit_applied_role_action", "failure_unknown,success_grant,skipped_role_does_not_exist,skipped_blacklisted,failure_missing_permissions,failure_missing_permissions_hierarchy");

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
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Cache_GuildRole");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Cache_GuildRole");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:discord_snapshot_source", "unknown,member_joined,member_updated,user_updated,user_left,user_banned,user_unballed,role_created,role_updated,role_deleted,joined_guild,left_guild,guild_updated")
                .Annotation("Npgsql:Enum:role_preserve_audit_action", "unknown,blacklist_add,blacklist_remove,enable,disable,applied_roles")
                .Annotation("Npgsql:Enum:role_preserve_audit_applied_role_action", "failure_unknown,success_grant,skipped_role_does_not_exist,skipped_blacklisted,failure_missing_permissions,failure_missing_permissions_hierarchy")
                .OldAnnotation("Npgsql:Enum:discord_snapshot_source", "unknown,member_joined,member_updated,user_updated,user_left,user_banned,user_unbanned,role_created,role_updated,role_deleted,joined_guild,left_guild,guild_updated")
                .OldAnnotation("Npgsql:Enum:role_preserve_audit_action", "unknown,blacklist_add,blacklist_remove,enable,disable,applied_roles")
                .OldAnnotation("Npgsql:Enum:role_preserve_audit_applied_role_action", "failure_unknown,success_grant,skipped_role_does_not_exist,skipped_blacklisted,failure_missing_permissions,failure_missing_permissions_hierarchy");
        }
    }
}
