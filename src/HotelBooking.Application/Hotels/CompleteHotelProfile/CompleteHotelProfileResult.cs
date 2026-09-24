using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Hotels.CompleteHotelProfile;

public sealed record CompleteHotelProfileResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);