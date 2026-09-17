namespace HotelBooking.Api.Authentication.Register;

public sealed record RegisterRequest(
    string Username,
    string Email,
    string Password);