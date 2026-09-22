using FluentValidation;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Hotels.SearchHotels;

public sealed class SearchHotelsQueryHandler
{
    private readonly IValidator<SearchHotelsQuery> _validator;
    private readonly IHotelSearchQuery _hotelSearchQuery;

    public SearchHotelsQueryHandler(
        IValidator<SearchHotelsQuery> validator,
        IHotelSearchQuery hotelSearchQuery)
    {
        _validator = validator;
        _hotelSearchQuery = hotelSearchQuery;
    }

    public async Task<SearchHotelsResult> HandleAsync(
        SearchHotelsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            query,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new SearchHotelsResult(
                false,
                Array.Empty<SearchHotelItem>(),
                query.Page,
                query.PageSize,
                false,
                validationResult.ToApplicationErrors());
        }

        var now = DateTime.UtcNow;

        var page = await _hotelSearchQuery.SearchAsync(
            query,
            now,
            cancellationToken);

        return new SearchHotelsResult(
            true,
            page.Hotels,
            query.Page,
            query.PageSize,
            page.HasNextPage,
            Array.Empty<ApplicationError>());
    }
}