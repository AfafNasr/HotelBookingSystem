using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels;

namespace HotelBooking.Application.Bookings.GetHotelBookings;

public sealed class GetHotelBookingsQueryHandler
{
    private readonly IValidator<GetHotelBookingsQuery> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly IHotelBookingsQuery _hotelBookingsQuery;
    private readonly ICurrentUserService _currentUserService;

    public GetHotelBookingsQueryHandler(
        IValidator<GetHotelBookingsQuery> validator,
        IHotelRepository hotelRepository,
        IHotelBookingsQuery hotelBookingsQuery,
        ICurrentUserService currentUserService)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _hotelBookingsQuery = hotelBookingsQuery;
        _currentUserService = currentUserService;
    }

    public async Task<GetHotelBookingsResult> HandleAsync(
        GetHotelBookingsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            query,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new GetHotelBookingsResult(
                false,
                [],
                query.Page,
                query.PageSize,
                false,
                validationResult.ToApplicationErrors());
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            query.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new GetHotelBookingsResult(
                false,
                [],
                query.Page,
                query.PageSize,
                false,
                [HotelErrors.NotFound]);
        }

        if (!HotelAccessPolicy.CanManage(
                hotel,
                _currentUserService))
        {
            return new GetHotelBookingsResult(
                false,
                [],
                query.Page,
                query.PageSize,
                false,
                [HotelErrors.ManagementForbidden]);
        }

        var page = await _hotelBookingsQuery.GetAsync(
            query.HotelId,
            query.Page,
            query.PageSize,
            cancellationToken);

        return new GetHotelBookingsResult(
            true,
            page.Bookings,
            query.Page,
            query.PageSize,
            page.HasNextPage,
            []);
    }
}

public sealed record GetHotelBookingsResult(
    bool Succeeded,
    IReadOnlyCollection<HotelBookingItem> Bookings,
    int Page,
    int PageSize,
    bool HasNextPage,
    IReadOnlyCollection<ApplicationError> Errors);