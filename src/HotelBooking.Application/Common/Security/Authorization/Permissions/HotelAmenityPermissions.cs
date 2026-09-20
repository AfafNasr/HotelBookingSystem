namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class HotelAmenityPermissions
{
    public const string Manage =
        "HotelAmenities.Manage";

    public static readonly IReadOnlyCollection<string> All =
    [
        Manage
    ];
}