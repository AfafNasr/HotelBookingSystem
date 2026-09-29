using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Hotels;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Rooms.DeleteRoomImage;

public sealed class DeleteRoomImageCommandHandler
{
    private readonly IValidator<DeleteRoomImageCommand> _validator;
    private readonly IRoomRepository _roomRepository;
    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomImageRepository _roomImageRepository;
    private readonly IImageStorageService _imageStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteRoomImageCommandHandler> _logger;

    public DeleteRoomImageCommandHandler(
        IValidator<DeleteRoomImageCommand> validator,
        IRoomRepository roomRepository,
        IHotelRepository hotelRepository,
        IRoomImageRepository roomImageRepository,
        IImageStorageService imageStorageService,
        ICurrentUserService currentUserService,
        ILogger<DeleteRoomImageCommandHandler> logger)
    {
        _validator = validator;
        _roomRepository = roomRepository;
        _hotelRepository = hotelRepository;
        _roomImageRepository = roomImageRepository;
        _imageStorageService = imageStorageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<DeleteRoomImageResult> HandleAsync(
        DeleteRoomImageCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(
                command,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return new DeleteRoomImageResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var room =
            await _roomRepository.GetByIdAsync(
                command.RoomId,
                cancellationToken);

        if (room is null)
        {
            return new DeleteRoomImageResult(
                false,
                [RoomErrors.NotFound]);
        }

        var hotel =
            await _hotelRepository.GetByIdAsync(
                room.HotelId,
                cancellationToken);

        if (hotel is null)
        {
            return new DeleteRoomImageResult(
                false,
                [HotelErrors.NotFound]);
        }

        if (!HotelAccessPolicy.CanManage(
                hotel,
                _currentUserService))
        {
            return new DeleteRoomImageResult(
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
            return new DeleteRoomImageResult(
                false,
                [RoomImageErrors.NotFound]);
        }

        var storageKey =
            image.StorageKey;

        _roomImageRepository.Remove(
            image);

        await _roomImageRepository.SaveChangesAsync(
            cancellationToken);

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
                "Failed to delete room image blob after removing DB record. StorageKey: {StorageKey}",
                storageKey);
        }

        return new DeleteRoomImageResult(
            true,
            []);
    }
}

public sealed record DeleteRoomImageResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);