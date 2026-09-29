namespace HotelBooking.Api.Users;

public sealed record CreateUserRequest(
    string UserName,
    string Email,
    string Password);

public sealed record CreateUserResponse(
    string UserId);

public sealed record UpdateUserRequest(
    string UserName,
    string Email);