using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Authentication.Register;

public sealed record RegisterResult(
    bool Succeeded,
    string? UserId,
    IReadOnlyCollection<ApplicationError> Errors);