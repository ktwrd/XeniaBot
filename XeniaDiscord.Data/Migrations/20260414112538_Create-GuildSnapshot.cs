using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateGuildSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:discord_snapshot_source", "unknown,member_joined,member_updated,user_updated,user_left,user_banned,user_unballed,role_created,role_updated,role_deleted,joined_guild,left_guild,guild_updated");

            migrationBuilder.CreateTable(
                name: "Snapshot_Guild",
                columns: table => new
                {
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SnapshotSource = table.Column<int>(type: "integer", nullable: false),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OwnerUserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EveryoneRoleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IconId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BannerUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BannerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SplashUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SplashId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DiscoverySplashUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DiscoverySplashId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VanityUrlCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AfkTimeout = table.Column<int>(type: "integer", nullable: false),
                    DefaultMessageNotifications = table.Column<int>(type: "integer", nullable: false),
                    MfaLevel = table.Column<int>(type: "integer", nullable: false),
                    VerificationLevel = table.Column<int>(type: "integer", nullable: false),
                    ExplicitContentFilter = table.Column<int>(type: "integer", nullable: false),
                    NsfwLevel = table.Column<int>(type: "integer", nullable: false),
                    GuildFeatures = table.Column<long>(type: "bigint", nullable: false),
                    PremiumSubscriptionCount = table.Column<int>(type: "integer", nullable: false),
                    IsBoostProgressBarEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MaxPresences = table.Column<int>(type: "integer", nullable: true),
                    MaxMembers = table.Column<int>(type: "integer", nullable: true),
                    MaxVideoChannelUsers = table.Column<int>(type: "integer", nullable: true),
                    MaxStageVideoChannelUsers = table.Column<int>(type: "integer", nullable: true),
                    ApproximateMemberCount = table.Column<int>(type: "integer", nullable: true),
                    ApproximatePresenceCount = table.Column<int>(type: "integer", nullable: true),
                    MaxBitrate = table.Column<int>(type: "integer", nullable: false),
                    MaxUploadLimit = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    PreferredLocale = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VoiceRegionId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AfkChannelId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    WidgetChannelId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    SafetyAlertsChannelId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    SystemChannelId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RulesChannelId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PublicUpdatesChannelId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ApplicationId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshot_Guild", x => x.RecordId);
                });

            migrationBuilder.CreateTable(
                name: "SnapshotEvent_Guild",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    BeforeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotEvent_Guild", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SnapshotEvent_Guild_Snapshot_Guild_BeforeId",
                        column: x => x.BeforeId,
                        principalTable: "Snapshot_Guild",
                        principalColumn: "RecordId");
                    table.ForeignKey(
                        name: "FK_SnapshotEvent_Guild_Snapshot_Guild_CurrentId",
                        column: x => x.CurrentId,
                        principalTable: "Snapshot_Guild",
                        principalColumn: "RecordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_Guild_RecordCreatedAt_GuildId",
                table: "Snapshot_Guild",
                columns: new[] { "RecordCreatedAt", "GuildId" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotEvent_Guild_BeforeId",
                table: "SnapshotEvent_Guild",
                column: "BeforeId");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotEvent_Guild_CurrentId",
                table: "SnapshotEvent_Guild",
                column: "CurrentId");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotEvent_Guild_GuildId_Timestamp_Source",
                table: "SnapshotEvent_Guild",
                columns: new[] { "GuildId", "Timestamp", "Source" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotEvent_Guild_Timestamp_GuildId",
                table: "SnapshotEvent_Guild",
                columns: new[] { "Timestamp", "GuildId" },
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SnapshotEvent_Guild");

            migrationBuilder.DropTable(
                name: "Snapshot_Guild");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:discord_snapshot_source", "unknown,member_joined,member_updated,user_updated,user_left,user_banned,user_unballed,role_created,role_updated,role_deleted,joined_guild,left_guild,guild_updated");
        }
    }
}
