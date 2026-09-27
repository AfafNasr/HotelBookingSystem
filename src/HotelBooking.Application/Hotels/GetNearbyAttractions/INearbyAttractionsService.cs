namespace HotelBooking.Application.Hotels.GetNearbyAttractions;

public interface INearbyAttractionsService
{
    Task<IReadOnlyCollection<NearbyAttraction>> GetAsync(
        decimal latitude,
        decimal longitude,
        int radiusMeters,
        int limit,
        CancellationToken cancellationToken);
}