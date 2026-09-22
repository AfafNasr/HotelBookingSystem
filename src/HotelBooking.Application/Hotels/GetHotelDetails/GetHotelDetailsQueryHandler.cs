using FluentValidation;
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
            var errors = validationResult.Errors
                .Select(error => new ApplicationError(
                    error.PropertyName,
                    error.ErrorMessage,
                    ErrorType.Validation))
                .ToArray();

            return new GetHotelDetailsResult(
                false,
                null,
                errors);
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