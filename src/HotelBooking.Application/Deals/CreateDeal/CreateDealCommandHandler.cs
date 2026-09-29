using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels;
using HotelBooking.Domain.Deals;

namespace HotelBooking.Application.Deals.CreateDeal;

public sealed class CreateDealCommandHandler
{
    private readonly IValidator<CreateDealCommand> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly IDealRepository _dealRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public CreateDealCommandHandler(
        IValidator<CreateDealCommand> validator,
        IHotelRepository hotelRepository,
        IDealRepository dealRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _dealRepository = dealRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
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
            return new CreateDealResult(
                false,
                null,
              validationResult.ToApplicationErrors());
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            command.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new CreateDealResult(
                false,
                null,
              [HotelErrors.NotFound]);

        }

        if (!HotelAccessPolicy.CanManage(
     hotel,
     _currentUserService))
        {
            return new CreateDealResult(
                false,
                null,
                 [HotelErrors.ManagementForbidden]);
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
             [DealErrors.OverlappingDeal]);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

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

public sealed record CreateDealResult(
    bool Succeeded,
    int? DealId,
    IReadOnlyCollection<ApplicationError> Errors);