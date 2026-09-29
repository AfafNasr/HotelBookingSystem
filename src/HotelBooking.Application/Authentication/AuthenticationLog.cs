using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Authentication;

internal static class AuthenticationLog
{
    private static readonly Action<ILogger, string, Exception?>
        LoginSucceededMessage =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(
                    3001,
                    nameof(LoginSucceeded)),
                "Login succeeded for username {Username}.");

    private static readonly Action<ILogger, string, Exception?>
        LoginFailedMessage =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(
                    3002,
                    nameof(LoginFailed)),
                "Login failed for username {Username}.");

    private static readonly Action<ILogger, string, Exception?>
        UserRegisteredMessage =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(
                    3003,
                    nameof(UserRegistered)),
                "User {UserId} registered successfully.");

    public static void LoginSucceeded(
        ILogger logger,
        string username)
    {
        LoginSucceededMessage(
            logger,
            username,
            null);
    }

    public static void LoginFailed(
        ILogger logger,
        string username)
    {
        LoginFailedMessage(
            logger,
            username,
            null);
    }

    public static void UserRegistered(
        ILogger logger,
        string userId)
    {
        UserRegisteredMessage(
            logger,
            userId,
            null);
    }
}