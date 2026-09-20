using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.HotelAmenities.AddHotelAmenity;

public sealed record AddHotelAmenityResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);