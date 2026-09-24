using FluentValidation;

namespace HotelBooking.Application.Rooms.DeleteRoom;

public sealed class DeleteRoomCommandValidator
    : AbstractValidator<DeleteRoomCommand>
{
    public DeleteRoomCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .GreaterThan(0);
    }
}