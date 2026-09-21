using HotelBooking.Domain.Deals;
using HotelBooking.Domain.Hotels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable("Deals");

        builder.HasKey(deal => deal.Id);

        builder.Property(deal => deal.DiscountPercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(deal => deal.StartDate)
            .IsRequired();

        builder.Property(deal => deal.EndDate)
            .IsRequired();

        builder.Property(deal => deal.CreatedAt)
            .IsRequired();

        builder.Property(deal => deal.UpdatedAt)
            .IsRequired();

        builder.HasOne(deal => deal.Hotel)
            .WithMany()
            .HasForeignKey(deal => deal.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasCheckConstraint(
            "CK_Deals_DiscountPercentage",
            "[DiscountPercentage] > 0 AND [DiscountPercentage] < 100");

        builder.HasCheckConstraint(
            "CK_Deals_DateRange",
            "[EndDate] >= [StartDate]");

        builder.HasIndex(deal => new
        {
            deal.HotelId,
            deal.StartDate,
            deal.EndDate
        });
    }
}