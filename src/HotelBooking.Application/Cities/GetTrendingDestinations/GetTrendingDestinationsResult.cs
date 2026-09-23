using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Cities.GetTrendingDestinations;

public sealed record GetTrendingDestinationsResult(
    bool Succeeded,
    IReadOnlyCollection<TrendingDestination> Destinations,
    IReadOnlyCollection<ApplicationError> Errors);