using Azure.Identity;
using Azure.Storage.Blobs;
using FluentValidation;
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Rooms;
using HotelBooking.Infrastructure.Authentication;
using HotelBooking.Infrastructure.BackgroundJobs;
using HotelBooking.Infrastructure.Identity;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories;
using HotelBooking.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

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

        services
    .AddOptions<JwtOptions>()
    .Bind(configuration.GetSection(JwtOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Issuer),
        "JWT issuer is required.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Audience),
        "JWT audience is required.")
    .Validate(
    options => !string.IsNullOrWhiteSpace(options.Key)
        && Encoding.UTF8.GetByteCount(options.Key) >= 32,
    "JWT signing key must be at least 32 bytes.")
    .Validate(
        options => options.ExpirationMinutes > 0,
        "JWT expiration must be greater than zero.")
    .ValidateOnStart();

        services.AddSingleton<ITokenService, JwtTokenService>();

        services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = configuration
            .GetRequiredSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "JWT configuration is missing.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Key)),

            ValidateLifetime = true,

            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

        services.Configure<AzureStorageOptions>(
    configuration.GetSection(AzureStorageOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp
                .GetRequiredService<IOptions<AzureStorageOptions>>()
                .Value;

            var serviceUri = new Uri(
                $"https://{options.AccountName}.blob.core.windows.net");

            return new BlobServiceClient(
                serviceUri,
                new DefaultAzureCredential());
        });

        services.AddScoped<
            IImageStorageService,
            AzureBlobImageStorageService>();
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<IHotelRepository, HotelRepository>();
        services.AddScoped<IAmenityRepository, AmenityRepository>();
        services.AddScoped< IHotelAmenityRepository, HotelAmenityRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IHotelImageRepository, HotelImageRepository>();
        services.AddScoped<IRoomImageRepository, RoomImageRepository>();
        services.AddScoped<IDealRepository, DealRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IBookingConcurrencyManager, BookingConcurrencyManager>();
        services.AddHostedService<BookingExpirationWorker>();

        return services;
    }
}