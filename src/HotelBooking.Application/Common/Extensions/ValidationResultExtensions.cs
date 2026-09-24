using FluentValidation.Results;
using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Common.Extensions;

public static class ValidationResultExtensions
{
    public static ApplicationError[] ToApplicationErrors(
        this ValidationResult validationResult)
    {
        return validationResult.Errors
            .Select(error => new ApplicationError(
                error.PropertyName,
                error.ErrorMessage,
                ErrorType.Validation))
            .ToArray();
    }
}