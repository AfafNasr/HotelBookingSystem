using FluentValidation;

namespace HotelBooking.Application.Cities.DeleteCity;

public sealed class DeleteCityCommandValidator
    : AbstractValidator<DeleteCityCommand>
{
    public DeleteCityCommandValidator()
    {
        RuleFor(command => command.CityId)
            .GreaterThan(0);
    }
}