using HotelBooking.Infrastructure.Identity;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace HotelBooking.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly string _databaseName;
    private readonly string _connectionString;

    public CustomWebApplicationFactory()
    {
        _databaseName =
            $"HotelBookingIntegration_{Guid.NewGuid():N}";

        var configuredConnectionString =
            Environment.GetEnvironmentVariable(
                "HOTELBOOKING_TEST_CONNECTION_STRING");

        var baseConnectionString =
            string.IsNullOrWhiteSpace(configuredConnectionString)
                ? "Server=localhost;" +
                  "Trusted_Connection=True;" +
                  "TrustServerCertificate=True;"
                : configuredConnectionString;

        var connectionStringBuilder =
            new SqlConnectionStringBuilder(
                baseConnectionString)
            {
                InitialCatalog = _databaseName,
                TrustServerCertificate = true
            };

        _connectionString =
            connectionStringBuilder.ConnectionString;
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        _connectionString,

                    ["InitialAdmin:Username"] =
                        "integration-admin",

                    ["InitialAdmin:Email"] =
                        "integration-admin@test.local",

                    ["InitialAdmin:Password"] =
                        "IntegrationAdmin123!",

                    ["Jwt:Issuer"] =
                        "HotelBooking.Api.Tests",

                    ["Jwt:Audience"] =
                        "HotelBooking.Api.Tests",

                    ["Jwt:ExpirationMinutes"] =
                        "60",

                    ["Jwt:Key"] =
                        "integration-test-signing-key-at-least-32-bytes-long",

                    ["Email:Host"] =
                        "localhost",

                    ["Email:Port"] =
                        "1025",

                    ["Email:Username"] =
                        "integration-test",

                    ["Email:Password"] =
                        "integration-test",

                    ["Email:FromEmail"] =
                        "integration-test@example.com",

                    ["Booking:PaymentHoldDurationMinutes"] =
                        "15" ,
                    ["Caching:TrendingDestinationsExpirationMinutes"] = "5",
                });
        });

        builder.ConfigureServices(services =>
        {
            /*
             * Important:
             *
             * The production Infrastructure project already registered
             * ApplicationDbContext using the normal application connection
             * string.
             *
             * For integration tests we explicitly replace that registration
             * so every WebApplicationFactory gets its own isolated SQL Server
             * database.
             */

            services.RemoveAll<
                DbContextOptions<ApplicationDbContext>>();

            services.RemoveAll<
                ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(
                options =>
                {
                    options.UseSqlServer(
                        _connectionString);
                });
        });
    }

    protected override IHost CreateHost(
        IHostBuilder builder)
    {
        var host =
            base.CreateHost(builder);

        InitializeDatabaseAsync(
                host.Services)
            .GetAwaiter()
            .GetResult();

        return host;
    }

    private static async Task InitializeDatabaseAsync(
        IServiceProvider services)
    {
        await using var scope =
            services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        await dbContext.Database
            .MigrateAsync();

        var identityInitializer =
            scope.ServiceProvider
                .GetRequiredService<IdentityInitializer>();

        await identityInitializer
            .InitializeAsync();
    }
}