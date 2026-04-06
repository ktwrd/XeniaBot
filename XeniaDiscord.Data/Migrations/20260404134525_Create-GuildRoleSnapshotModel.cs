using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateGuildRoleSnapshotModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GuildRoleSnapshotId",
                table: "Snapshot_GuildMemberRole",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Snapshot_GuildRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RoleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PermissionsValue = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Flags = table.Column<int>(type: "integer", nullable: false),
                    IconHash = table.Column<string>(type: "text", nullable: true),
                    IsManaged = table.Column<bool>(type: "boolean", nullable: false),
                    IsMentionable = table.Column<bool>(type: "boolean", nullable: false),
                    IsHoisted = table.Column<bool>(type: "boolean", nullable: false),
                    RecordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshot_GuildRole", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_GuildMemberRole_GuildRoleSnapshotId",
                table: "Snapshot_GuildMemberRole",
                column: "GuildRoleSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_GuildMemberPermission_RecordCreatedAt_GuildMemberS~",
                table: "Snapshot_GuildMemberPermission",
                columns: new[] { "RecordCreatedAt", "GuildMemberSnapshotId", "GuildId", "UserId" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_GuildRole_RecordCreatedAt_GuildId_RoleId",
                table: "Snapshot_GuildRole",
                columns: new[] { "RecordCreatedAt", "GuildId", "RoleId" },
                descending: new bool[0]);

            migrationBuilder.AddForeignKey(
                name: "FK_Snapshot_GuildMemberRole_Snapshot_GuildRole_GuildRoleSnapsh~",
                table: "Snapshot_GuildMemberRole",
                column: "GuildRoleSnapshotId",
                principalTable: "Snapshot_GuildRole",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Snapshot_GuildMemberRole_Snapshot_GuildRole_GuildRoleSnapsh~",
                table: "Snapshot_GuildMemberRole");

            migrationBuilder.DropTable(
                name: "Snapshot_GuildRole");

            migrationBuilder.DropIndex(
                name: "IX_Snapshot_GuildMemberRole_GuildRoleSnapshotId",
                table: "Snapshot_GuildMemberRole");

            migrationBuilder.DropIndex(
                name: "IX_Snapshot_GuildMemberPermission_RecordCreatedAt_GuildMemberS~",
                table: "Snapshot_GuildMemberPermission");

            migrationBuilder.DropColumn(
                name: "GuildRoleSnapshotId",
                table: "Snapshot_GuildMemberRole");
        }
    }
}
