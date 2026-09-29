using FluentValidation;

namespace HotelBooking.Application.Rooms.DeleteRoomImage;

public sealed class DeleteRoomImageCommandValidator
    : AbstractValidator<DeleteRoomImageCommand>
{
    public DeleteRoomImageCommandValidator()
    {
        RuleFor(command => command.RoomId)
            .GreaterThan(0);

        RuleFor(command => command.ImageId)
            .GreaterThan(0);
    }
}