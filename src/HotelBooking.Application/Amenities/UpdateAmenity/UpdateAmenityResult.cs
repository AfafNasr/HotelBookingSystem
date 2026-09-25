using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Amenities.UpdateAmenity;

public sealed record UpdateAmenityResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);