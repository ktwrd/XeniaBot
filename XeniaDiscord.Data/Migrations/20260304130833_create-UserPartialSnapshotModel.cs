using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class createUserPartialSnapshotModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuildName",
                table: "BanSyncRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "UserPartialSnapshotId",
                table: "BanSyncRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "GuildId",
                table: "BanSyncGuildSnapshots",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "LogChannelId",
                table: "BanSyncGuildSnapshots",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserPartialSnapshot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Discriminator = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPartialSnapshot", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BanSyncRecords_UserPartialSnapshotId",
                table: "BanSyncRecords",
                column: "UserPartialSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPartialSnapshot_CreatedAt_UserId",
                table: "UserPartialSnapshot",
                columns: new[] { "CreatedAt", "UserId" },
                descending: new bool[0]);

            migrationBuilder.AddForeignKey(
                name: "FK_BanSyncRecords_UserPartialSnapshot_UserPartialSnapshotId",
                table: "BanSyncRecords",
                column: "UserPartialSnapshotId",
                principalTable: "UserPartialSnapshot",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BanSyncRecords_UserPartialSnapshot_UserPartialSnapshotId",
                table: "BanSyncRecords");

            migrationBuilder.DropTable(
                name: "UserPartialSnapshot");

            migrationBuilder.DropIndex(
                name: "IX_BanSyncRecords_UserPartialSnapshotId",
                table: "BanSyncRecords");

            migrationBuilder.DropColumn(
                name: "GuildName",
                table: "BanSyncRecords");

            migrationBuilder.DropColumn(
                name: "UserPartialSnapshotId",
                table: "BanSyncRecords");

            migrationBuilder.DropColumn(
                name: "LogChannelId",
                table: "BanSyncGuildSnapshots");

            migrationBuilder.AlterColumn<string>(
                name: "GuildId",
                table: "BanSyncGuildSnapshots",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);
        }
    }
}
