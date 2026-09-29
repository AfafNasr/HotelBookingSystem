using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Users.ActivateUser;
using HotelBooking.Application.Users.DeactivateUser;
using HotelBooking.Application.Users.GetAllUsers;
using HotelBooking.Application.Users.PromoteToHotelOwner;
using HotelBooking.Application.Users.UpdateUser;

namespace HotelBooking.Application.Authentication;

public interface IIdentityService
{
    Task<RegisterResult> CreateCustomerAsync(
        string username,
        string email,
        string password ,
        CancellationToken cancellationToken);

    Task<AuthenticatedUser?> AuthenticateAsync(
        string username,
        string password);

    Task<PromoteToHotelOwnerResult> PromoteToHotelOwnerAsync(
    string userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AdminUserItem>> GetAllUsersAsync(
    CancellationToken cancellationToken);

    Task<bool> IsUserInRoleAsync(
    string userId,
    string role);

    Task<UpdateUserResult> UpdateUserAsync(
    string userId,
    string userName,
    string email,
    CancellationToken cancellationToken);

    Task<DeactivateUserResult> DeactivateUserAsync(
    string userId,
    DateTime deactivatedAt,
    CancellationToken cancellationToken);

    Task<ActivateUserResult> ActivateUserAsync(
        string userId,
        CancellationToken cancellationToken);
}