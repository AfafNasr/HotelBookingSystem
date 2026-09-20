namespace HotelBooking.Domain.Hotels;

public sealed class HotelAmenity
{
    public int HotelId { get; private set; }
    public int AmenityId { get; private set; }

    private HotelAmenity()
    {
    }

    public HotelAmenity(
        int hotelId,
        int amenityId)
    {
        HotelId = hotelId;
        AmenityId = amenityId;
    }
}