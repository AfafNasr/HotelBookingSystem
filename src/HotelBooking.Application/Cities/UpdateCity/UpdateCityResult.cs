using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Cities.UpdateCity;

public sealed record UpdateCityResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);