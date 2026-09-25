using FluentValidation;

namespace HotelBooking.Application.Deals.UpdateDeal;

public sealed class UpdateDealCommandValidator
    : AbstractValidator<UpdateDealCommand>
{
    public UpdateDealCommandValidator()
    {
        RuleFor(command => command.DealId)
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