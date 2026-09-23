using HotelBooking.Domain.Hotels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class RecentlyVisitedHotelConfiguration
    : IEntityTypeConfiguration<RecentlyVisitedHotel>
{
    public void Configure(
        EntityTypeBuilder<RecentlyVisitedHotel> builder)
    {
        builder.ToTable("RecentlyVisitedHotels");

        builder.HasKey(visit => visit.Id);

        builder.Property(visit => visit.UserId)
            .IsRequired();

        builder.Property(visit => visit.HotelId)
            .IsRequired();

        builder.Property(visit => visit.LastVisitedAt)
            .IsRequired();

        builder.HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(visit => visit.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(visit => visit.Hotel)
     .WithMany()
     .HasForeignKey(visit => visit.HotelId)
     .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(visit => new
        {
            visit.UserId,
            visit.HotelId
        })
        .IsUnique();

        builder.HasIndex(visit => new
        {
            visit.UserId,
            visit.LastVisitedAt
        });
    }
}