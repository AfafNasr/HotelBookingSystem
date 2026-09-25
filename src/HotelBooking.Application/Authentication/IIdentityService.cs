using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Users.PromoteToHotelOwner;

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

    Task<bool> IsUserInRoleAsync(
    string userId,
    string role);
}