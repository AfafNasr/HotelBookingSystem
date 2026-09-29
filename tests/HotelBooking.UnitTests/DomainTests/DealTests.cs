using HotelBooking.Domain.Deals;

namespace HotelBooking.UnitTests.DomainTests;

public sealed class DealTests
{
    private static readonly DateTime CreatedAt =
        new(
            2026,
            9,
            27,
            10,
            0,
            0,
            DateTimeKind.Utc);

    private static readonly DateOnly StartDate =
        new(2026, 10, 1);

    private static readonly DateOnly EndDate =
        new(2026, 10, 10);

    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateDeal()
    {
        // Act
        var deal =
            new Deal(
                hotelId: 5,
                discountPercentage: 20m,
                startDate: StartDate,
                endDate: EndDate,
                createdAt: CreatedAt);

        // Assert
        Assert.Equal(5, deal.HotelId);
        Assert.Equal(20m, deal.DiscountPercentage);
        Assert.Equal(StartDate, deal.StartDate);
        Assert.Equal(EndDate, deal.EndDate);
        Assert.Equal(CreatedAt, deal.CreatedAt);
        Assert.Equal(CreatedAt, deal.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenHotelIdIsInvalid_ShouldThrowArgumentOutOfRangeException(
        int hotelId)
    {
        // Act
        var action =
            () => new Deal(
                hotelId,
                20m,
                StartDate,
                EndDate,
                CreatedAt);

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                action);

        Assert.Equal(
            "hotelId",
            exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    [InlineData(101)]
    public void Constructor_WhenDiscountPercentageIsInvalid_ShouldThrowArgumentOutOfRangeException(
        decimal discountPercentage)
    {
        // Act
        var action =
            () => new Deal(
                5,
                discountPercentage,
                StartDate,
                EndDate,
                CreatedAt);

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                action);

        Assert.Equal(
            "discountPercentage",
            exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenEndDateIsBeforeStartDate_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidEndDate =
            StartDate.AddDays(-1);

        // Act
        var action =
            () => new Deal(
                5,
                20m,
                StartDate,
                invalidEndDate,
                CreatedAt);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(
                action);

        Assert.Equal(
            "endDate",
            exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenStartAndEndDateAreSame_ShouldCreateDeal()
    {
        // Act
        var deal =
            new Deal(
                5,
                20m,
                StartDate,
                StartDate,
                CreatedAt);

        // Assert
        Assert.Equal(
            StartDate,
            deal.StartDate);

        Assert.Equal(
            StartDate,
            deal.EndDate);
    }

    [Fact]
    public void Update_WhenDataIsValid_ShouldUpdateDeal()
    {
        // Arrange
        var deal =
            CreateDeal();

        var newStartDate =
            new DateOnly(
                2026,
                11,
                1);

        var newEndDate =
            new DateOnly(
                2026,
                11,
                15);

        var updatedAt =
            CreatedAt.AddDays(1);

        // Act
        deal.Update(
            35m,
            newStartDate,
            newEndDate,
            updatedAt);

        // Assert
        Assert.Equal(
            35m,
            deal.DiscountPercentage);

        Assert.Equal(
            newStartDate,
            deal.StartDate);

        Assert.Equal(
            newEndDate,
            deal.EndDate);

        Assert.Equal(
            updatedAt,
            deal.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    [InlineData(150)]
    public void Update_WhenDiscountPercentageIsInvalid_ShouldThrowArgumentOutOfRangeException(
        decimal discountPercentage)
    {
        // Arrange
        var deal =
            CreateDeal();

        // Act
        var action =
            () => deal.Update(
                discountPercentage,
                StartDate,
                EndDate,
                CreatedAt.AddDays(1));

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                action);

        Assert.Equal(
            "discountPercentage",
            exception.ParamName);
    }

    [Fact]
    public void Update_WhenEndDateIsBeforeStartDate_ShouldThrowArgumentException()
    {
        // Arrange
        var deal =
            CreateDeal();

        var newStartDate =
            new DateOnly(
                2026,
                11,
                10);

        var invalidEndDate =
            newStartDate.AddDays(-1);

        // Act
        var action =
            () => deal.Update(
                25m,
                newStartDate,
                invalidEndDate,
                CreatedAt.AddDays(1));

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(
                action);

        Assert.Equal(
            "endDate",
            exception.ParamName);
    }

    [Fact]
    public void Update_WhenValidationFails_ShouldNotModifyDeal()
    {
        // Arrange
        var deal =
            CreateDeal();

        var originalDiscount =
            deal.DiscountPercentage;

        var originalStartDate =
            deal.StartDate;

        var originalEndDate =
            deal.EndDate;

        var originalUpdatedAt =
            deal.UpdatedAt;

        // Act
        var action =
            () => deal.Update(
                100m,
                new DateOnly(2026, 11, 1),
                new DateOnly(2026, 11, 10),
                CreatedAt.AddDays(2));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            action);

        Assert.Equal(
            originalDiscount,
            deal.DiscountPercentage);

        Assert.Equal(
            originalStartDate,
            deal.StartDate);

        Assert.Equal(
            originalEndDate,
            deal.EndDate);

        Assert.Equal(
            originalUpdatedAt,
            deal.UpdatedAt);
    }

    private static Deal CreateDeal()
    {
        return new Deal(
            hotelId: 5,
            discountPercentage: 20m,
            startDate: StartDate,
            endDate: EndDate,
            createdAt: CreatedAt);
    }
}