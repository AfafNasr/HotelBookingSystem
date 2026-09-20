using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Hotels.CreateHotel;

public sealed record CreateHotelResult(
    bool Succeeded,
    int? HotelId,
    IReadOnlyCollection<ApplicationError> Errors);