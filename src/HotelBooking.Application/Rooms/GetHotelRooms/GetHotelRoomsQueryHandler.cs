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
                new[]
                {
                    new ApplicationError(
                        "HotelNotFound",
                        "The specified hotel does not exist.",
                        ErrorType.NotFound)
                });
        }

        var isAdmin = _currentUserService.IsInRole(Roles.Admin);
        var isOwner = hotel.OwnerId == _currentUserService.UserId;

        if (!isAdmin && !isOwner)
        {
            return new GetHotelRoomsResult(
                false,
                Array.Empty<HotelRoom>(),
                new[]
                {
                    new ApplicationError(
                        "HotelOwnershipRequired",
                        "You are not allowed to view rooms for this hotel.",
                        ErrorType.Authorization)
                });
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