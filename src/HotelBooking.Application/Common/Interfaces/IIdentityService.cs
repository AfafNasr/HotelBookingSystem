using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<RegisterResult> CreateCustomerAsync(
        string username,
        string email,
        string password );

    Task<AuthenticatedUser?> AuthenticateAsync(
        string username,
        string password);
}