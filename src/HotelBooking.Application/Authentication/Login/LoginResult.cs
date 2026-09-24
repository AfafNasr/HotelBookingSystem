using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Authentication.Login;

public sealed record LoginResult(
    bool Succeeded,
    AccessToken? AccessToken,
    IReadOnlyCollection<ApplicationError> Errors);