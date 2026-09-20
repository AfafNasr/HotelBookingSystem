using FluentValidation;

namespace HotelBooking.Application.Hotels.CreateHotel;

public sealed class CreateHotelCommandValidator
    : AbstractValidator<CreateHotelCommand>
{
    public CreateHotelCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.CityId)
            .GreaterThan(0);

        RuleFor(command => command.OwnerId)
            .NotEmpty();

        RuleFor(command => command.StarRating)
            .InclusiveBetween(1, 5);

        RuleFor(command => command.Category)
            .IsInEnum();
    }
}