using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class Create_GuildAndUserAndRoleSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cache_GuildChannel",
                columns: table => new
                {
                    ChannelId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cache_GuildChannel", x => x.ChannelId);
                    table.ForeignKey(
                        name: "FK_Cache_GuildChannel_Cache_Guild_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Cache_Guild",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServerLogGuilds",
                columns: table => new
                {
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerLogGuilds", x => x.GuildId);
                    table.ForeignKey(
                        name: "FK_ServerLogGuilds_Cache_Guild_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Cache_Guild",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServerLogChannel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ChannelId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Event = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ServerLogGuildGuildId = table.Column<string>(type: "character varying(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerLogChannel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerLogChannel_Cache_Guild_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Cache_Guild",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServerLogChannel_ServerLogGuilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "ServerLogGuilds",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServerLogChannel_ServerLogGuilds_ServerLogGuildGuildId",
                        column: x => x.ServerLogGuildGuildId,
                        principalTable: "ServerLogGuilds",
                        principalColumn: "GuildId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_GuildMemberRole_RecordCreatedAt_GuildMemberSnapsho~",
                table: "Snapshot_GuildMemberRole",
                columns: new[] { "RecordCreatedAt", "GuildMemberSnapshotId", "GuildId", "UserId" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Cache_GuildChannel_GuildId_ChannelId",
                table: "Cache_GuildChannel",
                columns: new[] { "GuildId", "ChannelId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServerLogChannel_GuildId_ChannelId_Event",
                table: "ServerLogChannel",
                columns: new[] { "GuildId", "ChannelId", "Event" });

            migrationBuilder.CreateIndex(
                name: "IX_ServerLogChannel_ServerLogGuildGuildId",
                table: "ServerLogChannel",
                column: "ServerLogGuildGuildId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerLogGuilds_GuildId_Enabled",
                table: "ServerLogGuilds",
                columns: new[] { "GuildId", "Enabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cache_GuildChannel");

            migrationBuilder.DropTable(
                name: "ServerLogChannel");

            migrationBuilder.DropTable(
                name: "ServerLogGuilds");

            migrationBuilder.DropIndex(
                name: "IX_Snapshot_GuildMemberRole_RecordCreatedAt_GuildMemberSnapsho~",
                table: "Snapshot_GuildMemberRole");
        }
    }
}
