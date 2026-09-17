using FluentValidation;
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Infrastructure.Identity;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services
             .AddIdentityCore<IdentityUser>(options =>
             {
                options.User.RequireUniqueEmail = true;
             })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services
            .AddOptions<InitialAdminOptions>()
           .Bind(configuration.GetSection(InitialAdminOptions.SectionName))
           .Validate(
               options => !string.IsNullOrWhiteSpace(options.Username),
               "Initial admin username is required.")
           .Validate(
               options => !string.IsNullOrWhiteSpace(options.Email),
               "Initial admin email is required.")
           .Validate(
               options => !string.IsNullOrWhiteSpace(options.Password),
              "Initial admin password is required.")
           .ValidateOnStart();

        services.AddScoped<IdentityInitializer>();

        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }
}