using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class PartialSnapshotRenameCreatedAttoTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "UserPartialSnapshot",
                newName: "Timestamp");

            migrationBuilder.RenameIndex(
                name: "IX_UserPartialSnapshot_CreatedAt_UserId",
                table: "UserPartialSnapshot",
                newName: "IX_UserPartialSnapshot_Timestamp_UserId");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "GuildPartialSnapshot",
                newName: "Timestamp");

            migrationBuilder.RenameIndex(
                name: "IX_GuildPartialSnapshot_CreatedAt_GuildId",
                table: "GuildPartialSnapshot",
                newName: "IX_GuildPartialSnapshot_Timestamp_GuildId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "UserPartialSnapshot",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_UserPartialSnapshot_Timestamp_UserId",
                table: "UserPartialSnapshot",
                newName: "IX_UserPartialSnapshot_CreatedAt_UserId");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "GuildPartialSnapshot",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_GuildPartialSnapshot_Timestamp_GuildId",
                table: "GuildPartialSnapshot",
                newName: "IX_GuildPartialSnapshot_CreatedAt_GuildId");
        }
    }
}
