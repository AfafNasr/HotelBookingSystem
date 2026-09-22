namespace HotelBooking.Application.Countries.GetCountries;

public sealed class GetCountriesQueryHandler
{
    private readonly ICountryRepository _countryRepository;

    public GetCountriesQueryHandler(
        ICountryRepository countryRepository)
    {
        _countryRepository = countryRepository;
    }

    public async Task<IReadOnlyList<CountryResult>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var countries = await _countryRepository.GetAllAsync(
            cancellationToken);

        return countries
            .Select(country => new CountryResult(
                country.Code,
                country.Name))
            .ToArray();
    }
}