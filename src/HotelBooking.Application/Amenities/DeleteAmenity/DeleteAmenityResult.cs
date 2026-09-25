using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Amenities.DeleteAmenity;

public sealed record DeleteAmenityResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);