using HotelBooking.Domain.Carts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class CartItemConfiguration
    : IEntityTypeConfiguration<CartItem>
{
    public void Configure(
        EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");

        builder.HasKey(
            cartItem => new
            {
                cartItem.CartId,
                cartItem.RoomId
            });

        builder.Property(
                cartItem => cartItem.AddedAt)
            .IsRequired();

        builder.HasOne(
                cartItem => cartItem.Cart)
            .WithMany(
                cart => cart.Items)
            .HasForeignKey(
                cartItem => cartItem.CartId)
            .OnDelete(
                DeleteBehavior.Cascade);

        builder.HasOne(
                cartItem => cartItem.Room)
            .WithMany()
            .HasForeignKey(
                cartItem => cartItem.RoomId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasIndex(
            cartItem => cartItem.RoomId);
    }
}