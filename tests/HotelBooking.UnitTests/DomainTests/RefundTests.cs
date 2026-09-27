using HotelBooking.Domain.Payments;

namespace HotelBooking.UnitTests.DomainTests;

public sealed class RefundTests
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
    public void Constructor_WhenDataIsValid_ShouldCreatePendingRefund()
    {
        // Act
        var refund =
            new Refund(
                paymentId: 15,
                amount: 100m,
                currency: "USD",
                createdAt: CreatedAt);

        // Assert
        Assert.Equal(15, refund.PaymentId);
        Assert.Equal(100m, refund.Amount);
        Assert.Equal("usd", refund.Currency);

        Assert.Equal(
            RefundStatus.Pending,
            refund.Status);

        Assert.Equal(
            CreatedAt,
            refund.CreatedAt);

        Assert.Null(
            refund.UpdatedAt);

        Assert.Null(
            refund.ProviderRefundId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenPaymentIdIsInvalid_ShouldThrowArgumentOutOfRangeException(
        int paymentId)
    {
        // Act
        var action =
            () => new Refund(
                paymentId,
                100m,
                "usd",
                CreatedAt);

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                action);

        Assert.Equal(
            "paymentId",
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
            () => new Refund(
                15,
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
            () => new Refund(
                15,
                100m,
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
        var refund =
            new Refund(
                15,
                100m,
                "  USD  ",
                CreatedAt);

        // Assert
        Assert.Equal(
            "usd",
            refund.Currency);
    }

    [Fact]
    public void MarkSucceeded_WhenPending_ShouldMarkRefundAsSucceeded()
    {
        // Arrange
        var refund =
            CreateRefund();

        var succeededAt =
            CreatedAt.AddMinutes(5);

        // Act
        refund.MarkSucceeded(
            "re_123",
            succeededAt);

        // Assert
        Assert.Equal(
            RefundStatus.Succeeded,
            refund.Status);

        Assert.Equal(
            "re_123",
            refund.ProviderRefundId);

        Assert.Equal(
            succeededAt,
            refund.UpdatedAt);
    }

    [Fact]
    public void MarkSucceeded_ShouldTrimProviderRefundId()
    {
        // Arrange
        var refund =
            CreateRefund();

        // Act
        refund.MarkSucceeded(
            "  re_123  ",
            CreatedAt.AddMinutes(5));

        // Assert
        Assert.Equal(
            "re_123",
            refund.ProviderRefundId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkSucceeded_WhenProviderRefundIdIsEmpty_ShouldThrowArgumentException(
        string providerRefundId)
    {
        // Arrange
        var refund =
            CreateRefund();

        // Act
        var action =
            () => refund.MarkSucceeded(
                providerRefundId,
                CreatedAt.AddMinutes(5));

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(
                action);

        Assert.Equal(
            "providerRefundId",
            exception.ParamName);

        Assert.Equal(
            RefundStatus.Pending,
            refund.Status);

        Assert.Null(
            refund.ProviderRefundId);
    }

    [Fact]
    public void MarkSucceeded_WhenAlreadySucceededWithSameRefundId_ShouldBeIdempotent()
    {
        // Arrange
        var refund =
            CreateRefund();

        var originalSucceededAt =
            CreatedAt.AddMinutes(5);

        refund.MarkSucceeded(
            "re_123",
            originalSucceededAt);

        // Act
        refund.MarkSucceeded(
            "re_123",
            CreatedAt.AddMinutes(10));

        // Assert
        Assert.Equal(
            RefundStatus.Succeeded,
            refund.Status);

        Assert.Equal(
            "re_123",
            refund.ProviderRefundId);

        Assert.Equal(
            originalSucceededAt,
            refund.UpdatedAt);
    }

    [Fact]
    public void MarkSucceeded_WhenAlreadySucceededWithSameTrimmedRefundId_ShouldBeIdempotent()
    {
        // Arrange
        var refund =
            CreateRefund();

        var originalSucceededAt =
            CreatedAt.AddMinutes(5);

        refund.MarkSucceeded(
            "re_123",
            originalSucceededAt);

        // Act
        refund.MarkSucceeded(
            "  re_123  ",
            CreatedAt.AddMinutes(10));

        // Assert
        Assert.Equal(
            RefundStatus.Succeeded,
            refund.Status);

        Assert.Equal(
            originalSucceededAt,
            refund.UpdatedAt);
    }

    [Fact]
    public void MarkSucceeded_WhenAlreadySucceededWithDifferentRefundId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var refund =
            CreateRefund();

        refund.MarkSucceeded(
            "re_123",
            CreatedAt.AddMinutes(5));

        // Act
        var action =
            () => refund.MarkSucceeded(
                "re_999",
                CreatedAt.AddMinutes(10));

        // Assert
        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Equal(
            "A different provider refund is already attached.",
            exception.Message);

        Assert.Equal(
            "re_123",
            refund.ProviderRefundId);

        Assert.Equal(
            RefundStatus.Succeeded,
            refund.Status);
    }

    [Fact]
    public void MarkFailed_WhenPending_ShouldMarkRefundAsFailed()
    {
        // Arrange
        var refund =
            CreateRefund();

        var failedAt =
            CreatedAt.AddMinutes(5);

        // Act
        refund.MarkFailed(
            failedAt);

        // Assert
        Assert.Equal(
            RefundStatus.Failed,
            refund.Status);

        Assert.Equal(
            failedAt,
            refund.UpdatedAt);
    }

    [Fact]
    public void MarkFailed_WhenAlreadyFailed_ShouldBeIdempotent()
    {
        // Arrange
        var refund =
            CreateRefund();

        var originalFailedAt =
            CreatedAt.AddMinutes(5);

        refund.MarkFailed(
            originalFailedAt);

        // Act
        refund.MarkFailed(
            CreatedAt.AddMinutes(10));

        // Assert
        Assert.Equal(
            RefundStatus.Failed,
            refund.Status);

        Assert.Equal(
            originalFailedAt,
            refund.UpdatedAt);
    }

    [Fact]
    public void MarkFailed_WhenAlreadySucceeded_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var refund =
            CreateRefund();

        refund.MarkSucceeded(
            "re_123",
            CreatedAt.AddMinutes(5));

        // Act
        var action =
            () => refund.MarkFailed(
                CreatedAt.AddMinutes(10));

        // Assert
        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Equal(
            "Only a pending refund can fail.",
            exception.Message);

        Assert.Equal(
            RefundStatus.Succeeded,
            refund.Status);
    }

    [Fact]
    public void MarkSucceeded_WhenAlreadyFailed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var refund =
            CreateRefund();

        refund.MarkFailed(
            CreatedAt.AddMinutes(5));

        // Act
        var action =
            () => refund.MarkSucceeded(
                "re_123",
                CreatedAt.AddMinutes(10));

        // Assert
        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Equal(
            "Only a pending refund can succeed.",
            exception.Message);

        Assert.Equal(
            RefundStatus.Failed,
            refund.Status);

        Assert.Null(
            refund.ProviderRefundId);
    }

    private static Refund CreateRefund()
    {
        return new Refund(
            paymentId: 15,
            amount: 100m,
            currency: "usd",
            createdAt: CreatedAt);
    }
}