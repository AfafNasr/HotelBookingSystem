using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Payments;

namespace HotelBooking.Application.Bookings.GetBookingConfirmation;

public sealed class GetBookingConfirmationQueryHandler
{
    private readonly IBookingConfirmationQuery _bookingConfirmationQuery;
    private readonly ICurrentUserService _currentUserService;

    public GetBookingConfirmationQueryHandler(
        IBookingConfirmationQuery bookingConfirmationQuery,
        ICurrentUserService currentUserService)
    {
        _bookingConfirmationQuery = bookingConfirmationQuery;
        _currentUserService = currentUserService;
    }

    public async Task<GetBookingConfirmationResult> HandleAsync(
        GetBookingConfirmationQuery query,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new GetBookingConfirmationResult(
                false,
                null,
              [AuthenticationErrors.Required]);
        }

        var confirmation = await _bookingConfirmationQuery.GetAsync(
            query.BookingId,
            cancellationToken);

        if (confirmation is null ||
            confirmation.UserId != userId)
        {
            return new GetBookingConfirmationResult(
                false,
                null,
                [BookingErrors.NotFound]);
        }

        if (confirmation.BookingStatus != BookingStatus.Confirmed ||
            confirmation.PaymentStatus != PaymentStatus.Succeeded ||
            string.IsNullOrWhiteSpace(confirmation.ConfirmationNumber))
        {
            return new GetBookingConfirmationResult(
                false,
                null,
               [BookingErrors.ConfirmationNotReady]);
        }

        return new GetBookingConfirmationResult(
            true,
            confirmation,
            []);
    }
}

public sealed record GetBookingConfirmationResult(
    bool Succeeded,
    BookingConfirmation? Confirmation,
    IReadOnlyCollection<ApplicationError> Errors);