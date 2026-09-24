using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Hotels.CreateHotel;

public sealed record CreateHotelResult(
    bool Succeeded,
    int? HotelId,
    IReadOnlyCollection<ApplicationError> Errors);