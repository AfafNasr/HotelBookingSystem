namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class UserPermissions
{
    public const string PromoteToHotelOwner ="Users.PromoteToHotelOwner";
    public const string ViewRecentlyVisited = "Hotels.ViewRecentlyVisited";

    public static readonly IReadOnlyCollection<string> All =
    [
        PromoteToHotelOwner,
        ViewRecentlyVisited
    ];
}