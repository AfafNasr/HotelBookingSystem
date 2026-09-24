using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Hotels.UpdateHotel;

public sealed record UpdateHotelResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);