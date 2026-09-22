using HotelBooking.Api.Common;
using HotelBooking.Api.ErrorHandling;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Payments;
using HotelBooking.Application.Payments.StartPayment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Payments;

[ApiController]
[Route("api/bookings")]
public sealed class StartPaymentEndpoint : ControllerBase
{
    private readonly StartPaymentCommandHandler _handler;

    public StartPaymentEndpoint(
        StartPaymentCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("{bookingId:int}/payment")]
    [Authorize(Policy = BookingPermissions.StartPayment)]
    public async Task<IActionResult> StartPayment(
        int bookingId,
        CancellationToken cancellationToken)
    {
        var command = new StartPaymentCommand(
            bookingId);

        var result = await _handler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        var response = new StartPaymentResponse(
            result.PaymentId!.Value,
            result.ClientSecret!);

        return Ok(response);
    }
}
