using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using HotelBooking.Application.Common.Security;

namespace HotelBooking.Api.Authentication;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?
            .User
            .FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsInRole(string role) 
    {
        return _httpContextAccessor.HttpContext?
            .User
            .IsInRole(role) == true;
    }
}