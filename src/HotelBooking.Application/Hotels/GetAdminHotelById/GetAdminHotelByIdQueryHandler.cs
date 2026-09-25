using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Hotels.GetAdminHotelById;

public sealed class GetAdminHotelByIdQueryHandler
{
    private readonly IAdminHotelByIdQuery _hotelQuery;

    public GetAdminHotelByIdQueryHandler(
        IAdminHotelByIdQuery hotelQuery)
    {
        _hotelQuery = hotelQuery;
    }

    public async Task<GetAdminHotelByIdResult> HandleAsync(
        int hotelId,
        CancellationToken cancellationToken)
    {
        var hotel = await _hotelQuery.GetByIdAsync(
            hotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new GetAdminHotelByIdResult(
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

        return new GetAdminHotelByIdResult(
            true,
            hotel,
            Array.Empty<ApplicationError>());
    }
}