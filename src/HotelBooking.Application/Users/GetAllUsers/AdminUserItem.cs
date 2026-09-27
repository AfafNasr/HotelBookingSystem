namespace HotelBooking.Application.Users.GetAllUsers;

public sealed record AdminUserItem(
    string Id,
    string UserName,
    string Email,
    IReadOnlyCollection<string> Roles,
    bool IsActive,
    DateTime? DeactivatedAt);