using HotelBooking.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class RoomImageConfiguration
    : IEntityTypeConfiguration<RoomImage>
{
    public void Configure(EntityTypeBuilder<RoomImage> builder)
    {
        builder.ToTable("RoomImages", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_RoomImages_DisplayOrder",
                "[DisplayOrder] >= 0");
        });

        builder.HasKey(image => image.Id);

        builder.Property(image => image.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(image => image.DisplayOrder)
            .IsRequired();

        builder.Property(image => image.IsPrimary)
            .IsRequired();

        builder.HasOne(image => image.Room)
            .WithMany()
            .HasForeignKey(image => image.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(image => new
        {
            image.RoomId,
            image.StorageKey
        })
            .IsUnique();
    }
}