using FluentValidation;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Rooms;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Rooms.CreateRoom;

public sealed class CreateRoomCommandHandler
{
    private readonly IValidator<CreateRoomCommand> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public CreateRoomCommandHandler(
        IValidator<CreateRoomCommand> validator,
        IHotelRepository hotelRepository,
        IRoomRepository roomRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _roomRepository = roomRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<CreateRoomResult> HandleAsync(
        CreateRoomCommand command,
        CancellationToken cancellationToken)
    {

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new CreateRoomResult(
                false,
                null,
                validationResult.ToApplicationErrors());
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            command.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new CreateRoomResult(
                false,
                null,
                [HotelErrors.NotFound]);
        }

        if (!HotelAccessPolicy.CanManage(
         hotel,
         _currentUserService))
        {
            return new CreateRoomResult(
                false,
                null,
                [HotelErrors.ManagementForbidden]);
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
                [RoomErrors.NumberAlreadyExists]);
        }

        var room = new Room(
            command.HotelId,
            command.RoomNumber,
            command.RoomType,
            command.Description,
            command.AdultsCapacity,
            command.ChildrenCapacity,
            command.PricePerNight,
            now);

        _roomRepository.Add(room);

        await _roomRepository.SaveChangesAsync(
            cancellationToken);

        return new CreateRoomResult(
            true,
            room.Id,
            Array.Empty<ApplicationError>());
    }
}