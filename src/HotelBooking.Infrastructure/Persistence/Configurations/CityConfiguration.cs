using HotelBooking.Domain.Cities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities");

        builder.HasKey(city => city.Id);

        builder.Property(city => city.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(city => city.CountryCode)
            .HasMaxLength(2)
            .IsFixedLength()
            .IsRequired();

        builder.Property(city => city.PostOffice)
            .HasMaxLength(200);

        builder.Property(city => city.ThumbnailStorageKey)
           .HasMaxLength(500);

        builder.Property(city => city.CreatedAt)
            .IsRequired();

        builder.Property(city => city.UpdatedAt);

        builder.Property(city => city.IsDeleted)
            .IsRequired();

        builder.Property(city => city.DeletedAt);

        builder.HasOne(city => city.Country)
            .WithMany()
            .HasForeignKey(city => city.CountryCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(city => new
        {
            city.CountryCode,
            city.Name
        })
        .IsUnique();
    }
}