namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class AmenityPermissions
{
    public const string Create = "Amenities.Create";
    public const string Update = "Amenities.Update";
    public const string Delete = "Amenities.Delete";

    public static readonly IReadOnlyCollection<string> All =
    [
        Create,
        Update,
        Delete
    ];
}