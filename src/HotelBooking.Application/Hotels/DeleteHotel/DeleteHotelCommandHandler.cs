using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;

namespace HotelBooking.Application.Hotels.DeleteHotel;

public sealed class DeleteHotelCommandHandler
{
    private readonly IValidator<DeleteHotelCommand> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly TimeProvider _timeProvider;
    public DeleteHotelCommandHandler(
        IValidator<DeleteHotelCommand> validator,
        IHotelRepository hotelRepository,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _timeProvider = timeProvider;
    }

    public async Task<DeleteHotelResult> HandleAsync(
        DeleteHotelCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new DeleteHotelResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            command.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new DeleteHotelResult(
                false,
                [HotelErrors.NotFound]);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var hasActiveOrUpcomingBookings =
            await _hotelRepository.HasActiveOrUpcomingBookingsAsync(
                hotel.Id,
                today,
                now,
                cancellationToken);

        if (hasActiveOrUpcomingBookings)
        {
            return new DeleteHotelResult(
                false,
              [HotelErrors.HasActiveBookings]);
        }

        hotel.Delete(now);

        await _hotelRepository.SaveChangesAsync(
            cancellationToken);

        return new DeleteHotelResult(
            true,
            Array.Empty<ApplicationError>());
    }
}