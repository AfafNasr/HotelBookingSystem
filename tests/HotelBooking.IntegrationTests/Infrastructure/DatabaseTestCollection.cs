namespace HotelBooking.IntegrationTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DatabaseTestCollection
{
    public const string Name = "Database tests";
}