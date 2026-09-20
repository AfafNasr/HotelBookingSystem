using HotelBooking.Domain.Amenities;
using HotelBooking.Domain.Cities;
using HotelBooking.Domain.Hotels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext
{

    public DbSet<Country> Countries => Set<Country>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<HotelAmenity> HotelAmenities =>Set<HotelAmenity>();

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

        builder.Entity<IdentityUser>()
            .HasIndex(user => user.NormalizedEmail)
            .IsUnique();
    }
}