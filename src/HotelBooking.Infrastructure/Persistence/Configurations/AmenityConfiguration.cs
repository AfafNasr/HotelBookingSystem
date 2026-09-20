using HotelBooking.Domain.Amenities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class AmenityConfiguration
    : IEntityTypeConfiguration<Amenity>
{
    public void Configure(
        EntityTypeBuilder<Amenity> builder)
    {
        builder.ToTable("Amenities");

        builder.HasKey(amenity => amenity.Id);

        builder.Property(amenity => amenity.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(amenity => amenity.CreatedAt)
            .IsRequired();

        builder.Property(amenity => amenity.UpdatedAt);

        builder.Property(amenity => amenity.IsDeleted)
            .IsRequired();

        builder.Property(amenity => amenity.DeletedAt);

        builder.HasIndex(amenity => amenity.Name)
            .IsUnique();
    }
}