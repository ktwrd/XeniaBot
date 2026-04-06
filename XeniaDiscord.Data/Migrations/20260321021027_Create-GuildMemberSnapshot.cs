using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateGuildMemberSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Snapshot_GuildMember",
                columns: table => new
                {
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Discriminator = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    Nickname = table.Column<string>(type: "text", nullable: true),
                    IsSelfDeafened = table.Column<bool>(type: "boolean", nullable: false),
                    IsSelfMuted = table.Column<bool>(type: "boolean", nullable: false),
                    IsSuppressed = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeafened = table.Column<bool>(type: "boolean", nullable: false),
                    IsMuted = table.Column<bool>(type: "boolean", nullable: false),
                    IsStreaming = table.Column<bool>(type: "boolean", nullable: false),
                    VoiceChannelId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    GuildAvatarId = table.Column<string>(type: "text", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimedOutUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Flags = table.Column<int>(type: "integer", nullable: false),
                    PublicFlags = table.Column<int>(type: "integer", nullable: true),
                    IsPending = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshot_GuildMember", x => x.RecordId);
                });

            migrationBuilder.CreateTable(
                name: "Snapshot_PrimaryGuild",
                columns: table => new
                {
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    IdentityEnabled = table.Column<bool>(type: "boolean", nullable: true),
                    Tag = table.Column<string>(type: "text", nullable: false),
                    BadgeHash = table.Column<string>(type: "text", nullable: false),
                    BadgeUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshot_PrimaryGuild", x => x.RecordId);
                });

            migrationBuilder.CreateTable(
                name: "Snapshot_GuildMemberPermission",
                columns: table => new
                {
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildMemberSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Value = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshot_GuildMemberPermission", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK_Snapshot_GuildMemberPermission_Snapshot_GuildMember_GuildMe~",
                        column: x => x.GuildMemberSnapshotId,
                        principalTable: "Snapshot_GuildMember",
                        principalColumn: "RecordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Snapshot_GuildMemberRole",
                columns: table => new
                {
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildMemberSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RoleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshot_GuildMemberRole", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK_Snapshot_GuildMemberRole_Snapshot_GuildMember_GuildMemberSn~",
                        column: x => x.GuildMemberSnapshotId,
                        principalTable: "Snapshot_GuildMember",
                        principalColumn: "RecordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Snapshot_User",
                columns: table => new
                {
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Discriminator = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    GlobalName = table.Column<string>(type: "text", nullable: true),
                    IsBot = table.Column<bool>(type: "boolean", nullable: false),
                    IsWebhook = table.Column<bool>(type: "boolean", nullable: false),
                    PublicFlags = table.Column<int>(type: "integer", nullable: true),
                    AvatarId = table.Column<string>(type: "text", nullable: true),
                    AvatarDecorationSkuId = table.Column<string>(type: "text", nullable: true),
                    AvatarDecorationHash = table.Column<string>(type: "text", nullable: true),
                    PrimaryGuildId = table.Column<Guid>(type: "uuid", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    DefaultAvatarUrl = table.Column<string>(type: "text", nullable: true),
                    DisplayAvatarUrl = table.Column<string>(type: "text", nullable: true),
                    AvatarDecorationUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshot_User", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK_Snapshot_User_Snapshot_PrimaryGuild_PrimaryGuildId",
                        column: x => x.PrimaryGuildId,
                        principalTable: "Snapshot_PrimaryGuild",
                        principalColumn: "RecordId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_GuildMember_RecordCreatedAt_UserId_GuildId",
                table: "Snapshot_GuildMember",
                columns: new[] { "RecordCreatedAt", "UserId", "GuildId" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_GuildMemberPermission_GuildMemberSnapshotId",
                table: "Snapshot_GuildMemberPermission",
                column: "GuildMemberSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_GuildMemberRole_GuildMemberSnapshotId",
                table: "Snapshot_GuildMemberRole",
                column: "GuildMemberSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_User_PrimaryGuildId",
                table: "Snapshot_User",
                column: "PrimaryGuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_User_RecordCreatedAt_UserId",
                table: "Snapshot_User",
                columns: new[] { "RecordCreatedAt", "UserId" },
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Snapshot_GuildMemberPermission");

            migrationBuilder.DropTable(
                name: "Snapshot_GuildMemberRole");

            migrationBuilder.DropTable(
                name: "Snapshot_User");

            migrationBuilder.DropTable(
                name: "Snapshot_GuildMember");

            migrationBuilder.DropTable(
                name: "Snapshot_PrimaryGuild");
        }
    }
}
