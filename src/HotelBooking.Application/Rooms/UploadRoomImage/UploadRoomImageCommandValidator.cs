using FluentValidation;

namespace HotelBooking.Application.Rooms.UploadRoomImage;

public sealed class UploadRoomImageCommandValidator
    : AbstractValidator<UploadRoomImageCommand>
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public UploadRoomImageCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .GreaterThan(0);

        RuleFor(x => x.Content)
            .NotNull();

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.ContentType)
            .Must(contentType =>
                AllowedContentTypes.Contains(
                    contentType,
                    StringComparer.OrdinalIgnoreCase))
            .WithMessage("Only JPEG, PNG, and WebP images are supported.");

        RuleFor(x => x.Length)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage("Image size must not exceed 5 MB.");
    }
}