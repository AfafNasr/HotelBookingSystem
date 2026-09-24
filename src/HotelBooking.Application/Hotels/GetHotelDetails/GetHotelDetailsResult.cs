using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Hotels.GetHotelDetails;

public sealed record GetHotelDetailsResult(
    bool Succeeded,
    HotelDetails? Hotel,
    IReadOnlyCollection<ApplicationError> Errors);