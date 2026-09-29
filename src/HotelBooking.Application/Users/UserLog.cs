using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Users;

internal static class UserLog
{
    private static readonly Action<ILogger, string, Exception?>
        UserCreatedMessage =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(
                    3101,
                    nameof(UserCreated)),
                "User {UserId} was created by an administrator.");

    private static readonly Action<ILogger, string, Exception?>
        UserUpdatedMessage =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(
                    3102,
                    nameof(UserUpdated)),
                "User {UserId} was updated by an administrator.");

    private static readonly Action<ILogger, string, Exception?>
        UserPromotedMessage =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(
                    3103,
                    nameof(UserPromoted)),
                "User {UserId} was promoted to hotel owner.");

    private static readonly Action<ILogger, string, Exception?>
        UserDeactivatedMessage =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(
                    3104,
                    nameof(UserDeactivated)),
                "User {UserId} was deactivated.");

    private static readonly Action<ILogger, string, Exception?>
        UserActivatedMessage =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(
                    3105,
                    nameof(UserActivated)),
                "User {UserId} was activated.");

    private static readonly Action<ILogger, string, Exception?>
        SelfDeactivationAttemptMessage =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(
                    3106,
                    nameof(SelfDeactivationAttempt)),
                "Administrator {AdminUserId} attempted to deactivate their own account.");

    public static void UserCreated(
        ILogger logger,
        string userId)
    {
        UserCreatedMessage(
            logger,
            userId,
            null);
    }

    public static void UserUpdated(
        ILogger logger,
        string userId)
    {
        UserUpdatedMessage(
            logger,
            userId,
            null);
    }

    public static void UserPromoted(
        ILogger logger,
        string userId)
    {
        UserPromotedMessage(
            logger,
            userId,
            null);
    }

    public static void UserDeactivated(
        ILogger logger,
        string userId)
    {
        UserDeactivatedMessage(
            logger,
            userId,
            null);
    }

    public static void UserActivated(
        ILogger logger,
        string userId)
    {
        UserActivatedMessage(
            logger,
            userId,
            null);
    }

    public static void SelfDeactivationAttempt(
        ILogger logger,
        string adminUserId)
    {
        SelfDeactivationAttemptMessage(
            logger,
            adminUserId,
            null);
    }
}