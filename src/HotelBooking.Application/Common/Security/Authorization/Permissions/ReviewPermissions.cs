namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class ReviewPermissions
{
    public const string Create = "Reviews.Create";

    public static readonly IReadOnlyCollection<string> All =
    [
        Create
    ];
}