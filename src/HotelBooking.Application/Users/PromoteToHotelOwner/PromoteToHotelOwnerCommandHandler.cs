using HotelBooking.Application.Authentication;
using HotelBooking.Application.Common.Errors;

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

public sealed record PromoteToHotelOwnerResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);