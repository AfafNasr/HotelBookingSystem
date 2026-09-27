namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class UserPermissions
{
    public const string PromoteToHotelOwner ="Users.PromoteToHotelOwner";
    public const string ViewRecentlyVisited = "Hotels.ViewRecentlyVisited";

    public const string View = "Users.View";
    public const string Create = "Users.Create";
    public const string Update = "Users.Update";
    public const string Deactivate ="Users.Deactivate";
    public const string Activate ="Users.Activate";

    public static readonly IReadOnlyCollection<string> All =
    [
        PromoteToHotelOwner,
        ViewRecentlyVisited ,
        View,
        Create,
        Update,
        Deactivate,
        Activate
    ];
}