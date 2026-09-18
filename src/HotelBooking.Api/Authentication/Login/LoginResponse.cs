namespace HotelBooking.Api.Authentication.Login;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt);