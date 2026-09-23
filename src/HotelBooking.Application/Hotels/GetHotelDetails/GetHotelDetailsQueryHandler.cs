using FluentValidation;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;


namespace HotelBooking.Application.Hotels.GetHotelDetails;

public sealed class GetHotelDetailsQueryHandler
{
    private readonly IValidator<GetHotelDetailsQuery> _validator;
    private readonly IHotelDetailsQuery _hotelDetailsQuery;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRecentlyVisitedHotelRepository _recentlyVisitedHotelRepository;

    public GetHotelDetailsQueryHandler(
        IValidator<GetHotelDetailsQuery> validator,
        IHotelDetailsQuery hotelDetailsQuery,
        ICurrentUserService currentUserService,
        IRecentlyVisitedHotelRepository recentlyVisitedHotelRepository)
    {
        _validator = validator;
        _hotelDetailsQuery = hotelDetailsQuery;
        _currentUserService = currentUserService;
        _recentlyVisitedHotelRepository = recentlyVisitedHotelRepository;
    }

    public async Task<GetHotelDetailsResult> HandleAsync(
        GetHotelDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            query,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new GetHotelDetailsResult(
                false,
                null,
                validationResult.ToApplicationErrors());
        }

        var now = DateTime.UtcNow;

        var hotel = await _hotelDetailsQuery.GetByIdAsync(
            query,
            now,
            cancellationToken);

        if (hotel is null)
        {
            return new GetHotelDetailsResult(
                false,
                null,
                new[]
                {
            new ApplicationError(
                "HotelNotFound",
                "The specified hotel does not exist.",
                ErrorType.NotFound)
                });
        }

        var userId = _currentUserService.UserId;

        if (userId is not null)
        {
            await _recentlyVisitedHotelRepository.RecordVisitAsync(
                userId,
                query.HotelId,
                now,
                cancellationToken);
        }

        return new GetHotelDetailsResult(
            true,
            hotel,
            Array.Empty<ApplicationError>());

        return new GetHotelDetailsResult(
            true,
            hotel,
            Array.Empty<ApplicationError>());
    }
}