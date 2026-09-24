namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class CityPermissions
{
    public const string Create = "Cities.Create";
    public const string Update = "Cities.Update";
    public const string Delete = "Cities.Delete";
    public const string View = "Cities.View";

    public static readonly IReadOnlyCollection<string> All =
    [
        Create,
        Update,
        Delete,
        View
    ];
}