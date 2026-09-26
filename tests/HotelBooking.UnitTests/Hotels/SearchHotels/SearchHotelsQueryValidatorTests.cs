using FluentValidation.TestHelper;
using HotelBooking.Application.Hotels.SearchHotels;
using HotelBooking.Domain.Hotels;

namespace HotelBooking.UnitTests.Hotels.SearchHotels;

public sealed class SearchHotelsQueryValidatorTests
{
    private static readonly DateTimeOffset FixedUtcNow =
        new(
            2026,
            9,
            26,
            12,
            0,
            0,
            TimeSpan.Zero);

    private readonly SearchHotelsQueryValidator _validator =
        new(new FixedTimeProvider(FixedUtcNow));

    private static SearchHotelsQuery CreateValidQuery()
    {
        return new SearchHotelsQuery(
            Destination: "Test City",
            CheckInDate: new DateOnly(2026, 9, 26),
            CheckOutDate: new DateOnly(2026, 9, 28),
            Adults: 2,
            Children: 1,
            Rooms: 1);
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenQueryIsValid()
    {
        var query = CreateValidQuery();

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenDestinationIsEmpty()
    {
        var query = CreateValidQuery() with
        {
            Destination = ""
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            query => query.Destination);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenDestinationExceedsMaximumLength()
    {
        var query = CreateValidQuery() with
        {
            Destination = new string('A', 151)
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            query => query.Destination);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCheckInDateIsInPast()
    {
        var query = CreateValidQuery() with
        {
            CheckInDate = new DateOnly(2026, 9, 25),
            CheckOutDate = new DateOnly(2026, 9, 28)
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            query => query.CheckInDate);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenCheckInDateIsToday()
    {
        var query = CreateValidQuery() with
        {
            CheckInDate = new DateOnly(2026, 9, 26),
            CheckOutDate = new DateOnly(2026, 9, 27)
        };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(
            query => query.CheckInDate);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCheckOutDateIsNotAfterCheckInDate()
    {
        var query = CreateValidQuery() with
        {
            CheckInDate = new DateOnly(2026, 9, 27),
            CheckOutDate = new DateOnly(2026, 9, 27)
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            query => query.CheckOutDate);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAdultsIsNotPositive()
    {
        var query = CreateValidQuery() with
        {
            Adults = 0
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            query => query.Adults);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenChildrenIsNegative()
    {
        var query = CreateValidQuery() with
        {
            Children = -1
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            query => query.Children);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRoomsIsNotPositive()
    {
        var query = CreateValidQuery() with
        {
            Rooms = 0
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            query => query.Rooms);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenMinPriceIsNegative()
    {
        var query = CreateValidQuery() with
        {
            MinPrice = -1m
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            query => query.MinPrice);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenMaxPriceIsNotPositive()
    {
        var query = CreateValidQuery() with
        {
            MaxPrice = 0m
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            query => query.MaxPrice);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenMinPriceIsGreaterThanMaxPrice()
    {
        var query = CreateValidQuery() with
        {
            MinPrice = 200m,
            MaxPrice = 100m
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            query => query);
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenOptionalPricesAreNull()
    {
        var query = CreateValidQuery() with
        {
            MinPrice = null,
            MaxPrice = null
        };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(
            query => query.MinPrice);

        result.ShouldNotHaveValidationErrorFor(
            query => query.MaxPrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_ShouldHaveError_WhenStarRatingIsOutsideAllowedRange(
        int starRating)
    {
        var query = CreateValidQuery() with
        {
            StarRating = starRating
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            query => query.StarRating);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCategoryIsInvalid()
    {
        var query = CreateValidQuery() with
        {
            Category = (HotelCategory)999
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            query => query.Category);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAmenityIdIsInvalid()
    {
        var query = CreateValidQuery() with
        {
            AmenityIds = [1, 0, 3]
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            "AmenityIds[1]");
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenAmenityIdsAreNull()
    {
        var query = CreateValidQuery() with
        {
            AmenityIds = null
        };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(
            query => query.AmenityIds);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPageIsNotPositive()
    {
        var query = CreateValidQuery() with
        {
            Page = 0
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            query => query.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validate_ShouldHaveError_WhenPageSizeIsOutsideAllowedRange(
        int pageSize)
    {
        var query = CreateValidQuery() with
        {
            PageSize = pageSize
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            query => query.PageSize);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenSortByIsInvalid()
    {
        var query = CreateValidQuery() with
        {
            SortBy = (HotelSearchSort)999
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(
            query => query.SortBy);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}