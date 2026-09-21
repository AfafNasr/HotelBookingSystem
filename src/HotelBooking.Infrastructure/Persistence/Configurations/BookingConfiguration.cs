using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Hotels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Identity;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(booking => booking.Id);

        builder.Property(booking => booking.UserId)
            .IsRequired();

        builder.Property(booking => booking.CheckInDate)
            .IsRequired();

        builder.Property(booking => booking.CheckOutDate)
            .IsRequired();

        builder.Property(booking => booking.Status)
            .IsRequired();

        builder.Property(booking => booking.ExpiresAt);

        builder.Property(booking => booking.GuestFullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(booking => booking.GuestEmail)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(booking => booking.GuestPhoneNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(booking => booking.SpecialRequests)
            .HasMaxLength(2000);

        builder.Property(booking => booking.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(booking => booking.ConfirmationNumber)
            .HasMaxLength(50);

        builder.Property(booking => booking.CreatedAt)
            .IsRequired();

        builder.Property(booking => booking.UpdatedAt);

        builder.HasOne<Hotel> ()
            .WithMany()
            .HasForeignKey(booking => booking.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<IdentityUser>()
    .WithMany()
    .HasForeignKey(booking => booking.UserId)
    .OnDelete(DeleteBehavior.Restrict); 


        builder.HasIndex(booking => booking.ConfirmationNumber)
            .IsUnique()
            .HasFilter("[ConfirmationNumber] IS NOT NULL");



        builder.HasCheckConstraint(
            "CK_Bookings_DateRange",
            "[CheckOutDate] > [CheckInDate]");

        builder.HasCheckConstraint(
            "CK_Bookings_TotalAmount",
            "[TotalAmount] > 0");
    }
}