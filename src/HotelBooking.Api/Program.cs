using HotelBooking.Api.Authentication;
using HotelBooking.Api.Authorization;
using HotelBooking.Api.ErrorHandling;
using HotelBooking.Application;
using HotelBooking.Application.Bookings;
using HotelBooking.Application.Common.Security;
using HotelBooking.Infrastructure;
using HotelBooking.Infrastructure.Identity;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Add services to the container.

builder.Services.AddApplication();



builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddControllers();


var trendingCacheExpirationMinutes =
    builder.Configuration.GetValue<int>(
        "Caching:TrendingDestinationsExpirationMinutes");

if (trendingCacheExpirationMinutes <= 0)
{
    throw new InvalidOperationException(
        "Trending destinations cache expiration must be greater than zero.");
}

builder.Services.AddOutputCache(options =>
{
    options.AddPolicy(
        "TrendingDestinations",
        policy =>
            policy.Expire(
                TimeSpan.FromMinutes(
                    trendingCacheExpirationMinutes)));
});

var loginPermitLimit =
    builder.Configuration.GetValue<int>(
        "RateLimiting:Login:PermitLimit");

var loginWindowSeconds =
    builder.Configuration.GetValue<int>(
        "RateLimiting:Login:WindowSeconds");

if (loginPermitLimit <= 0)
{
    throw new InvalidOperationException(
        "Login rate limit permit count must be greater than zero.");
}

if (loginWindowSeconds <= 0)
{
    throw new InvalidOperationException(
        "Login rate limit window must be greater than zero.");
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy(
        "Login",
        httpContext =>
        {
            var clientIp =
                httpContext.Connection.RemoteIpAddress?
                    .ToString()
                ?? "unknown";

            return RateLimitPartition
                .GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit =
                                loginPermitLimit,

                            Window =
                                TimeSpan.FromSeconds(
                                    loginWindowSeconds),

                            QueueLimit = 0,

                            AutoReplenishment = true
                        });
        });
});


builder.Services.AddHttpContextAccessor();

builder.Services.AddPermissionAuthorization();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped< ICurrentUserService, CurrentUserService>();

QuestPDF.Settings.License = LicenseType.Community;

builder.Services
    .AddOptions<BookingOptions>()
    .Bind(builder.Configuration.GetSection(BookingOptions.SectionName))
    .Validate(
        options => options.PaymentHoldDurationMinutes > 0,
        "Payment hold duration must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddSingleton(
    serviceProvider =>
        serviceProvider
            .GetRequiredService<IOptions<BookingOptions>>()
            .Value);

var app = builder.Build();


// Initialize the default Identity roles at application startup.
if (!app.Environment.IsEnvironment("Testing"))
{
    await using var scope =
        app.Services.CreateAsyncScope();

    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();

    var identityInitializer =
        scope.ServiceProvider
            .GetRequiredService<IdentityInitializer>();

    await identityInitializer.InitializeAsync();

    var shouldSeedPerformanceData =
        args.Contains(
            "--seed-performance-data",
            StringComparer.OrdinalIgnoreCase);

    if (shouldSeedPerformanceData)
    {
        if (!app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Performance data can only be seeded in the Development environment.");
        }

        var performanceDataSeeder =
            scope.ServiceProvider
                .GetRequiredService<PerformanceDataSeeder>();

        await performanceDataSeeder.SeedAsync();

        return;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.UseOutputCache();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

