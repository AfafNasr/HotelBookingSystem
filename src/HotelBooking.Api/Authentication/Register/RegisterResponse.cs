namespace HotelBooking.Api.Authentication.Register;

public sealed record RegisterResponse(
    string Id,
    string Username,
    string Email);