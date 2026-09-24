using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;

namespace HotelBooking.Application.Cities.GetAdminCities;

public sealed class GetAdminCitiesQueryHandler
{
    private readonly IValidator<GetAdminCitiesQuery> _validator;
    private readonly IAdminCitiesQuery _adminCitiesQuery;

    public GetAdminCitiesQueryHandler(
        IValidator<GetAdminCitiesQuery> validator,
        IAdminCitiesQuery adminCitiesQuery)
    {
        _validator = validator;
        _adminCitiesQuery = adminCitiesQuery;
    }

    public async Task<GetAdminCitiesResult> HandleAsync(
        GetAdminCitiesQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            query,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new GetAdminCitiesResult(
                false,
                Array.Empty<AdminCity>(),
                validationResult.ToApplicationErrors());
        }

        var cities = await _adminCitiesQuery.GetAsync(
            query,
            cancellationToken);

        return new GetAdminCitiesResult(
            true,
            cities,
            Array.Empty<ApplicationError>());
    }
}