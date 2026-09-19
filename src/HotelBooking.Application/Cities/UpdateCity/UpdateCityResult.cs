using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Cities.UpdateCity;

public sealed record UpdateCityResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);