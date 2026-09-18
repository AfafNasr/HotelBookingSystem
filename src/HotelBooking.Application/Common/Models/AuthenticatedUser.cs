namespace HotelBooking.Application.Common.Models;

public sealed record AuthenticatedUser(
    string UserId,
    string Username,
    IReadOnlyCollection<string> Roles);