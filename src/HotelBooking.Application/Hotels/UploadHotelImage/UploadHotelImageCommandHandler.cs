using FluentValidation;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Domain.Hotels;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Hotels.UploadHotelImage;

public sealed class UploadHotelImageCommandHandler
{
    private readonly IValidator<UploadHotelImageCommand> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly IHotelImageRepository _hotelImageRepository;
    private readonly IImageStorageService _imageStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UploadHotelImageCommandHandler> _logger;

    public UploadHotelImageCommandHandler(
        IValidator<UploadHotelImageCommand> validator,
        IHotelRepository hotelRepository,
        IHotelImageRepository hotelImageRepository,
        IImageStorageService imageStorageService,
        ICurrentUserService currentUserService,
        ILogger<UploadHotelImageCommandHandler> logger)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _hotelImageRepository = hotelImageRepository;
        _imageStorageService = imageStorageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<UploadHotelImageResult> HandleAsync(
        UploadHotelImageCommand command,
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

            return new UploadHotelImageResult(
                false,
                null,
                errors);
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            command.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new UploadHotelImageResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "HotelNotFound",
                        "The specified hotel does not exist.",
                        ErrorType.NotFound)
                });
        }

        if (hotel.OwnerId != _currentUserService.UserId)
        {
            return new UploadHotelImageResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "HotelOwnershipRequired",
                        "You can only upload images for hotels you own.",
                        ErrorType.Authorization)
                });
        }

        if (!ImageFileValidator.HasValidSignature(
                command.Content,
                command.ContentType))
        {
            return new UploadHotelImageResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "InvalidImageContent",
                        "The uploaded file content does not match a supported image format.",
                        ErrorType.Validation)
                });
        }

        var extension = GetExtension(command.ContentType);

        var storageKey =
            $"{hotel.Id}/{Guid.NewGuid():N}{extension}";

        var displayOrder =
       await _hotelImageRepository.GetNextDisplayOrderAsync(
        hotel.Id,
        cancellationToken);

        await _imageStorageService.UploadAsync(
      ImageContainer.HotelImages,
      storageKey,
      command.Content,
      command.ContentType,
      cancellationToken); 

        var hotelImage = new HotelImage(
            hotel.Id,
            storageKey,
            displayOrder,
            isPrimary: false);

        _hotelImageRepository.Add(hotelImage);

        try
        {
            await _hotelImageRepository.SaveChangesAsync(
                cancellationToken);
        }
        catch
        {
            await TryDeleteUploadedImageAsync(
                storageKey,
                CancellationToken.None);

            throw;
        }

        return new UploadHotelImageResult(
            true,
            hotelImage.Id,
            Array.Empty<ApplicationError>());
    }

    private async Task TryDeleteUploadedImageAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        try
        {
            await _imageStorageService.DeleteAsync(
    ImageContainer.HotelImages,
    storageKey,
    CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete uploaded hotel image during cleanup. StorageKey: {StorageKey}",
                storageKey);
        }
    }

    private static string GetExtension(string contentType)
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