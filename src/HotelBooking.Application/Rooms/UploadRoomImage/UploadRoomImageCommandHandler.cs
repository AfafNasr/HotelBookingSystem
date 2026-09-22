using FluentValidation;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Hotels.UploadHotelImage;
using HotelBooking.Domain.Rooms;
using Microsoft.Extensions.Logging;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Hotels;

namespace HotelBooking.Application.Rooms.UploadRoomImage;

public sealed class UploadRoomImageCommandHandler
{
    private readonly IValidator<UploadRoomImageCommand> _validator;
    private readonly IRoomRepository _roomRepository;
    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomImageRepository _roomImageRepository;
    private readonly IImageStorageService _imageStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UploadRoomImageCommandHandler> _logger;

    public UploadRoomImageCommandHandler(
        IValidator<UploadRoomImageCommand> validator,
        IRoomRepository roomRepository,
        IHotelRepository hotelRepository,
        IRoomImageRepository roomImageRepository,
        IImageStorageService imageStorageService,
        ICurrentUserService currentUserService,
        ILogger<UploadRoomImageCommandHandler> logger)
    {
        _validator = validator;
        _roomRepository = roomRepository;
        _hotelRepository = hotelRepository;
        _roomImageRepository = roomImageRepository;
        _imageStorageService = imageStorageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<UploadRoomImageResult> HandleAsync(
        UploadRoomImageCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new UploadRoomImageResult(
                false,
                null,
                 validationResult.ToApplicationErrors());
        }

        var room = await _roomRepository.GetByIdAsync(
            command.RoomId,
            cancellationToken);

        if (room is null)
        {
            return new UploadRoomImageResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "RoomNotFound",
                        "The specified room does not exist.",
                        ErrorType.NotFound)
                });
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            room.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new UploadRoomImageResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "HotelNotFound",
                        "The hotel associated with the room does not exist.",
                        ErrorType.NotFound)
                });
        }

        if (hotel.OwnerId != _currentUserService.UserId)
        {
            return new UploadRoomImageResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "HotelOwnershipRequired",
                        "You can only upload images for rooms belonging to hotels you own.",
                        ErrorType.Authorization)
                });
        }

        if (!ImageFileValidator.HasValidSignature(
                command.Content,
                command.ContentType))
        {
            return new UploadRoomImageResult(
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
            $"{room.Id}/{Guid.NewGuid():N}{extension}";

        var displayOrder =
            await _roomImageRepository.GetNextDisplayOrderAsync(
                room.Id,
                cancellationToken);

        await _imageStorageService.UploadAsync(
            ImageContainer.RoomImages,
            storageKey,
            command.Content,
            command.ContentType,
            cancellationToken);

        var roomImage = new RoomImage(
            room.Id,
            storageKey,
            displayOrder,
            isPrimary: false);

        _roomImageRepository.Add(roomImage);

        try
        {
            await _roomImageRepository.SaveChangesAsync(
                cancellationToken);
        }
        catch
        {
            await TryDeleteUploadedImageAsync(
                storageKey,
                CancellationToken.None);

            throw;
        }

        return new UploadRoomImageResult(
            true,
            roomImage.Id,
            Array.Empty<ApplicationError>());
    }

    private async Task TryDeleteUploadedImageAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        try
        {
            await _imageStorageService.DeleteAsync(
                ImageContainer.RoomImages,
                storageKey,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete uploaded room image during cleanup. StorageKey: {StorageKey}",
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