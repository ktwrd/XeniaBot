using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class Create_InteractionStatisticModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Statistics_Interactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InteractionGroup = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InteractionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ChannelId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    GuildId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Count = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statistics_Interactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Statistics_Interactions_InteractionGroup_InteractionName_Gu~",
                table: "Statistics_Interactions",
                columns: new[] { "InteractionGroup", "InteractionName", "GuildId", "ChannelId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Statistics_Interactions");
        }
    }
}
