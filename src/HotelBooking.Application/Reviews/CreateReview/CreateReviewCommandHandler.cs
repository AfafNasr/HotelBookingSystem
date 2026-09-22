using FluentValidation;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Reviews;

namespace HotelBooking.Application.Reviews.CreateReview;

public sealed class CreateReviewCommandHandler
{
    private readonly IValidator<CreateReviewCommand> _validator;
    private readonly IBookingRepository _bookingRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateReviewCommandHandler(
        IValidator<CreateReviewCommand> validator,
        IBookingRepository bookingRepository,
        IReviewRepository reviewRepository,
        ICurrentUserService currentUserService)
    {
        _validator = validator;
        _bookingRepository = bookingRepository;
        _reviewRepository = reviewRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CreateReviewResult> HandleAsync(
        CreateReviewCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new ApplicationError(
                    error.PropertyName,
                    error.ErrorMessage,
                    ErrorType.Validation))
                .ToArray();

            return new CreateReviewResult(
                false,
                null,
                errors);
        }

        var booking = await _bookingRepository.GetByIdAsync(
            command.BookingId,
            cancellationToken);

        var currentUserId = _currentUserService.UserId;

        if (booking is null ||
            booking.UserId != currentUserId)
        {
            return new CreateReviewResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "BookingNotFound",
                        "The specified booking does not exist.",
                        ErrorType.NotFound)
                });
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            return new CreateReviewResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "BookingNotEligibleForReview",
                        "Only confirmed bookings can be reviewed.",
                        ErrorType.Conflict)
                });
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (booking.CheckOutDate > today)
        {
            return new CreateReviewResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "StayNotCompleted",
                        "The booking can only be reviewed after the stay has been completed.",
                        ErrorType.Conflict)
                });
        }

        var reviewExists =
            await _reviewRepository.ExistsForBookingAsync(
                booking.Id,
                cancellationToken);

        if (reviewExists)
        {
            return new CreateReviewResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "ReviewAlreadyExists",
                        "A review already exists for this booking.",
                        ErrorType.Conflict)
                });
        }

        var review = new Review(
            booking.Id,
            command.Rating,
            command.Comment,
            DateTime.UtcNow);

        _reviewRepository.Add(review);

        await _reviewRepository.SaveChangesAsync(
            cancellationToken);

        return new CreateReviewResult(
            true,
            review.Id,
            Array.Empty<ApplicationError>());
    }
}