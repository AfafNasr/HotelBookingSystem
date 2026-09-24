using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.HotelAmenities.AddHotelAmenity;

public sealed record AddHotelAmenityResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);