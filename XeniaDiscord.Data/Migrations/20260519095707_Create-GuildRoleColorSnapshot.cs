using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateGuildRoleColorSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Snapshot_GuildRole_Color",
                columns: table => new
                {
                    GuildRoleSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    R = table.Column<byte>(type: "smallint", nullable: false),
                    G = table.Column<byte>(type: "smallint", nullable: false),
                    B = table.Column<byte>(type: "smallint", nullable: false),
                    SecondaryRed = table.Column<byte>(type: "smallint", nullable: true),
                    SecondaryGreen = table.Column<byte>(type: "smallint", nullable: true),
                    SecondaryBlue = table.Column<byte>(type: "smallint", nullable: true),
                    TertiaryRed = table.Column<byte>(type: "smallint", nullable: true),
                    TertiaryGreen = table.Column<byte>(type: "smallint", nullable: true),
                    TertiaryBlue = table.Column<byte>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshot_GuildRole_Color", x => x.GuildRoleSnapshotId);
                    table.ForeignKey(
                        name: "FK_Snapshot_GuildRole_Color_Snapshot_GuildRole_GuildRoleSnapsh~",
                        column: x => x.GuildRoleSnapshotId,
                        principalTable: "Snapshot_GuildRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Snapshot_GuildRole_Color");
        }
    }
}
