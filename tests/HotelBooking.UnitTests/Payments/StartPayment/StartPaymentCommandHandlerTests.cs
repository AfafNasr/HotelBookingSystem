using System.Reflection;
using HotelBooking.Application.Bookings;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Payments;
using HotelBooking.Application.Payments.Gateway;
using HotelBooking.Application.Payments.StartPayment;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Payments;
using Moq;

namespace HotelBooking.UnitTests.Payments.StartPayment;

public sealed class StartPaymentCommandHandlerTests
{
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private readonly StartPaymentCommandValidator _validator = new();

    private readonly DateTimeOffset _now =
        new(
            2026,
            9,
            26,
            12,
            0,
            0,
            TimeSpan.Zero);

    private StartPaymentCommandHandler CreateHandler()
    {
        var timeProvider = new TestTimeProvider(_now);

        return new StartPaymentCommandHandler(
            _validator,
            _bookingRepository.Object,
            _paymentRepository.Object,
            _paymentGateway.Object,
            _currentUserService.Object,
            timeProvider);
    }

    private static StartPaymentCommand CreateValidCommand()
    {
        return new StartPaymentCommand(
            BookingId: 1);
    }

    private Booking CreatePendingBooking(
        string userId = "user-1",
        DateTime? expiresAt = null,
        int bookingId = 1)
    {
        var booking = new Booking(
            userId,
            hotelId: 1,
            checkInDate: new DateOnly(2026, 10, 10),
            checkOutDate: new DateOnly(2026, 10, 12),
            expiresAt: expiresAt ??
                       _now.UtcDateTime.AddMinutes(15),
            guestFullName: "Test User",
            guestEmail: "test@example.com",
            guestPhoneNumber: "0590000000",
            specialRequests: null,
            totalAmount: 200m,
            createdAt: _now.UtcDateTime);

        SetEntityId(
            booking,
            bookingId);

        return booking;
    }

    private static void SetEntityId<T>(
        T entity,
        int id)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var field = typeof(T).GetField(
            "<Id>k__BackingField",
            BindingFlags.Instance |
            BindingFlags.NonPublic);

        if (field is null)
        {
            throw new InvalidOperationException(
                $"Could not find the Id backing field on " +
                $"{typeof(T).FullName}.");
        }

        field.SetValue(
            entity,
            id);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenBookingIdIsInvalid()
    {
        // Arrange
        var command = new StartPaymentCommand(
            BookingId: 0);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.PaymentId);
        Assert.Null(result.ClientSecret);
        Assert.NotEmpty(result.Errors);

        _bookingRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _paymentRepository.VerifyNoOtherCalls();
        _paymentGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthenticationError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var command = CreateValidCommand();

        _currentUserService
            .Setup(service => service.UserId)
            .Returns((string?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.PaymentId);
        Assert.Null(result.ClientSecret);

        Assert.Single(result.Errors);

        _bookingRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _paymentRepository.VerifyNoOtherCalls();
        _paymentGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var command = CreateValidCommand();

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(
                command.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.PaymentId);
        Assert.Null(result.ClientSecret);
        Assert.Single(result.Errors);

        _paymentRepository.Verify(
            repository => repository.GetByBookingIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _paymentGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAccessDenied_WhenBookingBelongsToAnotherUser()
    {
        // Arrange
        var command = CreateValidCommand();

        var booking = CreatePendingBooking(
            userId: "another-user");

        Assert.Equal(
            command.BookingId,
            booking.Id);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(
                command.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.PaymentId);
        Assert.Null(result.ClientSecret);
        Assert.Single(result.Errors);

        _paymentRepository.Verify(
            repository => repository.GetByBookingIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _paymentGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotPendingPayment_WhenBookingIsConfirmed()
    {
        // Arrange
        var command = CreateValidCommand();

        var booking = CreatePendingBooking();

        Assert.Equal(
            command.BookingId,
            booking.Id);

        booking.Confirm(
            "CONF-123",
            _now.UtcDateTime);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(
                command.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.PaymentId);
        Assert.Null(result.ClientSecret);
        Assert.Single(result.Errors);

        _paymentRepository.Verify(
            repository => repository.GetByBookingIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _paymentGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnPaymentHoldExpired_WhenBookingHoldHasExpired()
    {
        // Arrange
        var command = CreateValidCommand();

        var booking = CreatePendingBooking(
            expiresAt: _now.UtcDateTime);

        Assert.Equal(
            command.BookingId,
            booking.Id);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(
                command.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.PaymentId);
        Assert.Null(result.ClientSecret);
        Assert.Single(result.Errors);

        _paymentRepository.Verify(
            repository => repository.GetByBookingIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _paymentGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotPending_WhenExistingPaymentIsNotPending()
    {
        // Arrange
        var command = CreateValidCommand();

        var booking = CreatePendingBooking();

        Assert.Equal(
            command.BookingId,
            booking.Id);

        var payment = new Payment(
            bookingId: booking.Id,
            amount: booking.TotalAmount,
            currency: "usd",
            createdAt: _now.UtcDateTime);

        SetEntityId(
            payment,
            10);

        // A successful payment should already be associated
        // with a provider payment intent.
        payment.AttachProviderPaymentIntent(
            "pi_succeeded",
            _now.UtcDateTime);

        payment.MarkSucceeded(
            _now.UtcDateTime);

        Assert.Equal(
            PaymentStatus.Succeeded,
            payment.Status);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(
                command.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _paymentRepository
            .Setup(repository => repository.GetByBookingIdAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.PaymentId);
        Assert.Null(result.ClientSecret);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Payment.NotPending",
            error.Code);

        _paymentGateway.Verify(
            gateway => gateway.GetPaymentIntentAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _paymentGateway.Verify(
            gateway => gateway.CreatePaymentIntentAsync(
                It.IsAny<CreatePaymentIntentRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _paymentRepository.Verify(
            repository => repository.Add(
                It.IsAny<Payment>()),
            Times.Never);

        _paymentRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReuseExistingPaymentIntent_WhenProviderIntentAlreadyExists()
    {
        // Arrange
        var command = CreateValidCommand();

        var booking = CreatePendingBooking();

        // If this assertion fails, the test entity setup is wrong
        // before Payment is even created.
        Assert.Equal(
            1,
            booking.Id);

        var payment = new Payment(
            bookingId: booking.Id,
            amount: booking.TotalAmount,
            currency: "usd",
            createdAt: _now.UtcDateTime);

        SetEntityId(
            payment,
            10);

        payment.AttachProviderPaymentIntent(
            "pi_existing",
            _now.UtcDateTime);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(
                command.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _paymentRepository
            .Setup(repository => repository.GetByBookingIdAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _paymentGateway
            .Setup(gateway => gateway.GetPaymentIntentAsync(
                "pi_existing",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CreatePaymentIntentResult(
                    "pi_existing",
                    "client_secret_existing"));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Equal(
            10,
            result.PaymentId);

        Assert.Equal(
            "client_secret_existing",
            result.ClientSecret);

        _paymentGateway.Verify(
            gateway => gateway.GetPaymentIntentAsync(
                "pi_existing",
                It.IsAny<CancellationToken>()),
            Times.Once);

        _paymentGateway.Verify(
            gateway => gateway.CreatePaymentIntentAsync(
                It.IsAny<CreatePaymentIntentRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreatePaymentAndPaymentIntent_WhenPaymentDoesNotExist()
    {
        // Arrange
        var command = CreateValidCommand();

        var booking = CreatePendingBooking();

        // Critical assertion:
        // Payment cannot legally be created with BookingId = 0.
        Assert.Equal(
            command.BookingId,
            booking.Id);

        Assert.True(
            booking.Id > 0);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(
                command.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _paymentRepository
            .Setup(repository => repository.GetByBookingIdAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        Payment? createdPayment = null;

        _paymentRepository
            .Setup(repository => repository.Add(
                It.IsAny<Payment>()))
            .Callback<Payment>(payment =>
            {
                createdPayment = payment;

                // Simulate database-generated identity after insertion.
                SetEntityId(
                    payment,
                    10);
            });

        _paymentGateway
            .Setup(gateway => gateway.CreatePaymentIntentAsync(
                It.IsAny<CreatePaymentIntentRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CreatePaymentIntentResult(
                    "pi_new",
                    "client_secret_new"));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.NotNull(createdPayment);

        Assert.Equal(
            booking.Id,
            createdPayment.BookingId);

        Assert.Equal(
            booking.TotalAmount,
            createdPayment.Amount);

        Assert.Equal(
            "usd",
            createdPayment.Currency);

        Assert.Equal(
            PaymentStatus.Pending,
            createdPayment.Status);

        Assert.Equal(
            "pi_new",
            createdPayment.ProviderPaymentIntentId);

        Assert.Equal(
            10,
            result.PaymentId);

        Assert.Equal(
            "client_secret_new",
            result.ClientSecret);

        _paymentRepository.Verify(
            repository => repository.Add(
                It.IsAny<Payment>()),
            Times.Once);

        _paymentRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _paymentGateway.Verify(
            gateway => gateway.CreatePaymentIntentAsync(
                It.Is<CreatePaymentIntentRequest>(
                    request =>
                        request.Amount == booking.TotalAmount &&
                        request.Currency == "usd" &&
                        request.PaymentId == 10 &&
                        request.BookingId == booking.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _paymentGateway.Verify(
            gateway => gateway.GetPaymentIntentAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReuseExistingPendingPayment_WhenPaymentExistsWithoutProviderIntent()
    {
        // Arrange
        var command = CreateValidCommand();

        var booking = CreatePendingBooking();

        Assert.Equal(
            1,
            booking.Id);

        var payment = new Payment(
            bookingId: booking.Id,
            amount: booking.TotalAmount,
            currency: "usd",
            createdAt: _now.UtcDateTime);

        SetEntityId(
            payment,
            10);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(
                command.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _paymentRepository
            .Setup(repository => repository.GetByBookingIdAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _paymentGateway
            .Setup(gateway => gateway.CreatePaymentIntentAsync(
                It.IsAny<CreatePaymentIntentRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CreatePaymentIntentResult(
                    "pi_existing_payment",
                    "client_secret_existing_payment"));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Equal(
            10,
            result.PaymentId);

        Assert.Equal(
            "client_secret_existing_payment",
            result.ClientSecret);

        Assert.Equal(
            "pi_existing_payment",
            payment.ProviderPaymentIntentId);

        _paymentRepository.Verify(
            repository => repository.Add(
                It.IsAny<Payment>()),
            Times.Never);

        _paymentGateway.Verify(
            gateway => gateway.CreatePaymentIntentAsync(
                It.Is<CreatePaymentIntentRequest>(
                    request =>
                        request.PaymentId == 10 &&
                        request.BookingId == booking.Id &&
                        request.Amount == booking.TotalAmount &&
                        request.Currency == "usd"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _paymentRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public TestTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}