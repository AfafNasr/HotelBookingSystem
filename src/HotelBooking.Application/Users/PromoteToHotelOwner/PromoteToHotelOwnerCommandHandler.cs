using HotelBooking.Application.Authentication;

namespace HotelBooking.Application.Users.PromoteToHotelOwner;

public sealed class PromoteToHotelOwnerCommandHandler
{
    private readonly IIdentityService _identityService;

    public PromoteToHotelOwnerCommandHandler(
        IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<PromoteToHotelOwnerResult> HandleAsync(
        PromoteToHotelOwnerCommand command,
        CancellationToken cancellationToken)
    {
        return _identityService.PromoteToHotelOwnerAsync(
            command.UserId,
             cancellationToken);
    }
}