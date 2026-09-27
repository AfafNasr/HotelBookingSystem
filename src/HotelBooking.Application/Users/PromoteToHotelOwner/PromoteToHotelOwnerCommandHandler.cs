using HotelBooking.Application.Authentication;
using HotelBooking.Application.Common.Errors;
using Microsoft.Extensions.Logging;


namespace HotelBooking.Application.Users.PromoteToHotelOwner;

public sealed class PromoteToHotelOwnerCommandHandler
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<PromoteToHotelOwnerCommandHandler> _logger;

    public PromoteToHotelOwnerCommandHandler(
        IIdentityService identityService,
        ILogger<PromoteToHotelOwnerCommandHandler> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<PromoteToHotelOwnerResult> HandleAsync(
        PromoteToHotelOwnerCommand command,
        CancellationToken cancellationToken)
    {
        var result =
            await _identityService.PromoteToHotelOwnerAsync(
                command.UserId,
                cancellationToken);

        if (result.Succeeded)
        {
            UserLog.UserPromoted(
                _logger,
                command.UserId);
        }

        return result;
    }
}

public sealed record PromoteToHotelOwnerResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);