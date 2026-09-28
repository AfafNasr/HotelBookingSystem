using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable(
     "Reviews",
     table =>
     {
         table.HasCheckConstraint(
             "CK_Reviews_Rating",
             "[Rating] BETWEEN 1 AND 5");
     });

        builder.HasKey(review => review.Id);

        builder.Property(review => review.Rating)
            .IsRequired();

        builder.Property(review => review.Comment)
            .HasMaxLength(2000);

        builder.Property(review => review.CreatedAt)
            .IsRequired();

        builder.Property(review => review.UpdatedAt);

        builder.HasOne(review => review.Booking)
            .WithOne()
            .HasForeignKey<Review>(review => review.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(review => review.BookingId)
            .IsUnique();

    }
}