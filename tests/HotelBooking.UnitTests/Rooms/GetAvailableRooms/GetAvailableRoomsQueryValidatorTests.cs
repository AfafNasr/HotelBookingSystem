using FluentValidation.TestHelper;
using HotelBooking.Application.Rooms.GetAvailableRooms;
using HotelBooking.Domain.Rooms;

namespace HotelBooking.UnitTests.Rooms.GetAvailableRooms;

public sealed class GetAvailableRoomsQueryValidatorTests
{
    private readonly GetAvailableRoomsQueryValidator _validator = new();

    private static GetAvailableRoomsQuery CreateValidQuery()
    {
        return new GetAvailableRoomsQuery(
            HotelId: 1,
            RoomType: RoomType.Standard,
            CheckInDate: new DateOnly(2026, 10, 10),
            CheckOutDate: new DateOnly(2026, 10, 15),
            Adults: 2,
            Children: 1);
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenQueryIsValid()
    {
        // Arrange
        var query = CreateValidQuery();

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenHotelIdIsInvalid()
    {
        // Arrange
        var query = CreateValidQuery() with
        {
            HotelId = 0
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HotelId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRoomTypeIsInvalid()
    {
        // Arrange
        var query = CreateValidQuery() with
        {
            RoomType = (RoomType)999
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoomType);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCheckInDateIsEmpty()
    {
        // Arrange
        var query = CreateValidQuery() with
        {
            CheckInDate = default
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CheckInDate);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCheckOutDateEqualsCheckInDate()
    {
        // Arrange
        var query = CreateValidQuery();

        query = query with
        {
            CheckOutDate = query.CheckInDate
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CheckOutDate);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCheckOutDateIsBeforeCheckInDate()
    {
        // Arrange
        var query = CreateValidQuery() with
        {
            CheckInDate = new DateOnly(2026, 10, 15),
            CheckOutDate = new DateOnly(2026, 10, 10)
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CheckOutDate);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAdultsIsNotPositive()
    {
        // Arrange
        var query = CreateValidQuery() with
        {
            Adults = 0
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Adults);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenChildrenIsNegative()
    {
        // Arrange
        var query = CreateValidQuery() with
        {
            Children = -1
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Children);
    }
}