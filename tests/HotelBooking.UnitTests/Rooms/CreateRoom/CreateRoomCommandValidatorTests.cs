using FluentValidation.TestHelper;
using HotelBooking.Application.Rooms.CreateRoom;
using HotelBooking.Domain.Rooms;

namespace HotelBooking.UnitTests.Rooms.CreateRoom;

public sealed class CreateRoomCommandValidatorTests
{
    private readonly CreateRoomCommandValidator _validator = new();

    private static CreateRoomCommand CreateValidCommand()
    {
        return new CreateRoomCommand(
            HotelId: 1,
            RoomNumber: "101",
            RoomType: RoomType.Standard,
            Description: "Comfortable standard room.",
            AdultsCapacity: 2,
            ChildrenCapacity: 1,
            PricePerNight: 100m);
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenCommandIsValid()
    {
        var command = CreateValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenHotelIdIsInvalid()
    {
        var command = CreateValidCommand() with
        {
            HotelId = 0
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.HotelId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRoomNumberIsEmpty()
    {
        var command = CreateValidCommand() with
        {
            RoomNumber = ""
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RoomNumber);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRoomNumberExceedsMaximumLength()
    {
        var command = CreateValidCommand() with
        {
            RoomNumber = new string('A', 21)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RoomNumber);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRoomTypeIsInvalid()
    {
        var command = CreateValidCommand() with
        {
            RoomType = (RoomType)999
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RoomType);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenDescriptionExceedsMaximumLength()
    {
        var command = CreateValidCommand() with
        {
            Description = new string('A', 2001)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAdultsCapacityIsLessThanOne()
    {
        var command = CreateValidCommand() with
        {
            AdultsCapacity = 0
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AdultsCapacity);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenChildrenCapacityIsNegative()
    {
        var command = CreateValidCommand() with
        {
            ChildrenCapacity = -1
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ChildrenCapacity);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPricePerNightIsNotPositive()
    {
        var command = CreateValidCommand() with
        {
            PricePerNight = 0
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PricePerNight);
    }
}