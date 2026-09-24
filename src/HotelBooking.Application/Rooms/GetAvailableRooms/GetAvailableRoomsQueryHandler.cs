using FluentValidation;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Rooms.GetAvailableRooms;

public sealed class GetAvailableRoomsQueryHandler
{
    private readonly IValidator<GetAvailableRoomsQuery> _validator;
    private readonly IAvailableRoomsQuery _availableRoomsQuery;

    public GetAvailableRoomsQueryHandler(
        IValidator<GetAvailableRoomsQuery> validator,
        IAvailableRoomsQuery availableRoomsQuery)
    {
        _validator = validator;
        _availableRoomsQuery = availableRoomsQuery;
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

        var now = DateTime.UtcNow;

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