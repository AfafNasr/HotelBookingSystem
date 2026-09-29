namespace HotelBooking.Application.Users.UpdateUser;

public sealed record UpdateUserCommand(
    string UserId,
    string UserName,
    string Email);