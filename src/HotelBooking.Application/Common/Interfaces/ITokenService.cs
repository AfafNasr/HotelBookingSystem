using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Common.Interfaces;

public interface ITokenService
{
    AccessToken CreateToken(AuthenticatedUser user);
}