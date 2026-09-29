using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Common.Storage;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Hotels.DeleteHotelImage;

public sealed class DeleteHotelImageCommandHandler
{
    private readonly IValidator<DeleteHotelImageCommand> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly IHotelImageRepository _hotelImageRepository;
    private readonly IImageStorageService _imageStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteHotelImageCommandHandler> _logger;

    public DeleteHotelImageCommandHandler(
        IValidator<DeleteHotelImageCommand> validator,
        IHotelRepository hotelRepository,
        IHotelImageRepository hotelImageRepository,
        IImageStorageService imageStorageService,
        ICurrentUserService currentUserService,
        ILogger<DeleteHotelImageCommandHandler> logger)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _hotelImageRepository = hotelImageRepository;
        _imageStorageService = imageStorageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<DeleteHotelImageResult> HandleAsync(
        DeleteHotelImageCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(
                command,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return new DeleteHotelImageResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var hotel =
            await _hotelRepository.GetByIdAsync(
                command.HotelId,
                cancellationToken);

        if (hotel is null)
        {
            return new DeleteHotelImageResult(
                false,
                [HotelErrors.NotFound]);
        }

        if (!HotelAccessPolicy.CanManage(
                hotel,
                _currentUserService))
        {
            return new DeleteHotelImageResult(
                false,
                [HotelErrors.ManagementForbidden]);
        }

        var image =
            await _hotelImageRepository.GetByIdAsync(
                command.ImageId,
                cancellationToken);

        if (image is null ||
            image.HotelId != hotel.Id)
        {
            return new DeleteHotelImageResult(
                false,
                [HotelImageErrors.NotFound]);
        }

        var storageKey =
            image.StorageKey;

        _hotelImageRepository.Remove(
            image);

        await _hotelImageRepository.SaveChangesAsync(
            cancellationToken);

        /*
         * The database is now authoritative:
         * the image no longer belongs to the hotel.
         *
         * Azure cleanup is best-effort. A failure here produces
         * an orphan blob, not a broken database reference.
         */
        try
        {
            await _imageStorageService.DeleteAsync(
                ImageContainer.HotelImages,
                storageKey,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to delete hotel image blob after removing DB record. StorageKey: {StorageKey}",
                storageKey);
        }

        return new DeleteHotelImageResult(
            true,
            []);
    }
}

public sealed record DeleteHotelImageResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);