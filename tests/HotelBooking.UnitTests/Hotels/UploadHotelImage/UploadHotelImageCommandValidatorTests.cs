using FluentValidation.TestHelper;
using HotelBooking.Application.Hotels.UploadHotelImage;

namespace HotelBooking.UnitTests.Hotels.UploadHotelImage;

public sealed class UploadHotelImageCommandValidatorTests
{
    private readonly UploadHotelImageCommandValidator _validator = new();

    private static UploadHotelImageCommand CreateValidCommand()
    {
        var content = new MemoryStream(
        [
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x00
        ]);

        return new UploadHotelImageCommand(
            HotelId: 1,
            Content: content,
            FileName: "hotel.png",
            ContentType: "image/png",
            Length: content.Length);
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

        result.ShouldHaveValidationErrorFor(
            command => command.HotelId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenContentIsNull()
    {
        var command = CreateValidCommand() with
        {
            Content = null!
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.Content);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenFileNameIsEmpty()
    {
        var command = CreateValidCommand() with
        {
            FileName = ""
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.FileName);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenFileNameExceedsMaximumLength()
    {
        var command = CreateValidCommand() with
        {
            FileName = new string('A', 256)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.FileName);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("IMAGE/PNG")]
    public void Validate_ShouldNotHaveError_WhenContentTypeIsSupported(
        string contentType)
    {
        var command = CreateValidCommand() with
        {
            ContentType = contentType
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(
            command => command.ContentType);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenContentTypeIsUnsupported()
    {
        var command = CreateValidCommand() with
        {
            ContentType = "image/gif"
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.ContentType);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenLengthIsNotPositive()
    {
        var command = CreateValidCommand() with
        {
            Length = 0
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.Length);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenLengthEqualsMaximumFileSize()
    {
        const long maxFileSize =
            5 * 1024 * 1024;

        var command = CreateValidCommand() with
        {
            Length = maxFileSize
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(
            command => command.Length);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenLengthExceedsMaximumFileSize()
    {
        const long maxFileSize =
            5 * 1024 * 1024;

        var command = CreateValidCommand() with
        {
            Length = maxFileSize + 1
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.Length);
    }
}