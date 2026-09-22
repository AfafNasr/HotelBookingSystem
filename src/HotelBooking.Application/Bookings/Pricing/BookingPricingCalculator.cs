using HotelBooking.Domain.Deals;

namespace HotelBooking.Application.Bookings.Pricing;

public sealed class BookingPricingCalculator
{
    public decimal CalculateRoomTotal(
        decimal pricePerNight,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        IReadOnlyCollection<Deal> deals)
    {
        decimal total = 0;

        // Checkout is an exclusive boundary.
        // Each date from check-in up to, but not including, check-out
        // represents one chargeable night.
        for (var night = checkInDate;
             night < checkOutDate;
             night = night.AddDays(1))
        {
            var applicableDeal = deals.FirstOrDefault(deal =>
                deal.StartDate <= night &&
                deal.EndDate >= night);

            if (applicableDeal is null)
            {
                total += pricePerNight;
                continue;
            }

            var discountAmount =
                pricePerNight * applicableDeal.DiscountPercentage / 100m;

            total += pricePerNight - discountAmount;
        }

        return decimal.Round(
            total,
            2,
            MidpointRounding.AwayFromZero);
    }
}