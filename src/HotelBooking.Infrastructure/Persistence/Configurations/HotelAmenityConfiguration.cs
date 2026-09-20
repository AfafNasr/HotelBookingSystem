using HotelBooking.Domain.Hotels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelBooking.Domain.Amenities;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class HotelAmenityConfiguration
    : IEntityTypeConfiguration<HotelAmenity>
{
    public void Configure(
        EntityTypeBuilder<HotelAmenity> builder)
    {
        builder.ToTable("HotelAmenities");

        builder.HasKey(hotelAmenity => new
        {
            hotelAmenity.HotelId,
            hotelAmenity.AmenityId
        });

        builder.HasOne<Hotel>()
            .WithMany()
            .HasForeignKey(hotelAmenity => hotelAmenity.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Amenity>()
            .WithMany()
            .HasForeignKey(hotelAmenity => hotelAmenity.AmenityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}