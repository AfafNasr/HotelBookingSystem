using FluentValidation;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Hotels.CompleteHotelProfile;

public sealed class CompleteHotelProfileCommandHandler
{
    private readonly IValidator<CompleteHotelProfileCommand> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly ICurrentUserService _currentUserService;

    public CompleteHotelProfileCommandHandler(
        IValidator<CompleteHotelProfileCommand> validator,
        IHotelRepository hotelRepository,
        ICurrentUserService currentUserService)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CompleteHotelProfileResult> HandleAsync(
        CompleteHotelProfileCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new ApplicationError(
                    error.PropertyName,
                    error.ErrorMessage,
                    ErrorType.Validation))
                .ToArray();

            return new CompleteHotelProfileResult(false, errors);
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            command.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new CompleteHotelProfileResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "Hotel.NotFound",
                        "Hotel was not found.",
                        ErrorType.NotFound)
                });
        }

        var currentUserId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(currentUserId) ||
            hotel.OwnerId != currentUserId)
        {
            return new CompleteHotelProfileResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "Hotel.Forbidden",
                        "You are not allowed to manage this hotel.",
                        ErrorType.Authorization)
                });
        }

        hotel.CompleteProfile(
            command.Description,
            command.Address,
            command.Latitude,
            command.Longitude,
            DateTime.UtcNow);

        await _hotelRepository.SaveChangesAsync(cancellationToken);

        return new CompleteHotelProfileResult(
            true,
            Array.Empty<ApplicationError>());
    }
}