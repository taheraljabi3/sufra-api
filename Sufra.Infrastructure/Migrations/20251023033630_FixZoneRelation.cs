using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sufra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixZoneRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryProofs_Couriers_CourierId",
                table: "DeliveryProofs");

            migrationBuilder.DropForeignKey(
                name: "FK_MealRequests_Zones_ZoneId",
                table: "MealRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryProofs_Couriers_CourierId",
                table: "DeliveryProofs",
                column: "CourierId",
                principalTable: "Couriers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MealRequests_Zones_ZoneId",
                table: "MealRequests",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryProofs_Couriers_CourierId",
                table: "DeliveryProofs");

            migrationBuilder.DropForeignKey(
                name: "FK_MealRequests_Zones_ZoneId",
                table: "MealRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryProofs_Couriers_CourierId",
                table: "DeliveryProofs",
                column: "CourierId",
                principalTable: "Couriers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MealRequests_Zones_ZoneId",
                table: "MealRequests",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
