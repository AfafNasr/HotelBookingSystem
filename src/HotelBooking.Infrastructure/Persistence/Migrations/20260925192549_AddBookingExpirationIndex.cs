using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingExpirationIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status_ExpiresAt",
                table: "Bookings",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status_ExpiresAt",
                table: "Bookings");
        }
    }
}
