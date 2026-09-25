using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;

namespace HotelBooking.Application.Rooms.GetAdminRooms;

public sealed class GetAdminRoomsQueryHandler
{
    private readonly IValidator<GetAdminRoomsQuery> _validator;
    private readonly IAdminRoomsQuery _adminRoomsQuery;
    private readonly TimeProvider _timeProvider;

    public GetAdminRoomsQueryHandler(
        IValidator<GetAdminRoomsQuery> validator,
        IAdminRoomsQuery adminRoomsQuery,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _adminRoomsQuery = adminRoomsQuery;
        _timeProvider = timeProvider;
    }

    public async Task<GetAdminRoomsResult> HandleAsync(
        GetAdminRoomsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            query,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new GetAdminRoomsResult(
                false,
                Array.Empty<AdminRoom>(),
                validationResult.ToApplicationErrors());
        }
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var rooms = await _adminRoomsQuery.GetAsync(
            query,
            now,
            cancellationToken);

        return new GetAdminRoomsResult(
            true,
            rooms,
            Array.Empty<ApplicationError>());
    }
}