using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Users.PromoteToHotelOwner;

public sealed record PromoteToHotelOwnerResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);