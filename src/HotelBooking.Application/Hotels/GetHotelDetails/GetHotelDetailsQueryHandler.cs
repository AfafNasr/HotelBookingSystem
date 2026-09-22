using FluentValidation;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Hotels.GetHotelDetails;

public sealed class GetHotelDetailsQueryHandler
{
    private readonly IValidator<GetHotelDetailsQuery> _validator;
    private readonly IHotelDetailsQuery _hotelDetailsQuery;

    public GetHotelDetailsQueryHandler(
        IValidator<GetHotelDetailsQuery> validator,
        IHotelDetailsQuery hotelDetailsQuery)
    {
        _validator = validator;
        _hotelDetailsQuery = hotelDetailsQuery;
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

        return new GetHotelDetailsResult(
            true,
            hotel,
            Array.Empty<ApplicationError>());
    }
}