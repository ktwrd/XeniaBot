using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateGuildRolePermissionSnapshotModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PermissionsValue",
                table: "Snapshot_GuildRole");

            migrationBuilder.CreateTable(
                name: "Snapshot_GuildRolePermission",
                columns: table => new
                {
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildRoleSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RoleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Value = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshot_GuildRolePermission", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK_Snapshot_GuildRolePermission_Snapshot_GuildRole_GuildRoleSn~",
                        column: x => x.GuildRoleSnapshotId,
                        principalTable: "Snapshot_GuildRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_GuildRolePermission_GuildRoleSnapshotId",
                table: "Snapshot_GuildRolePermission",
                column: "GuildRoleSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_GuildRolePermission_RecordCreatedAt_GuildRoleSnaps~",
                table: "Snapshot_GuildRolePermission",
                columns: new[] { "RecordCreatedAt", "GuildRoleSnapshotId", "GuildId", "RoleId" },
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Snapshot_GuildRolePermission");

            migrationBuilder.AddColumn<string>(
                name: "PermissionsValue",
                table: "Snapshot_GuildRole",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");
        }
    }
}
