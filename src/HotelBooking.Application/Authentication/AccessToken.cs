namespace HotelBooking.Application.Authentication;

public sealed record AccessToken(
    string Token,
    DateTimeOffset ExpiresAt);