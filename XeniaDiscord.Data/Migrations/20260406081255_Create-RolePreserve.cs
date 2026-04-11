using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateRolePreserve : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RolePreserveGuilds",
                columns: table => new
                {
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePreserveGuilds", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "RolePreserveUsers",
                columns: table => new
                {
                    GuildId = table.Column<string>(type: "character varying(40)", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuildMemberSnapshotId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePreserveUsers", x => new { x.GuildId, x.UserId });
                    table.ForeignKey(
                        name: "FK_RolePreserveUsers_RolePreserveGuilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "RolePreserveGuilds",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePreserveUsers_Snapshot_GuildMember_GuildMemberSnapshotId",
                        column: x => x.GuildMemberSnapshotId,
                        principalTable: "Snapshot_GuildMember",
                        principalColumn: "RecordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RolePreserveUsers_GuildMemberSnapshotId",
                table: "RolePreserveUsers",
                column: "GuildMemberSnapshotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePreserveUsers");

            migrationBuilder.DropTable(
                name: "RolePreserveGuilds");
        }
    }
}
