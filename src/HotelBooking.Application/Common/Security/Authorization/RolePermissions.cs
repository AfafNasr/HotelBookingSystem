using HotelBooking.Application.Common.Security.Authorization.Permissions;
using System.Threading.Channels;

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
                    HotelPermissions.GetAdminHotels,
                    HotelPermissions.GetAdminHotelById,

                    CityPermissions.Create,
                    CityPermissions.Update,
                    CityPermissions.Delete,
                    CityPermissions.View,

                    RoomPermissions.Create,
                    RoomPermissions.Update,
                    RoomPermissions.Delete,
                    RoomPermissions.View,
                    RoomPermissions.GetAdminRooms,

                    UserPermissions.PromoteToHotelOwner ,

                    AmenityPermissions.Create,
                    AmenityPermissions.Update,
                    AmenityPermissions.Delete,

                    BookingPermissions.ViewHotelBookings
                },

                [Roles.Customer] = new HashSet<string>()
                {
                    BookingPermissions.Create,
                    BookingPermissions.StartPayment,
                    ReviewPermissions.Create,
                    UserPermissions.ViewRecentlyVisited,
                    BookingPermissions.ViewConfirmation,
                    ReviewPermissions.View,
                    ReviewPermissions.Update,
                    ReviewPermissions.Delete,
                    BookingPermissions.ViewBooking,
                    BookingPermissions.Cancel,
                    BookingPermissions.Update

                },

                [Roles.HotelOwner] = new HashSet<string>()
                {
                    HotelPermissions.ManageAmenities,
                    RoomPermissions.Create,
                    HotelPermissions.CompleteProfile,
                    HotelPermissions.UploadImage,
                    RoomPermissions.UploadImage,
                    DealPermissions.Create,
                    RoomPermissions.View,
                    RoomPermissions.Update,
                    RoomPermissions.Delete,
                    HotelPermissions.DeleteAmenities,
                    DealPermissions.Update,
                    DealPermissions.Delete,
                    BookingPermissions.ViewHotelBookings,
                    HotelPermissions.UpdateImage,
                    HotelPermissions.DeleteImage



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