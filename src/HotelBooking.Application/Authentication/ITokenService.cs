using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Authentication;

public interface ITokenService
{
    AccessToken CreateToken(AuthenticatedUser user);
}