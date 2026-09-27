using Azure.Identity;
using Azure.Storage.Blobs;
using FluentValidation;
using HotelBooking.Application.Amenities;
using HotelBooking.Application.Authentication;
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Bookings;
using HotelBooking.Application.Bookings.GetBookingConfirmation;
using HotelBooking.Application.Bookings.GetHotelBookings;
using HotelBooking.Application.Bookings.GetMyBookings;
using HotelBooking.Application.Cities;
using HotelBooking.Application.Cities.GetAdminCities;
using HotelBooking.Application.Cities.GetTrendingDestinations;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Countries;
using HotelBooking.Application.Deals;
using HotelBooking.Application.Deals.GetFeaturedDeals;
using HotelBooking.Application.Emails;
using HotelBooking.Application.HotelAmenities;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Hotels.GetAdminHotelById;
using HotelBooking.Application.Hotels.GetAdminHotels;
using HotelBooking.Application.Hotels.GetHotelDetails;
using HotelBooking.Application.Hotels.GetNearbyAttractions;
using HotelBooking.Application.Hotels.GetRecentlyVisitedHotels;
using HotelBooking.Application.Hotels.SearchHotels;
using HotelBooking.Application.Payments;
using HotelBooking.Application.Payments.Gateway;
using HotelBooking.Application.Reviews;
using HotelBooking.Application.Reviews.GetHotelReviews;
using HotelBooking.Application.Reviews.GetMyReviews;
using HotelBooking.Application.Rooms;
using HotelBooking.Application.Rooms.GetAdminRooms;
using HotelBooking.Application.Rooms.GetAvailableRooms;
using HotelBooking.Application.Rooms.GetHotelRooms;
using HotelBooking.Infrastructure.Authentication;
using HotelBooking.Infrastructure.BackgroundJobs;
using HotelBooking.Infrastructure.Documents;
using HotelBooking.Infrastructure.Emails;
using HotelBooking.Infrastructure.Hotels;
using HotelBooking.Infrastructure.Identity;
using HotelBooking.Infrastructure.Payments.Stripe;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Queries;
using HotelBooking.Infrastructure.Persistence.Repositories;
using HotelBooking.Infrastructure.Persistence.Seed;
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
    .AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(
        name: "database");

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

        services.Configure<StripeOptions>(
    configuration.GetSection(StripeOptions.SectionName));

        services.AddScoped<
            IImageStorageService,
            AzureBlobImageStorageService>();
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<IHotelRepository, HotelRepository>();
        services.AddScoped<IAmenityRepository, AmenityRepository>();
        services.AddScoped<IHotelAmenityRepository, HotelAmenityRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IHotelImageRepository, HotelImageRepository>();
        services.AddScoped<IRoomImageRepository, RoomImageRepository>();
        services.AddScoped<IDealRepository, DealRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IBookingConcurrencyManager, BookingConcurrencyManager>();
        services.AddHostedService<BookingExpirationWorker>();
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();
        services.AddScoped<IRefundRepository, RefundRepository>();
        services.AddScoped<IHotelSearchQuery, HotelSearchQuery>();
        services.AddScoped<IFeaturedDealsQuery, FeaturedDealsQuery>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IHotelDetailsQuery, HotelDetailsQuery>();
        services.AddScoped<IAvailableRoomsQuery, AvailableRoomsQuery>();
        services.AddScoped<IRecentlyVisitedHotelRepository,RecentlyVisitedHotelRepository>();
        services.AddScoped<IRecentlyVisitedHotelsQuery, RecentlyVisitedHotelsQuery>();
        services.AddScoped<IBookingConfirmationQuery, BookingConfirmationQuery>();
        services.AddScoped<ITrendingDestinationsQuery,TrendingDestinationsQuery>();
        services.AddScoped<IBookingConfirmationPdfGenerator,BookingConfirmationPdfGenerator>();


        services
    .AddOptions<SmtpEmailOptions>()
    .Bind(configuration.GetSection(SmtpEmailOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Host),
        "Email:Host is required.")
    .Validate(options => options.Port > 0,
        "Email:Port must be greater than zero.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Username),
        "Email:Username is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Password),
        "Email:Password is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.FromEmail),
        "Email:FromEmail is required.")
    .ValidateOnStart();

        services
     .AddOptions<GeoapifyOptions>()
     .Bind(configuration.GetSection(GeoapifyOptions.SectionName))
     .Validate(
         options => !string.IsNullOrWhiteSpace(options.BaseUrl),
         "Geoapify:BaseUrl is required.")
     .Validate(
         options => !string.IsNullOrWhiteSpace(options.ApiKey),
         "Geoapify:ApiKey is required.")
     .ValidateOnStart();

        services.AddHttpClient<
            INearbyAttractionsService,
            GeoapifyNearbyAttractionsService>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<GeoapifyOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(5);
            });

        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IHotelRoomsQuery, HotelRoomsQuery>();
        services.AddScoped<IAdminCitiesQuery, AdminCitiesQuery>();
        services.AddScoped<IAdminHotelsQuery, AdminHotelsQuery>();
        services.AddScoped<IAdminRoomsQuery, AdminRoomsQuery>();
        services.AddScoped<IAdminHotelByIdQuery, AdminHotelByIdQuery>();
        services.AddScoped<IGetMyReviewsQuery, GetMyReviewsQuery>();
        services.AddScoped<IHotelReviewsQuery, HotelReviewsQuery>();
        services.AddScoped< IHotelBookingsQuery,HotelBookingsQuery>();
        services.AddScoped<IMyBookingsQuery, MyBookingsQuery>();


        services.AddScoped<PerformanceDataSeeder>();

        return services;
    } 
}