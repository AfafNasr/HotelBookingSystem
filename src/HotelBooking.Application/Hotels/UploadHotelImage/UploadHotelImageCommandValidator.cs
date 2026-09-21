using FluentValidation;

namespace HotelBooking.Application.Hotels.UploadHotelImage;

public sealed class UploadHotelImageCommandValidator
    : AbstractValidator<UploadHotelImageCommand>
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public UploadHotelImageCommandValidator()
    {
        RuleFor(x => x.HotelId)
            .GreaterThan(0);

        RuleFor(x => x.Content)
            .NotNull();

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(contentType =>
                AllowedContentTypes.Contains(
                    contentType,
                    StringComparer.OrdinalIgnoreCase))
            .WithMessage(
                "Only JPEG, PNG, and WebP images are allowed.");

        RuleFor(x => x.Length)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage(
                "Image size must not exceed 5 MB.");
    }
}