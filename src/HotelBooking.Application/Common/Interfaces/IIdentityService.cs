using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<CreateCustomerResult> CreateCustomerAsync(
        string username,
        string email,
        string password );
}