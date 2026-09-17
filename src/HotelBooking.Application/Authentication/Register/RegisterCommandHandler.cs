using FluentValidation;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;

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

    public async Task<CreateCustomerResult> HandleAsync(
        RegisterCommand command)
    {
        var validationResult = await _validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
    .Select(error => new ApplicationError(
        error.ErrorCode,
        error.ErrorMessage,
        ErrorType.Validation))
    .ToArray();

            return new CreateCustomerResult(
                false,
                null,
                errors);
        }

        return await _identityService.CreateCustomerAsync(
            command.Username,
            command.Email,
            command.Password);
    }
}