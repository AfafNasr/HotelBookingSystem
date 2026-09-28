using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable(
     "Payments",
     table =>
     {
         table.HasCheckConstraint(
             "CK_Payments_Amount",
             "[Amount] > 0");
     });

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.ProviderPaymentIntentId)
            .HasMaxLength(255);

        builder.Property(payment => payment.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(payment => payment.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(payment => payment.Status)
            .IsRequired();

        builder.Property(payment => payment.CreatedAt)
            .IsRequired();

        builder.Property(payment => payment.UpdatedAt);

        builder.HasOne<Booking>()
    .WithOne(booking => booking.Payment)
    .HasForeignKey<Payment>(payment => payment.BookingId)
    .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(payment => payment.BookingId)
            .IsUnique();

        builder.HasIndex(payment => payment.ProviderPaymentIntentId)
            .IsUnique()
            .HasFilter("[ProviderPaymentIntentId] IS NOT NULL");

    }
}