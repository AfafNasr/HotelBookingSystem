using HotelBooking.Application.Common.Interfaces;

namespace HotelBooking.Application.Users.PromoteToHotelOwner;

public sealed class PromoteToHotelOwnerHandler
{
    private readonly IIdentityService _identityService;

    public PromoteToHotelOwnerHandler(
        IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<PromoteToHotelOwnerResult> HandleAsync(
        PromoteToHotelOwnerCommand command)
    {
        return _identityService.PromoteToHotelOwnerAsync(
            command.UserId);
    }
}