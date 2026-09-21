using HotelBooking.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class BookingRoomConfiguration
    : IEntityTypeConfiguration<BookingRoom>
{
    public void Configure(EntityTypeBuilder<BookingRoom> builder)
    {
        builder.ToTable("BookingRooms");

        builder.HasKey(bookingRoom => new
        {
            bookingRoom.BookingId,
            bookingRoom.RoomId
        });

        builder.Property(bookingRoom => bookingRoom.OriginalPricePerNight)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasOne(bookingRoom => bookingRoom.Booking)
            .WithMany(booking => booking.Rooms)
            .HasForeignKey(bookingRoom => bookingRoom.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bookingRoom => bookingRoom.Room)
            .WithMany()
            .HasForeignKey(bookingRoom => bookingRoom.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(bookingRoom => bookingRoom.RoomId);

        builder.HasCheckConstraint(
            "CK_BookingRooms_OriginalPricePerNight",
            "[OriginalPricePerNight] > 0");
    }
}