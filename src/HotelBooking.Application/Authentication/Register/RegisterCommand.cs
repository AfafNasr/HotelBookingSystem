namespace HotelBooking.Application.Authentication.Register;

public sealed record RegisterCommand(
    string Username,
    string Email,
    string Password);