namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class Permissions
{
    public static readonly IReadOnlyCollection<string> All =
    [
        .. HotelPermissions.All,
        .. CityPermissions.All,
        .. RoomPermissions.All,
        ..UserPermissions.All,
        ..AmenityPermissions.All,
        ..DealPermissions.All,
        ..BookingPermissions.All,
        ..ReviewPermissions.All
    ];
}