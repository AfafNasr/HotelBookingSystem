using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.HotelAmenities.DeleteHotelAmenity;

public sealed record DeleteHotelAmenityResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);