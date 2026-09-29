using FluentValidation.TestHelper;
using HotelBooking.Application.Bookings.CreateBooking;

namespace HotelBooking.UnitTests.Bookings.CreateBooking;

public sealed class CreateBookingCommandValidatorTests
{
    private static readonly DateTimeOffset FixedUtcNow =
    new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly CreateBookingCommandValidator _validator =
        new(new FixedTimeProvider(FixedUtcNow));

    private static CreateBookingCommand CreateValidCommand()
    {
        return new CreateBookingCommand(
            HotelId: 1,
            RoomIds: [1, 2],
            CheckInDate: new DateOnly(2026, 10, 10),
            CheckOutDate: new DateOnly(2026, 10, 12),
            GuestFullName: "Test User",
            GuestEmail: "test@example.com",
            GuestPhoneNumber: "+970599123456",
            SpecialRequests: "Late check-in.");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCheckInDateIsInThePast()
    {
        var command = CreateValidCommand() with
        {
            CheckInDate = new DateOnly(2026, 9, 30),
            CheckOutDate = new DateOnly(2026, 10, 2)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            x => x.CheckInDate);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenCheckInDateIsToday()
    {
        var command = CreateValidCommand() with
        {
            CheckInDate = new DateOnly(2026, 10, 1),
            CheckOutDate = new DateOnly(2026, 10, 2)
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(
            x => x.CheckInDate);
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenCommandIsValid()
    {
        var command = CreateValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenHotelIdIsInvalid()
    {
        var command = CreateValidCommand() with
        {
            HotelId = 0
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.HotelId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRoomIdsIsEmpty()
    {
        var command = CreateValidCommand() with
        {
            RoomIds = []
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RoomIds);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRoomIdIsInvalid()
    {
        var command = CreateValidCommand() with
        {
            RoomIds = [1, 0]
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("RoomIds[1]");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRoomIdsContainDuplicates()
    {
        var command = CreateValidCommand() with
        {
            RoomIds = [1, 1]
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RoomIds);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCheckOutDateEqualsCheckInDate()
    {
        var command = CreateValidCommand();

        command = command with
        {
            CheckOutDate = command.CheckInDate
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CheckOutDate);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCheckOutDateIsBeforeCheckInDate()
    {
        var command = CreateValidCommand() with
        {
            CheckInDate = new DateOnly(2026, 10, 12),
            CheckOutDate = new DateOnly(2026, 10, 10)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CheckOutDate);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenGuestFullNameIsEmpty()
    {
        var command = CreateValidCommand() with
        {
            GuestFullName = ""
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.GuestFullName);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenGuestFullNameExceedsMaximumLength()
    {
        var command = CreateValidCommand() with
        {
            GuestFullName = new string('A', 201)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.GuestFullName);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenGuestEmailIsEmpty()
    {
        var command = CreateValidCommand() with
        {
            GuestEmail = ""
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.GuestEmail);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenGuestEmailIsInvalid()
    {
        var command = CreateValidCommand() with
        {
            GuestEmail = "not-an-email"
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.GuestEmail);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenGuestEmailExceedsMaximumLength()
    {
        var command = CreateValidCommand() with
        {
            GuestEmail =
                $"{new string('a', 310)}@example.com"
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.GuestEmail);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenGuestPhoneNumberIsEmpty()
    {
        var command = CreateValidCommand() with
        {
            GuestPhoneNumber = ""
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.GuestPhoneNumber);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenGuestPhoneNumberExceedsMaximumLength()
    {
        var command = CreateValidCommand() with
        {
            GuestPhoneNumber = new string('1', 31)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.GuestPhoneNumber);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenSpecialRequestsExceedsMaximumLength()
    {
        var command = CreateValidCommand() with
        {
            SpecialRequests = new string('A', 2001)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.SpecialRequests);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenSpecialRequestsIsNull()
    {
        var command = CreateValidCommand() with
        {
            SpecialRequests = null
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.SpecialRequests);
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