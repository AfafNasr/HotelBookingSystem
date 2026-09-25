using HotelBooking.Application.Countries.GetCountries;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Countries.GetCountries;

[ApiController]
[Route("api/countries")]
public sealed class GetCountriesEndpoint : ControllerBase
{
    private readonly GetCountriesQueryHandler _handler;

    public GetCountriesEndpoint(
        GetCountriesQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CountryResponse>>> Get(
        CancellationToken cancellationToken)
    {
        var countries = await _handler.HandleAsync(
            cancellationToken);

        var response = countries
            .Select(country => new CountryResponse(
                country.Code,
                country.Name))
            .ToArray();

        return Ok(response);
    }
}

public sealed record CountryResponse(
    string Code,
    string Name);