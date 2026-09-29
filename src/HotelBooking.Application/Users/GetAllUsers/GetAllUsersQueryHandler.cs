using HotelBooking.Application.Authentication;

namespace HotelBooking.Application.Users.GetAllUsers;

public sealed class GetAllUsersQueryHandler
{
    private readonly IIdentityService _identityService;

    public GetAllUsersQueryHandler(
        IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<IReadOnlyCollection<AdminUserItem>> HandleAsync(
        CancellationToken cancellationToken)
    {
        return _identityService.GetAllUsersAsync(
            cancellationToken);
    }
}