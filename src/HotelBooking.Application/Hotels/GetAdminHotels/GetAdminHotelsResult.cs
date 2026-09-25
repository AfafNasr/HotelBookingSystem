using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Hotels.GetAdminHotels;

public sealed record GetAdminHotelsResult(
    bool Succeeded,
    IReadOnlyCollection<AdminHotel> Hotels,
    IReadOnlyCollection<ApplicationError> Errors);