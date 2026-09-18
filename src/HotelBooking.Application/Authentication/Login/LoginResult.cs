using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Authentication.Login;

public sealed record LoginResult(
    bool Succeeded,
    AccessToken? AccessToken,
    IReadOnlyCollection<ApplicationError> Errors);