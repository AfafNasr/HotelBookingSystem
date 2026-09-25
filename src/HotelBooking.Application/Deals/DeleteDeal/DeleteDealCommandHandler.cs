using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels;

namespace HotelBooking.Application.Deals.DeleteDeal;

public sealed class DeleteDealCommandHandler
{
    private readonly IDealRepository _dealRepository;
    private readonly IHotelRepository _hotelRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteDealCommandHandler(
        IDealRepository dealRepository,
        IHotelRepository hotelRepository,
        ICurrentUserService currentUserService)
    {
        _dealRepository = dealRepository;
        _hotelRepository = hotelRepository;
        _currentUserService = currentUserService;
    }

    public async Task<DeleteDealResult> HandleAsync(
        DeleteDealCommand command,
        CancellationToken cancellationToken)
    {
        if (command.DealId <= 0)
        {
            return new DeleteDealResult(
                false,
                [
                    new ApplicationError(
                        "Deal.InvalidId",
                        "The deal ID must be greater than zero.",
                        ErrorType.Validation)
                ]);
        }

        var deal = await _dealRepository.GetByIdAsync(
            command.DealId,
            cancellationToken);

        if (deal is null)
        {
            return new DeleteDealResult(
                false,
                [DealErrors.NotFound]);
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            deal.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new DeleteDealResult(
                false,
                [HotelErrors.NotFound]);
        }

        if (!HotelAccessPolicy.CanManage(
            hotel,
            _currentUserService))
        {
            return new DeleteDealResult(
                false,
                [HotelErrors.ManagementForbidden]);
        }

        _dealRepository.Remove(deal);

        await _dealRepository.SaveChangesAsync(
            cancellationToken);

        return new DeleteDealResult(
            true,
            []);
    }
}