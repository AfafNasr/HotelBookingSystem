namespace HotelBooking.Application.Authorization.Permissions;

public static class UserPermissions
{
    public const string PromoteToHotelOwner =
        "Users.PromoteToHotelOwner";

    public static readonly IReadOnlyCollection<string> All =
    [
        PromoteToHotelOwner
    ];
}