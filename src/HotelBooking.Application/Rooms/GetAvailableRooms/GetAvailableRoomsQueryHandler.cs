using FluentValidation;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Rooms.GetAvailableRooms;

public sealed class GetAvailableRoomsQueryHandler
{
    private readonly IValidator<GetAvailableRoomsQuery> _validator;
    private readonly IAvailableRoomsQuery _availableRoomsQuery;
    private readonly TimeProvider _timeProvider;

    public GetAvailableRoomsQueryHandler(
        IValidator<GetAvailableRoomsQuery> validator,
        IAvailableRoomsQuery availableRoomsQuery,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _availableRoomsQuery = availableRoomsQuery;
        _timeProvider = timeProvider;
    }

    public async Task<GetAvailableRoomsResult> HandleAsync(
        GetAvailableRoomsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(query, cancellationToken);

        if (!validationResult.IsValid)
        {

            return new GetAvailableRoomsResult(
                false,
                [],
                 validationResult.ToApplicationErrors());
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var rooms = await _availableRoomsQuery.GetAsync(
            query,
            now,
            cancellationToken);

        return new GetAvailableRoomsResult(
            true,
            rooms,
            []);
    }
}

public sealed record GetAvailableRoomsResult(
    bool Succeeded,
    IReadOnlyCollection<AvailableRoom> Rooms,
    IReadOnlyCollection<ApplicationError> Errors);