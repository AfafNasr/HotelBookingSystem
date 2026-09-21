namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class BookingPermissions
{
    public const string Create = "Bookings.Create";

    public static readonly IReadOnlyCollection<string> All =
    [
        Create
    ];
}