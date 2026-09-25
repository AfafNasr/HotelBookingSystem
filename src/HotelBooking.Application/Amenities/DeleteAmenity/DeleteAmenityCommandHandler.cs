namespace HotelBooking.Application.Amenities.DeleteAmenity;

public sealed class DeleteAmenityCommandHandler
{
    private readonly IAmenityRepository _amenityRepository;
    private readonly TimeProvider _timeProvider;

    public DeleteAmenityCommandHandler(
        IAmenityRepository amenityRepository,
        TimeProvider timeProvider)
    {
        _amenityRepository = amenityRepository;
        _timeProvider = timeProvider;
    }

    public async Task<DeleteAmenityResult> HandleAsync(
        DeleteAmenityCommand command,
        CancellationToken cancellationToken)
    {
        if (command.AmenityId <= 0)
        {
            return new DeleteAmenityResult(
                false,
                [AmenityErrors.InvalidId]);
        }

        var amenity = await _amenityRepository.GetByIdAsync(
            command.AmenityId,
            cancellationToken);

        if (amenity is null)
        {
            return new DeleteAmenityResult(
                false,
                [AmenityErrors.NotFound]);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        amenity.Delete(now);

        await _amenityRepository.SaveChangesAsync(
            cancellationToken);

        return new DeleteAmenityResult(
            true,
            []);
    }
}