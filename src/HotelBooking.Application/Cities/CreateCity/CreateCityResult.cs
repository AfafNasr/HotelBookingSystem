using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Cities.CreateCity;

public sealed record CreateCityResult(
    bool Succeeded,
    int? CityId,
    IReadOnlyCollection<ApplicationError> Errors);