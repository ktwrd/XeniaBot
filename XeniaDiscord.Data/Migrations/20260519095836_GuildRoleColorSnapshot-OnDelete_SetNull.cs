using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class GuildRoleColorSnapshotOnDelete_SetNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Snapshot_GuildRole_Color_Snapshot_GuildRole_GuildRoleSnapsh~",
                table: "Snapshot_GuildRole_Color");

            migrationBuilder.AddForeignKey(
                name: "FK_Snapshot_GuildRole_Color_Snapshot_GuildRole_GuildRoleSnapsh~",
                table: "Snapshot_GuildRole_Color",
                column: "GuildRoleSnapshotId",
                principalTable: "Snapshot_GuildRole",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Snapshot_GuildRole_Color_Snapshot_GuildRole_GuildRoleSnapsh~",
                table: "Snapshot_GuildRole_Color");

            migrationBuilder.AddForeignKey(
                name: "FK_Snapshot_GuildRole_Color_Snapshot_GuildRole_GuildRoleSnapsh~",
                table: "Snapshot_GuildRole_Color",
                column: "GuildRoleSnapshotId",
                principalTable: "Snapshot_GuildRole",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
