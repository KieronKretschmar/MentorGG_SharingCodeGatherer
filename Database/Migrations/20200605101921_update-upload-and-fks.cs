using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class updateuploadandfks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SharingCode",
                table: "Uploads");

            migrationBuilder.AddColumn<int>(
                name: "InternalMatchId",
                table: "Uploads",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Uploads_InternalMatchId",
                table: "Uploads",
                column: "InternalMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Uploads_SteamId",
                table: "Uploads",
                column: "SteamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Uploads_Matches_InternalMatchId",
                table: "Uploads",
                column: "InternalMatchId",
                principalTable: "Matches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Uploads_Users_SteamId",
                table: "Uploads",
                column: "SteamId",
                principalTable: "Users",
                principalColumn: "SteamId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Uploads_Matches_InternalMatchId",
                table: "Uploads");

            migrationBuilder.DropForeignKey(
                name: "FK_Uploads_Users_SteamId",
                table: "Uploads");

            migrationBuilder.DropIndex(
                name: "IX_Uploads_InternalMatchId",
                table: "Uploads");

            migrationBuilder.DropIndex(
                name: "IX_Uploads_SteamId",
                table: "Uploads");

            migrationBuilder.DropColumn(
                name: "InternalMatchId",
                table: "Uploads");

            migrationBuilder.AddColumn<string>(
                name: "SharingCode",
                table: "Uploads",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: true);
        }
    }
}
