using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Hotels.SearchHotels;

public sealed record SearchHotelsResult(
    bool Succeeded,
    IReadOnlyCollection<SearchHotelItem> Hotels,
    int Page,
    int PageSize,
    bool HasNextPage,
    IReadOnlyCollection<ApplicationError> Errors);