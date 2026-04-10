using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class RolePreserveUserRoleAssociationRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolePreserveUsers_Snapshot_GuildMember_GuildMemberSnapshotId",
                table: "RolePreserveUsers");

            migrationBuilder.DropIndex(
                name: "IX_RolePreserveUsers_GuildMemberSnapshotId",
                table: "RolePreserveUsers");

            migrationBuilder.DropColumn(
                name: "GuildMemberSnapshotId",
                table: "RolePreserveUsers");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "RolePreserveUsers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "RolePreserveUserRoles",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePreserveUserRoles", x => x.RoleId);
                    table.ForeignKey(
                        name: "FK_RolePreserveUserRoles_RolePreserveUsers_GuildId_UserId",
                        columns: x => new { x.GuildId, x.UserId },
                        principalTable: "RolePreserveUsers",
                        principalColumns: new[] { "GuildId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RolePreserveUserRoles_GuildId_UserId_RoleId",
                table: "RolePreserveUserRoles",
                columns: new[] { "GuildId", "UserId", "RoleId" },
                unique: true,
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePreserveUserRoles");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "RolePreserveUsers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<Guid>(
                name: "GuildMemberSnapshotId",
                table: "RolePreserveUsers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_RolePreserveUsers_GuildMemberSnapshotId",
                table: "RolePreserveUsers",
                column: "GuildMemberSnapshotId");

            migrationBuilder.AddForeignKey(
                name: "FK_RolePreserveUsers_Snapshot_GuildMember_GuildMemberSnapshotId",
                table: "RolePreserveUsers",
                column: "GuildMemberSnapshotId",
                principalTable: "Snapshot_GuildMember",
                principalColumn: "RecordId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
