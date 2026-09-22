using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Hotels.GetHotelDetails;

public sealed record GetHotelDetailsResult(
    bool Succeeded,
    HotelDetails? Hotel,
    IReadOnlyCollection<ApplicationError> Errors);