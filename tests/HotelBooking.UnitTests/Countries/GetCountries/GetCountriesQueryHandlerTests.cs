using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Countries.GetCountries;
using HotelBooking.Domain.Cities;
using Moq;

namespace HotelBooking.UnitTests.Countries.GetCountries;

public sealed class GetCountriesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCountriesExist_ShouldReturnCountries()
    {
        // Arrange
        var countries = new List<Country>
        {
            new("JO", "Jordan"),
            new("PS", "State of Palestine")
        };

        var countryRepositoryMock = new Mock<ICountryRepository>();

        countryRepositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(countries);

        var handler = new GetCountriesQueryHandler(
            countryRepositoryMock.Object);

        // Act
        var result = await handler.HandleAsync(
            CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Contains(
            result,
            country =>
                country.Code == "JO" &&
                country.Name == "Jordan");

        Assert.Contains(
            result,
            country =>
                country.Code == "PS" &&
                country.Name == "State of Palestine");

        countryRepositoryMock.Verify(
            repository => repository.GetAllAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}