using HotelBooking.Api.Common;
using HotelBooking.Api.ErrorHandling;
using HotelBooking.Application.Bookings.CreateBooking;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Domain.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Bookings;

[ApiController]
[Route("api/bookings")]
public sealed class CreateBookingEndpoint : ControllerBase
{
    private readonly CreateBookingCommandHandler _handler;

    public CreateBookingEndpoint(
        CreateBookingCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [Authorize(Policy = BookingPermissions.Create)]
    public async Task<IActionResult> Create(
        CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateBookingCommand(
            request.HotelId,
            request.RoomIds,
            request.CheckInDate,
            request.CheckOutDate,
            request.GuestFullName,
            request.GuestEmail,
            request.GuestPhoneNumber,
            request.SpecialRequests);

        var result = await _handler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        var response = new CreateBookingResponse(
            result.BookingId!.Value,
            BookingStatus.PendingPayment);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }
}

