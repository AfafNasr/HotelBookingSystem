using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Cities.CreateCity;

public sealed record CreateCityResult(
    bool Succeeded,
    int? CityId,
    IReadOnlyCollection<ApplicationError> Errors);