using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.Common.Security;

public static class HotelAccessPolicy
{
    public static bool CanManage(
        Hotel hotel,
        ICurrentUserService currentUser)
    {
        return currentUser.IsInRole(Roles.Admin) ||
               hotel.OwnerId == currentUser.UserId;
    }
}