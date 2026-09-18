namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class Permissions
{
    public static readonly IReadOnlyCollection<string> All =
    [
        .. HotelPermissions.All,
        .. CityPermissions.All,
        .. RoomPermissions.All
    ];
}