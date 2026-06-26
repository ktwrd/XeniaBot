using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRolePreserveAuditReferencedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RolePreserveAudit_ReferencedRoles",
                columns: table => new
                {
                    RolePreserveAuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePreserveAudit_ReferencedRoles", x => new { x.RolePreserveAuditId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_RolePreserveAudit_ReferencedRoles_RolePreserveAudit_RolePre~",
                        column: x => x.RolePreserveAuditId,
                        principalTable: "RolePreserveAudit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePreserveAudit_ReferencedRoles");
        }
    }
}
