using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels;

namespace HotelBooking.Application.Deals.UpdateDeal;

public sealed class UpdateDealCommandHandler
{
    private readonly IValidator<UpdateDealCommand> _validator;
    private readonly IDealRepository _dealRepository;
    private readonly IHotelRepository _hotelRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public UpdateDealCommandHandler(
        IValidator<UpdateDealCommand> validator,
        IDealRepository dealRepository,
        IHotelRepository hotelRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _dealRepository = dealRepository;
        _hotelRepository = hotelRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<UpdateDealResult> HandleAsync(
        UpdateDealCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new UpdateDealResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var deal = await _dealRepository.GetByIdAsync(
            command.DealId,
            cancellationToken);

        if (deal is null)
        {
            return new UpdateDealResult(
                false,
                [DealErrors.NotFound]);
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            deal.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new UpdateDealResult(
                false,
                [HotelErrors.NotFound]);
        }

        if (!HotelAccessPolicy.CanManage(
            hotel,
            _currentUserService))
        {
            return new UpdateDealResult(
                false,
                [HotelErrors.ManagementForbidden]);
        }

        var hasOverlap =
            await _dealRepository.HasOverlappingDealExceptAsync(
                deal.HotelId,
                command.StartDate,
                command.EndDate,
                deal.Id,
                cancellationToken);

        if (hasOverlap)
        {
            return new UpdateDealResult(
                false,
                [DealErrors.OverlappingDeal]);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        deal.Update(
            command.DiscountPercentage,
            command.StartDate,
            command.EndDate,
            now);

        await _dealRepository.SaveChangesAsync(
            cancellationToken);

        return new UpdateDealResult(
            true,
            []);
    }
}

public sealed record UpdateDealResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);