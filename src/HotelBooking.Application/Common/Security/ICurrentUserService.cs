namespace HotelBooking.Application.Common.Security;

public interface ICurrentUserService
{
    string? UserId { get; }

    bool IsInRole(string role);
}