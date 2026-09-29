using FluentValidation.TestHelper;
using HotelBooking.Application.Rooms.UploadRoomImage;

namespace HotelBooking.UnitTests.Rooms.UploadRoomImage;

public sealed class UploadRoomImageCommandValidatorTests
{
    private readonly UploadRoomImageCommandValidator _validator = new();

    private static UploadRoomImageCommand CreateValidCommand()
    {
        return new UploadRoomImageCommand(
            RoomId: 1,
            Content: new MemoryStream([0xFF, 0xD8, 0xFF]),
            FileName: "room.jpg",
            ContentType: "image/jpeg",
            Length: 1024);
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenCommandIsValid()
    {
        var command = CreateValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRoomIdIsInvalid()
    {
        var command = CreateValidCommand() with
        {
            RoomId = 0
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RoomId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenContentIsNull()
    {
        var command = CreateValidCommand() with
        {
            Content = null!
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenFileNameIsEmpty()
    {
        var command = CreateValidCommand() with
        {
            FileName = ""
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenFileNameExceedsMaximumLength()
    {
        var command = CreateValidCommand() with
        {
            FileName = new string('A', 256)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenContentTypeIsUnsupported()
    {
        var command = CreateValidCommand() with
        {
            ContentType = "application/pdf"
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenContentTypeUsesDifferentCasing()
    {
        var command = CreateValidCommand() with
        {
            ContentType = "IMAGE/JPEG"
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.ContentType);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenLengthIsZero()
    {
        var command = CreateValidCommand() with
        {
            Length = 0
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Length);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenLengthExceedsFiveMegabytes()
    {
        var command = CreateValidCommand() with
        {
            Length = (5 * 1024 * 1024) + 1
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Length);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenLengthIsExactlyFiveMegabytes()
    {
        var command = CreateValidCommand() with
        {
            Length = 5 * 1024 * 1024
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Length);
    }
}