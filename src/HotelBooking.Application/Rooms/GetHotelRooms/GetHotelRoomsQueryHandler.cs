using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels;

namespace HotelBooking.Application.Rooms.GetHotelRooms;

public sealed class GetHotelRoomsQueryHandler
{
    private readonly IHotelRepository _hotelRepository;
    private readonly IHotelRoomsQuery _hotelRoomsQuery;
    private readonly ICurrentUserService _currentUserService;

    public GetHotelRoomsQueryHandler(
        IHotelRepository hotelRepository,
        IHotelRoomsQuery hotelRoomsQuery,
        ICurrentUserService currentUserService)
    {
        _hotelRepository = hotelRepository;
        _hotelRoomsQuery = hotelRoomsQuery;
        _currentUserService = currentUserService;
    }

    public async Task<GetHotelRoomsResult> HandleAsync(
        GetHotelRoomsQuery query,
        CancellationToken cancellationToken)
    {
        var hotel = await _hotelRepository.GetByIdAsync(
            query.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new GetHotelRoomsResult(
                false,
                Array.Empty<HotelRoom>(),
               [HotelErrors.NotFound]);
        }

        if (!HotelAccessPolicy.CanManage(
        hotel,
        _currentUserService))
        {
            return new GetHotelRoomsResult(
                false,
                [],
                [HotelErrors.ManagementForbidden]);
        }

        var rooms = await _hotelRoomsQuery.GetAsync(
            query.HotelId,
            cancellationToken);

        return new GetHotelRoomsResult(
            true,
            rooms,
            Array.Empty<ApplicationError>());
    }
} 