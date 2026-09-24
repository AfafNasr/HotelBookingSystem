using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Amenities.CreateAmenity;

public sealed record CreateAmenityResult(
    bool Succeeded,
    int? AmenityId,
    IReadOnlyCollection<ApplicationError> Errors);