using FluentValidation;

namespace HotelBooking.Application.Payments.StartPayment;

public sealed class StartPaymentCommandValidator
    : AbstractValidator<StartPaymentCommand>
{
    public StartPaymentCommandValidator()
    {
        RuleFor(command => command.BookingId)
            .GreaterThan(0);
    }
}