using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class addanalyzerquality : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "AnalyzedQuality",
                table: "Matches",
                nullable: false,
                defaultValue: (byte)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalyzedQuality",
                table: "Matches");
        }
    }
}
