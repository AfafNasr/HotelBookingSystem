using HotelBooking.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refunds");

        builder.HasKey(refund => refund.Id);

        builder.Property(refund => refund.ProviderRefundId)
            .HasMaxLength(255);

        builder.Property(refund => refund.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(refund => refund.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(refund => refund.Status)
            .IsRequired();

        builder.Property(refund => refund.CreatedAt)
            .IsRequired();

        builder.Property(refund => refund.UpdatedAt);

        builder.HasOne<Payment>()
            .WithOne()
            .HasForeignKey<Refund>(refund => refund.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(refund => refund.PaymentId)
            .IsUnique();

        builder.HasIndex(refund => refund.ProviderRefundId)
            .IsUnique()
            .HasFilter("[ProviderRefundId] IS NOT NULL");

        builder.HasCheckConstraint(
            "CK_Refunds_Amount",
            "[Amount] > 0");
    }
}