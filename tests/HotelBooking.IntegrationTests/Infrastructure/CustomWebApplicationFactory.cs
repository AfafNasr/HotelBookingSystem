using HotelBooking.Infrastructure.BackgroundJobs;
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

        builder.ConfigureAppConfiguration(
            (_, configuration) =>
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
                            "15",

                        ["Caching:TrendingDestinationsExpirationMinutes"] =
                            "5",

                        ["RateLimiting:Login:PermitLimit"] =
                            "5",

                        ["RateLimiting:Login:WindowSeconds"] =
                            "60",

                        ["Geoapify:BaseUrl"] =
                            "https://api.geoapify.com/",

                        ["Geoapify:ApiKey"] =
                            "integration-test-api-key",
                        ["Stripe:SecretKey"] =
                              "sk_test_integration",

                         ["Stripe:WebhookSecret"] =
                                 "whsec_integration",
                    });
            });

        builder.ConfigureServices(services =>
        {
            /*
             * Disable the booking expiration background worker.
             *
             * Integration tests explicitly control their data and timing.
             * Running this worker during tests can introduce race conditions
             * and can keep accessing the test database while the host is
             * shutting down.
             */
            var bookingWorkerDescriptor =
                services.SingleOrDefault(
                    descriptor =>
                        descriptor.ServiceType ==
                            typeof(IHostedService) &&
                        descriptor.ImplementationType ==
                            typeof(BookingExpirationWorker));

            if (bookingWorkerDescriptor is not null)
            {
                services.Remove(
                    bookingWorkerDescriptor);
            }

            var outboxWorkerDescriptor =
    services.SingleOrDefault(
        descriptor =>
            descriptor.ServiceType ==
                typeof(IHostedService) &&
            descriptor.ImplementationType ==
                typeof(OutboxProcessorWorker));

            if (outboxWorkerDescriptor is not null)
            {
                services.Remove(
                    outboxWorkerDescriptor);
            }

            /*
             * The production Infrastructure project already registers
             * ApplicationDbContext using the normal application database.
             *
             * For integration tests we replace that registration so every
             * CustomWebApplicationFactory receives an isolated SQL Server
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

    protected override void Dispose(
        bool disposing)
    {
        /*
         * Dispose the web host first.
         *
         * This closes DbContext / SQL connections before we attempt
         * to drop the isolated test database.
         */
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        DeleteDatabase();
    }

    private void DeleteDatabase()
    {
        /*
         * We cannot connect to the database that we are about to drop,
         * so connect to SQL Server's master database instead.
         */
        var connectionStringBuilder =
            new SqlConnectionStringBuilder(
                _connectionString)
            {
                InitialCatalog = "master"
            };

        using var connection =
            new SqlConnection(
                connectionStringBuilder.ConnectionString);

        connection.Open();

        using var command =
            connection.CreateCommand();

        /*
         * _databaseName is generated internally from a GUID,
         * so it is not user-controlled input.
         *
         * SINGLE_USER WITH ROLLBACK IMMEDIATE ensures any remaining
         * test connections are terminated before DROP DATABASE.
         */
        command.CommandText = $"""
            IF DB_ID(N'{_databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_databaseName}]
                    SET SINGLE_USER
                    WITH ROLLBACK IMMEDIATE;

                DROP DATABASE [{_databaseName}];
            END
            """;

        command.ExecuteNonQuery();
    }
}