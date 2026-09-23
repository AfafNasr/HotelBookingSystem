using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
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
                [
                    new ApplicationError(
                        "Authentication.Required",
                        "The authenticated user could not be identified.",
                        ErrorType.Authentication)
                ]);
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
                [
                    new ApplicationError(
                        "Booking.NotFound",
                        "The booking was not found.",
                        ErrorType.NotFound)
                ]);
        }

        if (confirmation.BookingStatus != BookingStatus.Confirmed ||
            confirmation.PaymentStatus != PaymentStatus.Succeeded ||
            string.IsNullOrWhiteSpace(confirmation.ConfirmationNumber))
        {
            return new GetBookingConfirmationResult(
                false,
                null,
                [
                    new ApplicationError(
                        "Booking.ConfirmationNotReady",
                        "The booking confirmation is not available until payment succeeds.",
                        ErrorType.Conflict)
                ]);
        }

        var numberOfNights =
             confirmation.CheckOutDate.DayNumber -confirmation.CheckInDate.DayNumber;

        var subtotalAmount = confirmation.Rooms.Sum(
            room => room.OriginalPricePerNight * numberOfNights);

        var discountAmount =
            subtotalAmount - confirmation.TotalAmount;

        var completedConfirmation = confirmation with
        {
            NumberOfNights = numberOfNights,
            SubtotalAmount = subtotalAmount,
            DiscountAmount = discountAmount
        };

        return new GetBookingConfirmationResult(
            true,
            completedConfirmation,
            []);
    }
}