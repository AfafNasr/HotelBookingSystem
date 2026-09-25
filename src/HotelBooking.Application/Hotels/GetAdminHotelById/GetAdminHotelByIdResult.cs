using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Hotels.GetAdminHotelById;

public sealed record GetAdminHotelByIdResult(
    bool Succeeded,
    AdminHotelDetails? Hotel,
    IReadOnlyCollection<ApplicationError> Errors);