using HotelBooking.Api.Common;
using HotelBooking.Application.Bookings.GetBookingConfirmation;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Bookings.GetBookingConfirmation;

[ApiController]
[Route("api/bookings/{bookingId:int}/confirmation/pdf")]
[Authorize(Policy = BookingPermissions.ViewConfirmation)]
public sealed class GetBookingConfirmationPdfEndpoint : ControllerBase
{
    private readonly GetBookingConfirmationQueryHandler _handler;
    private readonly IBookingConfirmationPdfGenerator _pdfGenerator;

    public GetBookingConfirmationPdfEndpoint(
        GetBookingConfirmationQueryHandler handler,
        IBookingConfirmationPdfGenerator pdfGenerator)
    {
        _handler = handler;
        _pdfGenerator = pdfGenerator;
    }

    [HttpGet]
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

        var pdf = _pdfGenerator.Generate(confirmation);

        var fileName =
            $"booking-confirmation-{confirmation.ConfirmationNumber}.pdf";

        return File(
            pdf,
            "application/pdf",
            fileName);
    }
}