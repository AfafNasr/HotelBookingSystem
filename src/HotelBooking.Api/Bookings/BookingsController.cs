using HotelBooking.Api.Common;
using HotelBooking.Application.Bookings.CreateBooking;
using HotelBooking.Application.Bookings.GetBookingConfirmation;
using HotelBooking.Application.Bookings.GetMyBookings;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Domain.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace HotelBooking.Api.Bookings;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly CreateBookingCommandHandler _createHandler;
    private readonly GetBookingConfirmationQueryHandler _confirmationHandler;
    private readonly IBookingConfirmationPdfGenerator _pdfGenerator;
    private readonly GetMyBookingsQueryHandler _myBookingsHandler;

    public BookingsController(
      CreateBookingCommandHandler createHandler,
      GetBookingConfirmationQueryHandler confirmationHandler,
      GetMyBookingsQueryHandler myBookingsHandler,
      IBookingConfirmationPdfGenerator pdfGenerator)
    {
        _createHandler = createHandler;
        _confirmationHandler = confirmationHandler;
        _myBookingsHandler = myBookingsHandler;
        _pdfGenerator = pdfGenerator;
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

        var result = await _createHandler.HandleAsync(
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

    [HttpGet("{bookingId:int}/confirmation")]
    [Authorize(Policy = BookingPermissions.ViewConfirmation)]
    public async Task<IActionResult> GetConfirmation(
        int bookingId,
        CancellationToken cancellationToken)
    {
        var result = await _confirmationHandler.HandleAsync(
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

    [HttpGet("{bookingId:int}/confirmation/pdf")]
    [Authorize(Policy = BookingPermissions.ViewConfirmation)]
    public async Task<IActionResult> GetConfirmationPdf(
        int bookingId,
        CancellationToken cancellationToken)
    {
        var result = await _confirmationHandler.HandleAsync(
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

    [HttpGet]
    [Authorize(Policy = BookingPermissions.ViewBooking)]
    public async Task<IActionResult> GetMyBookings(
    CancellationToken cancellationToken)
    {
        var result = await _myBookingsHandler.HandleAsync(
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        var response = result.Bookings
            .Select(booking => new GetMyBookingResponse(
                booking.BookingId,
                booking.HotelName,
                booking.CheckInDate,
                booking.CheckOutDate,
                booking.TotalAmount,
                booking.Status,
                booking.ConfirmationNumber,
                booking.CreatedAt))
            .ToArray();

        return Ok(response);
    }

}
