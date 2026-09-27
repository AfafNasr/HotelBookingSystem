using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Hotels.UploadHotelImage;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Hotels.UpdateHotelImage;

public sealed class UpdateHotelImageCommandHandler
{
    private readonly IValidator<UpdateHotelImageCommand> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly IHotelImageRepository _hotelImageRepository;
    private readonly IImageStorageService _imageStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateHotelImageCommandHandler> _logger;

    public UpdateHotelImageCommandHandler(
        IValidator<UpdateHotelImageCommand> validator,
        IHotelRepository hotelRepository,
        IHotelImageRepository hotelImageRepository,
        IImageStorageService imageStorageService,
        ICurrentUserService currentUserService,
        ILogger<UpdateHotelImageCommandHandler> logger)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _hotelImageRepository = hotelImageRepository;
        _imageStorageService = imageStorageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<UpdateHotelImageResult> HandleAsync(
        UpdateHotelImageCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(
                command,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return new UpdateHotelImageResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var hotel =
            await _hotelRepository.GetByIdAsync(
                command.HotelId,
                cancellationToken);

        if (hotel is null)
        {
            return new UpdateHotelImageResult(
                false,
                [HotelErrors.NotFound]);
        }

        if (!HotelAccessPolicy.CanManage(
                hotel,
                _currentUserService))
        {
            return new UpdateHotelImageResult(
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
            return new UpdateHotelImageResult(
                false,
                [HotelImageErrors.NotFound]);
        }

        if (!ImageFileValidator.HasValidSignature(
                command.Content,
                command.ContentType))
        {
            return new UpdateHotelImageResult(
                false,
                [
                    new ApplicationError(
                        "InvalidImageContent",
                        "The uploaded file content does not match a supported image format.",
                        ErrorType.Validation)
                ]);
        }

        var extension =
            GetExtension(command.ContentType);

        var newStorageKey =
            $"{hotel.Id}/{Guid.NewGuid():N}{extension}";

        var oldStorageKey =
            image.StorageKey;

        await _imageStorageService.UploadAsync(
            ImageContainer.HotelImages,
            newStorageKey,
            command.Content,
            command.ContentType,
            cancellationToken);

        image.ReplaceStorageKey(
            newStorageKey);

        try
        {
            await _hotelImageRepository.SaveChangesAsync(
                cancellationToken);
        }
        catch
        {
            await TryDeleteImageAsync(
                newStorageKey);

            throw;
        }

        /*
         * DB now points to the new image.
         *
         * Failure to delete the old blob must not roll back a
         * successful replacement because the user-visible state
         * is already consistent.
         */
        await TryDeleteImageAsync(
            oldStorageKey);

        return new UpdateHotelImageResult(
            true,
            []);
    }

    private async Task TryDeleteImageAsync(
        string storageKey)
    {
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
                "Failed to delete hotel image blob. StorageKey: {StorageKey}",
                storageKey);
        }
    }

    private static string GetExtension(
        string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",

            _ => throw new InvalidOperationException(
                "Unsupported image content type.")
        };
    }
}

public sealed record UpdateHotelImageResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);