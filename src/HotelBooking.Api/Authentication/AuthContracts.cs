namespace HotelBooking.Api.Authentication;

public sealed record RegisterRequest(
    string Username,
    string Email,
    string Password);

public sealed record RegisterResponse(
    string Id,
    string Username,
    string Email);

public sealed record LoginRequest(
    string Username,
    string Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt);