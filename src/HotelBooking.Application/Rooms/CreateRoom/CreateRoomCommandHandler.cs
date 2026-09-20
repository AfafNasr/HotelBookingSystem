using FluentValidation;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Rooms.CreateRoom;

public sealed class CreateRoomCommandHandler
{
    private readonly IValidator<CreateRoomCommand> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateRoomCommandHandler(
        IValidator<CreateRoomCommand> validator,
        IHotelRepository hotelRepository,
        IRoomRepository roomRepository,
        ICurrentUserService currentUserService)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _roomRepository = roomRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CreateRoomResult> HandleAsync(
        CreateRoomCommand command,
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

            return new CreateRoomResult(
                false,
                null,
                errors);
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            command.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new CreateRoomResult(
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

        var isAdmin = _currentUserService.IsInRole(Roles.Admin);
        var isOwner = hotel.OwnerId == _currentUserService.UserId;

        if (!isAdmin && !isOwner)
        {
            return new CreateRoomResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "HotelOwnershipRequired",
                        "You are not allowed to create rooms for this hotel.",
                        ErrorType.Authorization)
                });
        }

        var roomNumberExists =
            await _roomRepository.ExistsByRoomNumberAsync(
                command.HotelId,
                command.RoomNumber,
                cancellationToken);

        if (roomNumberExists)
        {
            return new CreateRoomResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "RoomNumberAlreadyExists",
                        "A room with this number already exists in the hotel.",
                        ErrorType.Conflict)
                });
        }

        var room = new Room(
            command.HotelId,
            command.RoomNumber,
            command.RoomType,
            command.Description,
            command.AdultsCapacity,
            command.ChildrenCapacity,
            command.PricePerNight,
            DateTime.UtcNow);

        _roomRepository.Add(room);

        await _roomRepository.SaveChangesAsync(
            cancellationToken);

        return new CreateRoomResult(
            true,
            room.Id,
            Array.Empty<ApplicationError>());
    }
}