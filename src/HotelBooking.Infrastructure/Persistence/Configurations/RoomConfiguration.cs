using HotelBooking.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Room");

        builder.HasKey(room => room.Id);

        builder.Property(room => room.RoomNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(room => room.RoomType)
            .IsRequired();

        builder.Property(room => room.Description)
            .HasMaxLength(2000);

        builder.Property(room => room.AdultsCapacity)
            .IsRequired();

        builder.Property(room => room.ChildrenCapacity)
            .IsRequired();

        builder.Property(room => room.PricePerNight)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(room => room.CreatedAt)
            .IsRequired();

        builder.Property(room => room.UpdatedAt);

        builder.Property(room => room.IsDeleted)
            .IsRequired();

        builder.Property(room => room.DeletedAt);

        builder.HasIndex(room => new { room.HotelId, room.RoomNumber })
            .IsUnique();

        builder.HasOne(room => room.Hotel)
            .WithMany()
            .HasForeignKey(room => room.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(
            "Room",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_HotelRooms_AdultsCapacity",
                    "[AdultsCapacity] >= 1");

                table.HasCheckConstraint(
                    "CK_HotelRooms_ChildrenCapacity",
                    "[ChildrenCapacity] >= 0");

                table.HasCheckConstraint(
                    "CK_HotelRooms_PricePerNight",
                    "[PricePerNight] > 0");
            });
    }
}