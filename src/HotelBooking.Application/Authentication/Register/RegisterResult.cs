using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Authentication.Register;

public sealed record RegisterResult(
    bool Succeeded,
    string? UserId,
    IReadOnlyCollection<ApplicationError> Errors);