using FluentValidation;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Authentication.Register;

public sealed class RegisterCommandHandler
{
    private readonly IIdentityService _identityService;
    private readonly IValidator<RegisterCommand> _validator;

    public RegisterCommandHandler(
        IIdentityService identityService,
        IValidator<RegisterCommand> validator)
    {
        _identityService = identityService;
        _validator = validator;
    }

    public async Task<RegisterResult> HandleAsync(
        RegisterCommand command)
    {
        var validationResult = await _validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            return new RegisterResult(
                false,
                null,
                validationResult.ToApplicationErrors());
        }

        return await _identityService.CreateCustomerAsync(
            command.Username,
            command.Email,
            command.Password);
    }
}