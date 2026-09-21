using HotelBooking.Domain.Hotels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class HotelImageConfiguration
    : IEntityTypeConfiguration<HotelImage>
{
    public void Configure(
        EntityTypeBuilder<HotelImage> builder)
    {
        builder.ToTable("HotelImages");

        builder.HasKey(image => image.Id);

        builder.Property(image => image.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(image => image.DisplayOrder)
            .IsRequired();

        builder.Property(image => image.IsPrimary)
            .IsRequired();

        builder.HasOne(image => image.Hotel)
            .WithMany()
            .HasForeignKey(image => image.HotelId)
            .OnDelete(DeleteBehavior.Restrict);


        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_HotelImages_DisplayOrder_NonNegative",
                "[DisplayOrder] >= 0");
        });
    }
}