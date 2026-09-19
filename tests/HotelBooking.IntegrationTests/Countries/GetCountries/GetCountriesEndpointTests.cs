using HotelBooking.Api.Countries.GetCountries;
using HotelBooking.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace HotelBooking.IntegrationTests.Countries.GetCountries;

[Collection(DatabaseTestCollection.Name)]
public sealed class GetCountriesEndpointTests
{
    [Fact]
    public async Task GetCountries_ShouldReturnSeededCountries()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/countries");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var countries =
            await response.Content
                .ReadFromJsonAsync<List<CountryResponse>>();

        Assert.NotNull(countries);
        Assert.NotEmpty(countries);

        Assert.Contains(
            countries,
            country =>
                country.Code == "JO" &&
                country.Name == "Jordan");

        Assert.Contains(
            countries,
            country =>
                country.Code == "PS" &&
                country.Name == "State of Palestine");

        var countryNames = countries
    .Select(country => country.Name)
    .ToArray();

        var sortedCountryNames = countryNames
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(
            sortedCountryNames,
            countryNames);
    }
}