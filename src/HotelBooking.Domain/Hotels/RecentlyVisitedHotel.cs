namespace HotelBooking.Domain.Hotels;

public sealed class RecentlyVisitedHotel
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public int HotelId { get; private set; }
    public DateTime LastVisitedAt { get; private set; }

    public Hotel Hotel { get; private set; } = null!;

    private RecentlyVisitedHotel()
    {
    }

    public RecentlyVisitedHotel(
        string userId,
        int hotelId,
        DateTime visitedAt)
    {
        UserId = userId;
        HotelId = hotelId;
        LastVisitedAt = visitedAt;
    }

    public void MarkVisited(DateTime visitedAt)
    {
        LastVisitedAt = visitedAt;
    }
}