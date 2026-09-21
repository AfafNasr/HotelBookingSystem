using FluentValidation;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Domain.Deals;

namespace HotelBooking.Application.Deals.CreateDeal;

public sealed class CreateDealCommandHandler
{
    private readonly IValidator<CreateDealCommand> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly IDealRepository _dealRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateDealCommandHandler(
        IValidator<CreateDealCommand> validator,
        IHotelRepository hotelRepository,
        IDealRepository dealRepository,
        ICurrentUserService currentUserService)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _dealRepository = dealRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CreateDealResult> HandleAsync(
        CreateDealCommand command,
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

            return new CreateDealResult(
                false,
                null,
                errors);
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            command.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new CreateDealResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "HotelNotFound",
                        "Hotel was not found.",
                        ErrorType.NotFound)
                });
        }

        if (hotel.OwnerId != _currentUserService.UserId)
        {
            return new CreateDealResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "HotelOwnershipRequired",
                        "You can only manage deals for hotels you own.",
                        ErrorType.Authorization)
                });
        }

        var hasOverlap =
            await _dealRepository.HasOverlappingDealAsync(
                hotel.Id,
                command.StartDate,
                command.EndDate,
                cancellationToken);

        if (hasOverlap)
        {
            return new CreateDealResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "OverlappingDeal",
                        "The hotel already has a deal that overlaps with the selected date range.",
                        ErrorType.Conflict)
                });
        }

        var now = DateTime.UtcNow;

        var deal = new Deal(
            hotel.Id,
            command.DiscountPercentage,
            command.StartDate,
            command.EndDate,
            now);

        _dealRepository.Add(deal);

        await _dealRepository.SaveChangesAsync(
            cancellationToken);

        return new CreateDealResult(
            true,
            deal.Id,
            Array.Empty<ApplicationError>());
    }
}