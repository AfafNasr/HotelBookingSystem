namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class HotelPermissions
{
    public const string Create = "Hotels.Create";
    public const string Update = "Hotels.Update";
    public const string Delete = "Hotels.Delete";
    public const string CompleteProfile = "Hotels.CompleteProfile";

    public static readonly IReadOnlyCollection<string> All =
    [
        Create,
        Update,
        Delete,
        CompleteProfile
    ];
}