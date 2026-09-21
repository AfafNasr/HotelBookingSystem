using FluentValidation;

namespace HotelBooking.Application.Deals.CreateDeal;

public sealed class CreateDealCommandValidator
    : AbstractValidator<CreateDealCommand>
{
    public CreateDealCommandValidator()
    {
        RuleFor(command => command.HotelId)
            .GreaterThan(0);

        RuleFor(command => command.DiscountPercentage)
            .GreaterThan(0)
            .LessThan(100);

        RuleFor(command => command.EndDate)
            .GreaterThanOrEqualTo(command => command.StartDate)
            .WithMessage(
                "End date must be on or after start date.");
    }
}