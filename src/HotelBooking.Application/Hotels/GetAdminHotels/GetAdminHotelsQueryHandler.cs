using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;

namespace HotelBooking.Application.Hotels.GetAdminHotels;

public sealed class GetAdminHotelsQueryHandler
{
    private readonly IValidator<GetAdminHotelsQuery> _validator;
    private readonly IAdminHotelsQuery _adminHotelsQuery;

    public GetAdminHotelsQueryHandler(
        IValidator<GetAdminHotelsQuery> validator,
        IAdminHotelsQuery adminHotelsQuery)
    {
        _validator = validator;
        _adminHotelsQuery = adminHotelsQuery;
    }

    public async Task<GetAdminHotelsResult> HandleAsync(
        GetAdminHotelsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            query,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new GetAdminHotelsResult(
                false,
                Array.Empty<AdminHotel>(),
                validationResult.ToApplicationErrors());
        }

        var hotels = await _adminHotelsQuery.GetAsync(
            query,
            cancellationToken);

        return new GetAdminHotelsResult(
            true,
            hotels,
            Array.Empty<ApplicationError>());
    }
}