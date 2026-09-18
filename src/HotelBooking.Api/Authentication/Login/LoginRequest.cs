namespace HotelBooking.Api.Authentication.Login;

public sealed record LoginRequest(
    string Username,
    string Password);