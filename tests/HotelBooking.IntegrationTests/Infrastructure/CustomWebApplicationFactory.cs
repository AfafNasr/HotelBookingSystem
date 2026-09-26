using HotelBooking.Infrastructure.Identity;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace HotelBooking.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Email:Host"] = "localhost",
                    ["Email:Port"] = "1025",
                    ["Email:Username"] = "integration-test",
                    ["Email:Password"] = "integration-test",
                    ["Email:FromEmail"] = "integration-test@example.com"
                });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        InitializeDatabaseAsync(host.Services)
            .GetAwaiter()
            .GetResult();

        return host;
    }

    private static async Task InitializeDatabaseAsync(
        IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var identityInitializer =
            scope.ServiceProvider.GetRequiredService<IdentityInitializer>();

        await identityInitializer.InitializeAsync();
    }
}
