namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class RoomPermissions
{
    public const string Create = "Rooms.Create";
    public const string Update = "Rooms.Update";
    public const string Delete = "Rooms.Delete";
    public const string UploadImage = "Rooms.UploadImage";

    public static readonly IReadOnlyCollection<string> All =
    [
        Create,
        Update,
        Delete,
        UploadImage
    ];
}