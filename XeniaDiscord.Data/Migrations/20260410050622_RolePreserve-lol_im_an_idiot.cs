using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class RolePreservelol_im_an_idiot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RolePreserveUserRoles",
                table: "RolePreserveUserRoles");

            migrationBuilder.DropIndex(
                name: "IX_RolePreserveUserRoles_GuildId_UserId_RoleId",
                table: "RolePreserveUserRoles");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolePreserveUserRoles",
                table: "RolePreserveUserRoles",
                columns: new[] { "GuildId", "UserId", "RoleId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RolePreserveUserRoles",
                table: "RolePreserveUserRoles");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolePreserveUserRoles",
                table: "RolePreserveUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePreserveUserRoles_GuildId_UserId_RoleId",
                table: "RolePreserveUserRoles",
                columns: new[] { "GuildId", "UserId", "RoleId" },
                unique: true,
                descending: new bool[0]);
        }
    }
}
