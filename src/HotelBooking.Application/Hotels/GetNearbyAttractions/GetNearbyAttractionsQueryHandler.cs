using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;

namespace HotelBooking.Application.Hotels.GetNearbyAttractions;

public sealed class GetNearbyAttractionsQueryHandler
{
    private readonly IValidator<GetNearbyAttractionsQuery> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly INearbyAttractionsService _nearbyAttractionsService;

    public GetNearbyAttractionsQueryHandler(
        IValidator<GetNearbyAttractionsQuery> validator,
        IHotelRepository hotelRepository,
        INearbyAttractionsService nearbyAttractionsService)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _nearbyAttractionsService = nearbyAttractionsService;
    }

    public async Task<GetNearbyAttractionsResult> HandleAsync(
        GetNearbyAttractionsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            query,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new GetNearbyAttractionsResult(
                false,
                Array.Empty<NearbyAttraction>(),
                validationResult.ToApplicationErrors());
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            query.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new GetNearbyAttractionsResult(
                false,
                Array.Empty<NearbyAttraction>(),
                [HotelErrors.NotFound]);
        }

        if (!hotel.Latitude.HasValue ||
            !hotel.Longitude.HasValue)
        {
            return new GetNearbyAttractionsResult(
                false,
                Array.Empty<NearbyAttraction>(),
                [HotelErrors.CoordinatesNotConfigured]);
        }

        var attractions =
            await _nearbyAttractionsService.GetAsync(
                hotel.Latitude.Value,
                hotel.Longitude.Value,
                query.RadiusMeters,
                query.Limit,
                cancellationToken);

        return new GetNearbyAttractionsResult(
            true,
            attractions,
            Array.Empty<ApplicationError>());
    }
}


public sealed record GetNearbyAttractionsResult(
    bool Succeeded,
    IReadOnlyCollection<NearbyAttraction> Attractions,
    IReadOnlyCollection<ApplicationError> Errors);