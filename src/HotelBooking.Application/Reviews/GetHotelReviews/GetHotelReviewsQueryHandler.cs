using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Hotels;

namespace HotelBooking.Application.Reviews.GetHotelReviews;

public sealed class GetHotelReviewsQueryHandler
{
    private readonly IValidator<GetHotelReviewsQuery> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly IHotelReviewsQuery _hotelReviewsQuery;

    public GetHotelReviewsQueryHandler(
        IValidator<GetHotelReviewsQuery> validator,
        IHotelRepository hotelRepository,
        IHotelReviewsQuery hotelReviewsQuery)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _hotelReviewsQuery = hotelReviewsQuery;
    }

    public async Task<GetHotelReviewsResult> HandleAsync(
        GetHotelReviewsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            query,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new GetHotelReviewsResult(
                false,
                [],
                query.Page,
                query.PageSize,
                false,
                validationResult.ToApplicationErrors());
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            query.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new GetHotelReviewsResult(
                false,
                [],
                query.Page,
                query.PageSize,
                false,
                [HotelErrors.NotFound]);
        }

        var page = await _hotelReviewsQuery.GetAsync(
            query.HotelId,
            query.Page,
            query.PageSize,
            cancellationToken);

        return new GetHotelReviewsResult(
            true,
            page.Reviews,
            query.Page,
            query.PageSize,
            page.HasNextPage,
            []);
    }
}

public sealed record GetHotelReviewsResult(
    bool Succeeded,
    IReadOnlyCollection<HotelReviewItem> Reviews,
    int Page,
    int PageSize,
    bool HasNextPage,
    IReadOnlyCollection<ApplicationError> Errors);