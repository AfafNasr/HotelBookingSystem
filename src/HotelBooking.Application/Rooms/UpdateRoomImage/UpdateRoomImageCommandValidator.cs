using FluentValidation;

namespace HotelBooking.Application.Rooms.UpdateRoomImage;

public sealed class UpdateRoomImageCommandValidator
    : AbstractValidator<UpdateRoomImageCommand>
{
    private const long MaxFileSize =
        5 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public UpdateRoomImageCommandValidator()
    {
        RuleFor(command => command.RoomId)
            .GreaterThan(0);

        RuleFor(command => command.ImageId)
            .GreaterThan(0);

        RuleFor(command => command.Content)
            .NotNull();

        RuleFor(command => command.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(command => command.ContentType)
            .NotEmpty()
            .Must(contentType =>
                AllowedContentTypes.Contains(
                    contentType,
                    StringComparer.OrdinalIgnoreCase))
            .WithMessage(
                "Only JPEG, PNG, and WebP images are allowed.");

        RuleFor(command => command.Length)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage(
                "Image size must not exceed 5 MB.");
    }
}