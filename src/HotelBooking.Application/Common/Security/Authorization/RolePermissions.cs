using HotelBooking.Application.Authorization.Permissions;
using HotelBooking.Application.Common.Security.Authorization.Permissions;

namespace HotelBooking.Application.Common.Security.Authorization;

public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>>
        PermissionsByRole =
            new Dictionary<string, IReadOnlySet<string>>
            {
                [Roles.Admin] = new HashSet<string>
                {
                    HotelPermissions.Create,
                    HotelPermissions.Update,
                    HotelPermissions.Delete,

                    CityPermissions.Create,
                    CityPermissions.Update,
                    CityPermissions.Delete,

                    RoomPermissions.Create,
                    RoomPermissions.Update,
                    RoomPermissions.Delete,

                    UserPermissions.PromoteToHotelOwner ,

                    AmenityPermissions.Create,
                    AmenityPermissions.Update,
                    AmenityPermissions.Delete
                },

                [Roles.Customer] = new HashSet<string>() { },

                [Roles.HotelOwner] = new HashSet<string>()
                {
                    HotelAmenityPermissions.Manage

                }
            };

    public static bool HasPermission(
        string role,
        string permission)
    {
        return PermissionsByRole.TryGetValue(role, out var permissions)
            && permissions.Contains(permission);
    }
}