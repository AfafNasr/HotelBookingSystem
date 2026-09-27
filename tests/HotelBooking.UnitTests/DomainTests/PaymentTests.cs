using HotelBooking.Domain.Payments;

namespace HotelBooking.UnitTests.DomainTests;

public sealed class PaymentTests
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

    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreatePendingPayment()
    {
        // Act
        var payment =
            new Payment(
                bookingId: 10,
                amount: 250m,
                currency: "USD",
                createdAt: CreatedAt);

        // Assert
        Assert.Equal(10, payment.BookingId);
        Assert.Equal(250m, payment.Amount);
        Assert.Equal("usd", payment.Currency);

        Assert.Equal(
            PaymentStatus.Pending,
            payment.Status);

        Assert.Equal(
            CreatedAt,
            payment.CreatedAt);

        Assert.Null(
            payment.UpdatedAt);

        Assert.Null(
            payment.ProviderPaymentIntentId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenBookingIdIsInvalid_ShouldThrowArgumentOutOfRangeException(
        int bookingId)
    {
        // Act
        var action =
            () => new Payment(
                bookingId,
                250m,
                "usd",
                CreatedAt);

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                action);

        Assert.Equal(
            "bookingId",
            exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WhenAmountIsInvalid_ShouldThrowArgumentOutOfRangeException(
        decimal amount)
    {
        // Act
        var action =
            () => new Payment(
                10,
                amount,
                "usd",
                CreatedAt);

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                action);

        Assert.Equal(
            "amount",
            exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenCurrencyIsEmpty_ShouldThrowArgumentException(
        string currency)
    {
        // Act
        var action =
            () => new Payment(
                10,
                250m,
                currency,
                CreatedAt);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(
                action);

        Assert.Equal(
            "currency",
            exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldTrimAndNormalizeCurrencyToLowercase()
    {
        // Act
        var payment =
            new Payment(
                10,
                250m,
                "  USD  ",
                CreatedAt);

        // Assert
        Assert.Equal(
            "usd",
            payment.Currency);
    }

    [Fact]
    public void AttachProviderPaymentIntent_WhenPending_ShouldAttachIntent()
    {
        // Arrange
        var payment =
            CreatePayment();

        var updatedAt =
            CreatedAt.AddMinutes(1);

        // Act
        payment.AttachProviderPaymentIntent(
            "pi_123",
            updatedAt);

        // Assert
        Assert.Equal(
            "pi_123",
            payment.ProviderPaymentIntentId);

        Assert.Equal(
            updatedAt,
            payment.UpdatedAt);

        Assert.Equal(
            PaymentStatus.Pending,
            payment.Status);
    }

    [Fact]
    public void AttachProviderPaymentIntent_ShouldTrimIntentId()
    {
        // Arrange
        var payment =
            CreatePayment();

        // Act
        payment.AttachProviderPaymentIntent(
            "  pi_123  ",
            CreatedAt.AddMinutes(1));

        // Assert
        Assert.Equal(
            "pi_123",
            payment.ProviderPaymentIntentId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AttachProviderPaymentIntent_WhenIntentIdIsEmpty_ShouldThrowArgumentException(
        string providerPaymentIntentId)
    {
        // Arrange
        var payment =
            CreatePayment();

        // Act
        var action =
            () => payment.AttachProviderPaymentIntent(
                providerPaymentIntentId,
                CreatedAt.AddMinutes(1));

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(
                action);

        Assert.Equal(
            "providerPaymentIntentId",
            exception.ParamName);

        Assert.Null(
            payment.ProviderPaymentIntentId);
    }

    [Fact]
    public void AttachProviderPaymentIntent_WhenSameIntentIsAttachedAgain_ShouldBeIdempotent()
    {
        // Arrange
        var payment =
            CreatePayment();

        var originalUpdatedAt =
            CreatedAt.AddMinutes(1);

        payment.AttachProviderPaymentIntent(
            "pi_123",
            originalUpdatedAt);

        // Act
        payment.AttachProviderPaymentIntent(
            "pi_123",
            CreatedAt.AddMinutes(5));

        // Assert
        Assert.Equal(
            "pi_123",
            payment.ProviderPaymentIntentId);

        Assert.Equal(
            originalUpdatedAt,
            payment.UpdatedAt);
    }

    [Fact]
    public void AttachProviderPaymentIntent_WhenSameTrimmedIntentIsAttachedAgain_ShouldBeIdempotent()
    {
        // Arrange
        var payment =
            CreatePayment();

        var originalUpdatedAt =
            CreatedAt.AddMinutes(1);

        payment.AttachProviderPaymentIntent(
            "pi_123",
            originalUpdatedAt);

        // Act
        payment.AttachProviderPaymentIntent(
            "  pi_123  ",
            CreatedAt.AddMinutes(5));

        // Assert
        Assert.Equal(
            "pi_123",
            payment.ProviderPaymentIntentId);

        Assert.Equal(
            originalUpdatedAt,
            payment.UpdatedAt);
    }

    [Fact]
    public void AttachProviderPaymentIntent_WhenDifferentIntentIsAlreadyAttached_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payment =
            CreatePayment();

        payment.AttachProviderPaymentIntent(
            "pi_123",
            CreatedAt.AddMinutes(1));

        // Act
        var action =
            () => payment.AttachProviderPaymentIntent(
                "pi_999",
                CreatedAt.AddMinutes(2));

        // Assert
        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Equal(
            "A different provider payment intent is already attached.",
            exception.Message);

        Assert.Equal(
            "pi_123",
            payment.ProviderPaymentIntentId);
    }

    [Fact]
    public void MarkSucceeded_WhenPendingAndIntentExists_ShouldMarkPaymentAsSucceeded()
    {
        // Arrange
        var payment =
            CreatePayment();

        payment.AttachProviderPaymentIntent(
            "pi_123",
            CreatedAt.AddMinutes(1));

        var succeededAt =
            CreatedAt.AddMinutes(2);

        // Act
        payment.MarkSucceeded(
            succeededAt);

        // Assert
        Assert.Equal(
            PaymentStatus.Succeeded,
            payment.Status);

        Assert.Equal(
            succeededAt,
            payment.UpdatedAt);
    }

    [Fact]
    public void MarkSucceeded_WhenProviderIntentDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payment =
            CreatePayment();

        // Act
        var action =
            () => payment.MarkSucceeded(
                CreatedAt.AddMinutes(2));

        // Assert
        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Equal(
            "A payment cannot succeed without a provider payment intent.",
            exception.Message);

        Assert.Equal(
            PaymentStatus.Pending,
            payment.Status);
    }

    [Fact]
    public void MarkSucceeded_WhenAlreadySucceeded_ShouldBeIdempotent()
    {
        // Arrange
        var payment =
            CreatePayment();

        payment.AttachProviderPaymentIntent(
            "pi_123",
            CreatedAt.AddMinutes(1));

        var originalSucceededAt =
            CreatedAt.AddMinutes(2);

        payment.MarkSucceeded(
            originalSucceededAt);

        // Act
        payment.MarkSucceeded(
            CreatedAt.AddMinutes(10));

        // Assert
        Assert.Equal(
            PaymentStatus.Succeeded,
            payment.Status);

        Assert.Equal(
            originalSucceededAt,
            payment.UpdatedAt);
    }

    private static Payment CreatePayment()
    {
        return new Payment(
            bookingId: 10,
            amount: 250m,
            currency: "usd",
            createdAt: CreatedAt);
    }
}