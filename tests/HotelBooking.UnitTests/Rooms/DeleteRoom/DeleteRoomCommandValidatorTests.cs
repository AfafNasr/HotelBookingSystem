using FluentValidation.TestHelper;
using HotelBooking.Application.Rooms.DeleteRoom;

namespace HotelBooking.UnitTests.Rooms.DeleteRoom;

public sealed class DeleteRoomCommandValidatorTests
{
    private readonly DeleteRoomCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenCommandIsValid()
    {
        // Arrange
        var command = new DeleteRoomCommand(RoomId: 1);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRoomIdIsInvalid()
    {
        // Arrange
        var command = new DeleteRoomCommand(RoomId: 0);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoomId);
    }
}