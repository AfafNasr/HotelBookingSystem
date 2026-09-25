using HotelBooking.Api.Common;
using HotelBooking.Application.Bookings.GetBookingConfirmation;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Bookings.GetBookingConfirmation;

[ApiController]
[Route("api/bookings/{bookingId:int}/confirmation")]

public sealed class GetBookingConfirmationEndpoint : ControllerBase
{
    private readonly GetBookingConfirmationQueryHandler _handler;

    public GetBookingConfirmationEndpoint(
        GetBookingConfirmationQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [Authorize(Policy = BookingPermissions.ViewConfirmation)]
    public async Task<IActionResult> Get(
        int bookingId,
        CancellationToken cancellationToken)
    {
        var result = await _handler.HandleAsync(
            new GetBookingConfirmationQuery(bookingId),
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(this, result.Errors);
        }

        var confirmation = result.Confirmation!;

        var response = new GetBookingConfirmationResponse(
            confirmation.ConfirmationNumber!,
            confirmation.HotelName,
            confirmation.HotelAddress,
            confirmation.GuestFullName,
            confirmation.GuestEmail,
            confirmation.CheckInDate,
            confirmation.CheckOutDate,
            confirmation.NumberOfNights,
            confirmation.Rooms
                .Select(room => new BookingConfirmationRoomResponse(
                    room.RoomId,
                    room.RoomType,
                    room.Description,
                    room.OriginalPricePerNight))
                .ToArray(),
            confirmation.SubtotalAmount,
            confirmation.DiscountAmount,
            confirmation.TotalAmount,
            confirmation.PaymentStatus!.Value,
            confirmation.PaymentAmount!.Value,
            confirmation.Currency!);

        return Ok(response);
    }
}