using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Hotels.CompleteHotelProfile;

public sealed record CompleteHotelProfileResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);