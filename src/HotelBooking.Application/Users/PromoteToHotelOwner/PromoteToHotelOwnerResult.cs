using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Users.PromoteToHotelOwner;

public sealed record PromoteToHotelOwnerResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);