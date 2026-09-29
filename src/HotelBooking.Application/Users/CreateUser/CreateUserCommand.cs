namespace HotelBooking.Application.Users.CreateUser;

public sealed record CreateUserCommand(
    string UserName,
    string Email,
    string Password);