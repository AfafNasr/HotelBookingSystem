namespace HotelBooking.Application.Common.Errors;

public static class AuthenticationErrors
{
    public static readonly ApplicationError Required =
        new(
            "Authentication.Required",
            "The authenticated user could not be identified.",
            ErrorType.Authentication);

    public static readonly ApplicationError InvalidCredentials =
        new(
            "Authentication.InvalidCredentials",
            "Invalid username or password.",
            ErrorType.Authentication);
}