using HotelBooking.Domain.Carts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class CartConfiguration
    : IEntityTypeConfiguration<Cart>
{
    public void Configure(
        EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");

        builder.HasKey(
            cart => cart.Id);

        builder.Property(
                cart => cart.UserId)
            .IsRequired();

        builder.Property(
                cart => cart.HotelId)
            .IsRequired();

        builder.Property(
                cart => cart.CreatedAt)
            .IsRequired();

        builder.Property(
            cart => cart.UpdatedAt);

        builder.HasIndex(
                cart => cart.UserId)
            .IsUnique();

        builder.HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(
                cart => cart.UserId)
            .OnDelete(
                DeleteBehavior.Cascade);

        builder.HasOne(
                cart => cart.Hotel)
            .WithMany()
            .HasForeignKey(
                cart => cart.HotelId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}