namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class BookingPermissions
{
    public const string Create = "Bookings.Create";
    public const string StartPayment = "Bookings.StartPayment";
    public const string ViewConfirmation = "Bookings.ViewConfirmation";

    public static readonly IReadOnlyCollection<string> All =
    [
        Create,
        StartPayment,
        ViewConfirmation
    ];
}