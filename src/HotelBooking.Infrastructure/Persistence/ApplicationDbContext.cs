using HotelBooking.Domain.Amenities;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Cities;
using HotelBooking.Domain.Deals;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Payments;
using HotelBooking.Domain.Rooms;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HotelBooking.Domain.Reviews;

namespace HotelBooking.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext
{

    public DbSet<Country> Countries => Set<Country>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<HotelAmenity> HotelAmenities =>Set<HotelAmenity>();
    public DbSet<Room> Room => Set<Room>();
    public DbSet<HotelImage> HotelImages => Set<HotelImage>();
    public DbSet<RoomImage> RoomImages => Set<RoomImage>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingRoom> BookingRooms => Set<BookingRoom>();
    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Refund> Refunds => Set<Refund>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<RecentlyVisitedHotel> RecentlyVisitedHotels
    => Set<RecentlyVisitedHotel>();

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(
    typeof(ApplicationDbContext).Assembly);
        var userBuilder =
            builder.Entity<IdentityUser>();

        userBuilder
            .HasIndex(user => user.NormalizedEmail)
            .IsUnique();

        userBuilder
            .Property<DateTime?>("DeactivatedAt");

        userBuilder
            .HasQueryFilter(
                user =>
                    EF.Property<DateTime?>(
                        user,
                        "DeactivatedAt") == null);
    }
}