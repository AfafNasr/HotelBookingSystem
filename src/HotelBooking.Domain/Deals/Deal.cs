using HotelBooking.Domain.Hotels;

namespace HotelBooking.Domain.Deals;

public sealed class Deal
{
    public int Id { get; private set; }

    public int HotelId { get; private set; }

    public decimal DiscountPercentage { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }
    public Hotel Hotel { get; private set; } = null!;

    private Deal()
    {
    }

    public Deal(
        int hotelId,
        decimal discountPercentage,
        DateOnly startDate,
        DateOnly endDate,
        DateTime createdAt)
    {
        HotelId = hotelId;
        DiscountPercentage = discountPercentage;
        StartDate = startDate;
        EndDate = endDate;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }
}