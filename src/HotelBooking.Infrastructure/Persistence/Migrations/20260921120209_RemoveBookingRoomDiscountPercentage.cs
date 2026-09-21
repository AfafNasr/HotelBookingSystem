using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBookingRoomDiscountPercentage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingRooms_DiscountPercentage",
                table: "BookingRooms");

            migrationBuilder.DropColumn(
                name: "DiscountPercentage",
                table: "BookingRooms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentage",
                table: "BookingRooms",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingRooms_DiscountPercentage",
                table: "BookingRooms",
                sql: "[DiscountPercentage] >= 0 AND [DiscountPercentage] < 100");
        }
    }
}
