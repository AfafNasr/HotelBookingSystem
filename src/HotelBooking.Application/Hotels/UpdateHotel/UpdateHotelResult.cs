using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Hotels.UpdateHotel;

public sealed record UpdateHotelResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);