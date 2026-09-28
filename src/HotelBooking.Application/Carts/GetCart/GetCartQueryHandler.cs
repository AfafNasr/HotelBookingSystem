using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Security;

namespace HotelBooking.Application.Carts.GetCart;

public sealed class GetCartQueryHandler
{
    private readonly ICartQuery _cartQuery;
    private readonly ICurrentUserService _currentUserService;

    public GetCartQueryHandler(
        ICartQuery cartQuery,
        ICurrentUserService currentUserService)
    {
        _cartQuery = cartQuery;
        _currentUserService = currentUserService;
    }

    public async Task<GetCartResult> HandleAsync(
        CancellationToken cancellationToken)
    {
        var userId =
            _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new GetCartResult(
                false,
                null,
                [AuthenticationErrors.Required]);
        }

        var cart =
            await _cartQuery.GetAsync(
                userId,
                cancellationToken);

        return new GetCartResult(
            true,
            cart,
            []);
    }
}

public sealed record GetCartResult(
    bool Succeeded,
    CartDetails? Cart,
    IReadOnlyCollection<ApplicationError> Errors);