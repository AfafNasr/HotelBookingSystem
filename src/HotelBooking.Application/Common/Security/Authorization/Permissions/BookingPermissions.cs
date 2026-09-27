namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class BookingPermissions
{
    public const string Create = "Bookings.Create";
    public const string StartPayment = "Bookings.StartPayment";
    public const string ViewConfirmation = "Bookings.ViewConfirmation";
    public const string ViewBooking = "Bookings.ViewBooking";
    public const string ViewHotelBookings ="Bookings.ViewHotelBookings";
    public const string Cancel = "Bookings.Cancel";
    public const string Update = "Bookings.Update";

    public static readonly IReadOnlyCollection<string> All =
    [
        Create,
        StartPayment,
        ViewConfirmation,
        ViewBooking,
        ViewHotelBookings,
        Cancel,
        Update
    ];
}