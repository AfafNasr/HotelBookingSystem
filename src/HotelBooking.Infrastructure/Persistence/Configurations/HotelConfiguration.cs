using HotelBooking.Domain.Hotels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.AspNetCore.Identity;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class HotelConfiguration : IEntityTypeConfiguration<Hotel>
{
    public void Configure(EntityTypeBuilder<Hotel> builder)
    {
        builder.ToTable("Hotels", table =>
        {
            table.HasCheckConstraint(
                "CK_Hotels_StarRating",
                "[StarRating] BETWEEN 1 AND 5");

            table.HasCheckConstraint(
                "CK_Hotels_Latitude",
                "[Latitude] IS NULL OR [Latitude] BETWEEN -90 AND 90");

            table.HasCheckConstraint(
                "CK_Hotels_Longitude",
                "[Longitude] IS NULL OR [Longitude] BETWEEN -180 AND 180");

            table.HasCheckConstraint(
                "CK_Hotels_Coordinates",
                "([Latitude] IS NULL AND [Longitude] IS NULL) OR " +
                "([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL)");
        });

        builder.HasKey(hotel => hotel.Id);

        builder.Property(hotel => hotel.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(hotel => hotel.OwnerId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(hotel => hotel.StarRating)
            .IsRequired();

        builder.Property(hotel => hotel.Category)
            .IsRequired();

        builder.Property(hotel => hotel.Description)
            .HasMaxLength(2000);

        builder.Property(hotel => hotel.Address)
            .HasMaxLength(500);

        builder.Property(hotel => hotel.Latitude)
            .HasPrecision(9, 6);

        builder.Property(hotel => hotel.Longitude)
            .HasPrecision(9, 6);

        builder.Property(hotel => hotel.CreatedAt)
            .IsRequired();

        builder.Property(hotel => hotel.UpdatedAt);

        builder.Property(hotel => hotel.IsDeleted)
            .IsRequired();

        builder.Property(hotel => hotel.DeletedAt);

        builder.HasOne(hotel => hotel.City)
            .WithMany()
            .HasForeignKey(hotel => hotel.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<IdentityUser>()
    .WithMany()
    .HasForeignKey(hotel => hotel.OwnerId)
    .OnDelete(DeleteBehavior.Restrict);
    }
}