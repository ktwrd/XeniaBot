using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class RolePreserveAuditRoleReferenceGuildRoleCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RolePreserveAudit_ReferencedRoles_RoleId",
                table: "RolePreserveAudit_ReferencedRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePreserveAudit_AppliedRoles_RoleId",
                table: "RolePreserveAudit_AppliedRoles",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_RolePreserveAudit_AppliedRoles_Cache_GuildRole_RoleId",
                table: "RolePreserveAudit_AppliedRoles",
                column: "RoleId",
                principalTable: "Cache_GuildRole",
                principalColumn: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_RolePreserveAudit_ReferencedRoles_Cache_GuildRole_RoleId",
                table: "RolePreserveAudit_ReferencedRoles",
                column: "RoleId",
                principalTable: "Cache_GuildRole",
                principalColumn: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolePreserveAudit_AppliedRoles_Cache_GuildRole_RoleId",
                table: "RolePreserveAudit_AppliedRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePreserveAudit_ReferencedRoles_Cache_GuildRole_RoleId",
                table: "RolePreserveAudit_ReferencedRoles");

            migrationBuilder.DropIndex(
                name: "IX_RolePreserveAudit_ReferencedRoles_RoleId",
                table: "RolePreserveAudit_ReferencedRoles");

            migrationBuilder.DropIndex(
                name: "IX_RolePreserveAudit_AppliedRoles_RoleId",
                table: "RolePreserveAudit_AppliedRoles");
        }
    }
}
