using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Cities.GetAdminCities;

public sealed record GetAdminCitiesResult(
    bool Succeeded,
    IReadOnlyCollection<AdminCity> Cities,
    IReadOnlyCollection<ApplicationError> Errors);