using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Hotels.UploadHotelImage;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Rooms.UpdateRoomImage;

public sealed class UpdateRoomImageCommandHandler
{
    private readonly IValidator<UpdateRoomImageCommand> _validator;
    private readonly IRoomRepository _roomRepository;
    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomImageRepository _roomImageRepository;
    private readonly IImageStorageService _imageStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateRoomImageCommandHandler> _logger;

    public UpdateRoomImageCommandHandler(
        IValidator<UpdateRoomImageCommand> validator,
        IRoomRepository roomRepository,
        IHotelRepository hotelRepository,
        IRoomImageRepository roomImageRepository,
        IImageStorageService imageStorageService,
        ICurrentUserService currentUserService,
        ILogger<UpdateRoomImageCommandHandler> logger)
    {
        _validator = validator;
        _roomRepository = roomRepository;
        _hotelRepository = hotelRepository;
        _roomImageRepository = roomImageRepository;
        _imageStorageService = imageStorageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<UpdateRoomImageResult> HandleAsync(
        UpdateRoomImageCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(
                command,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return new UpdateRoomImageResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var room =
            await _roomRepository.GetByIdAsync(
                command.RoomId,
                cancellationToken);

        if (room is null)
        {
            return new UpdateRoomImageResult(
                false,
                [RoomErrors.NotFound]);
        }

        var hotel =
            await _hotelRepository.GetByIdAsync(
                room.HotelId,
                cancellationToken);

        if (hotel is null)
        {
            return new UpdateRoomImageResult(
                false,
                [HotelErrors.NotFound]);
        }

        if (!HotelAccessPolicy.CanManage(
                hotel,
                _currentUserService))
        {
            return new UpdateRoomImageResult(
                false,
                [HotelErrors.ManagementForbidden]);
        }

        var image =
            await _roomImageRepository.GetByIdAsync(
                command.ImageId,
                cancellationToken);

        if (image is null ||
            image.RoomId != room.Id)
        {
            return new UpdateRoomImageResult(
                false,
                [RoomImageErrors.NotFound]);
        }

        if (!ImageFileValidator.HasValidSignature(
                command.Content,
                command.ContentType))
        {
            return new UpdateRoomImageResult(
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
            $"{room.Id}/{Guid.NewGuid():N}{extension}";

        var oldStorageKey =
            image.StorageKey;

        await _imageStorageService.UploadAsync(
            ImageContainer.RoomImages,
            newStorageKey,
            command.Content,
            command.ContentType,
            cancellationToken);

        image.ReplaceStorageKey(
            newStorageKey);

        try
        {
            await _roomImageRepository.SaveChangesAsync(
                cancellationToken);
        }
        catch
        {
            await TryDeleteImageAsync(
                newStorageKey);

            throw;
        }

        await TryDeleteImageAsync(
            oldStorageKey);

        return new UpdateRoomImageResult(
            true,
            []);
    }

    private async Task TryDeleteImageAsync(
        string storageKey)
    {
        try
        {
            await _imageStorageService.DeleteAsync(
                ImageContainer.RoomImages,
                storageKey,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to delete room image blob. StorageKey: {StorageKey}",
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

public sealed record UpdateRoomImageResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);