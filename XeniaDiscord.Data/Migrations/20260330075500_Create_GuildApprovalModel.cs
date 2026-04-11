using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class Create_GuildApprovalModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildApproval",
                columns: table => new
                {
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ApprovedRoleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    LogChannelId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    EnableGreeter = table.Column<bool>(type: "boolean", nullable: false),
                    GreeterChannelId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    GreeterMessageTemplate = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    GreeterAsEmbed = table.Column<bool>(type: "boolean", nullable: false),
                    GreeterMentionUser = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildApproval", x => x.GuildId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildApproval_GuildId_Enabled",
                table: "GuildApproval",
                columns: new[] { "GuildId", "Enabled" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildApproval_GuildId_Enabled_EnableGreeter",
                table: "GuildApproval",
                columns: new[] { "GuildId", "Enabled", "EnableGreeter" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildApproval");
        }
    }
}
