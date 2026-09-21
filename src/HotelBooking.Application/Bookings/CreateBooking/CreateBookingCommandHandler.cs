using FluentValidation;
using HotelBooking.Application.Bookings.Pricing;
using HotelBooking.Application.Common;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Domain.Bookings;

namespace HotelBooking.Application.Bookings.CreateBooking;

public sealed class CreateBookingCommandHandler
{
    private readonly IValidator<CreateBookingCommand> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IDealRepository _dealRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly BookingPricingCalculator _pricingCalculator;
    private readonly IBookingConcurrencyManager _bookingConcurrencyManager;

    public CreateBookingCommandHandler(
        IValidator<CreateBookingCommand> validator,
        IHotelRepository hotelRepository,
        IRoomRepository roomRepository,
        IBookingRepository bookingRepository,
        IDealRepository dealRepository,
        ICurrentUserService currentUserService,
        BookingPricingCalculator pricingCalculator,
        IBookingConcurrencyManager bookingConcurrencyManager)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _roomRepository = roomRepository;
        _bookingRepository = bookingRepository;
        _dealRepository = dealRepository;
        _currentUserService = currentUserService;
        _pricingCalculator = pricingCalculator;
        _bookingConcurrencyManager = bookingConcurrencyManager;
    }

    public async Task<CreateBookingResult> HandleAsync(
        CreateBookingCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new ApplicationError(
                    error.PropertyName,
                    error.ErrorMessage,
                    ErrorType.Validation))
                .ToArray();

            return new CreateBookingResult(false, null, errors);
        }

        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new CreateBookingResult(
                false,
                null,
                [
                    new ApplicationError(
                        "Authentication.Required",
                        "The authenticated user could not be identified.",
                        ErrorType.Authentication)
                ]);
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            command.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new CreateBookingResult(
                false,
                null,
                [
                    new ApplicationError(
                        "Hotel.NotFound",
                        "The selected hotel was not found.",
                        ErrorType.NotFound)
                ]);
        }

        var rooms = await _roomRepository.GetByIdsAsync(
            command.RoomIds,
            cancellationToken);

        // The booking is atomic. Every requested physical room must exist;
        // otherwise no partial booking should be created.
        if (rooms.Count != command.RoomIds.Count)
        {
            return new CreateBookingResult(
                false,
                null,
                [
                    new ApplicationError(
                        "Booking.RoomNotFound",
                        "One or more selected rooms were not found.",
                        ErrorType.NotFound)
                ]);
        }

        // A single booking belongs to exactly one hotel.
        if (rooms.Any(room => room.HotelId != command.HotelId))
        {
            return new CreateBookingResult(
                false,
                null,
                [
                    new ApplicationError(
                        "Booking.RoomHotelMismatch",
                        "All selected rooms must belong to the selected hotel.",
                        ErrorType.Validation)
                ]);
        }

        var now = DateTime.UtcNow;


        var deals = await _dealRepository.GetOverlappingDealsAsync(
            command.HotelId,
            command.CheckInDate,
            command.CheckOutDate,
            cancellationToken);

        decimal totalAmount = 0;

        foreach (var room in rooms)
        {
            totalAmount += _pricingCalculator.CalculateRoomTotal(
                room.PricePerNight,
                command.CheckInDate,
                command.CheckOutDate,
                deals);
        }

        // Temporary value for now.
        // The hold duration will become an explicit booking policy/configuration.
        var expiresAt = now.AddMinutes(15);

        var booking = new Booking(
            userId,
            command.HotelId,
            command.CheckInDate,
            command.CheckOutDate,
            expiresAt,
            command.GuestFullName,
            command.GuestEmail,
            command.GuestPhoneNumber,
            command.SpecialRequests,
            totalAmount,
            now);

        return await _bookingConcurrencyManager.ExecuteWithRoomLocksAsync(
       command.RoomIds,
       async ct =>
       {
           // Availability must be checked after acquiring the room locks.
           // This prevents two concurrent requests from successfully
           // reserving the same physical room for overlapping dates.
           var unavailableRoomIds =
               await _bookingRepository.GetUnavailableRoomIdsAsync(
                   command.RoomIds,
                   command.CheckInDate,
                   command.CheckOutDate,
                   now,
                   ct);

           if (unavailableRoomIds.Count > 0)
           {
               return new CreateBookingResult(
                   false,
                   null,
                   [
                       new ApplicationError(
                        "Booking.RoomUnavailable",
                        $"One or more selected rooms are unavailable: " +
                        $"{string.Join(", ", unavailableRoomIds)}.",
                        ErrorType.Conflict)
                   ]);
           }

           var booking = new Booking(
               userId,
               command.HotelId,
               command.CheckInDate,
               command.CheckOutDate,
               expiresAt,
               command.GuestFullName,
               command.GuestEmail,
               command.GuestPhoneNumber,
               command.SpecialRequests,
               totalAmount,
               now);

           foreach (var room in rooms)
           {
               booking.AddRoom(
                   room.Id,
                   room.PricePerNight);
           }

           _bookingRepository.Add(booking);

           await _bookingRepository.SaveChangesAsync(ct);

           return new CreateBookingResult(
               true,
               booking.Id,
               []);
       },
       cancellationToken);

    }
}