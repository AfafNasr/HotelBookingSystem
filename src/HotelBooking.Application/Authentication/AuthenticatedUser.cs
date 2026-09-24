namespace HotelBooking.Application.Authentication;

public sealed record AuthenticatedUser(
    string UserId,
    string Username,
    IReadOnlyCollection<string> Roles);