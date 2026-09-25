using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Hotels.DeleteHotel;

public sealed record DeleteHotelResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);