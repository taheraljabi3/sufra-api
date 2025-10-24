using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sufra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Zone_Relation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentHousings_Zones_ZoneId1",
                table: "StudentHousings");

            migrationBuilder.DropIndex(
                name: "IX_StudentHousings_ZoneId1",
                table: "StudentHousings");

            migrationBuilder.DropColumn(
                name: "ZoneId1",
                table: "StudentHousings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ZoneId1",
                table: "StudentHousings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentHousings_ZoneId1",
                table: "StudentHousings",
                column: "ZoneId1");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentHousings_Zones_ZoneId1",
                table: "StudentHousings",
                column: "ZoneId1",
                principalTable: "Zones",
                principalColumn: "Id");
        }
    }
}
