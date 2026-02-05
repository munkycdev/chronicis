using Microsoft.Extensions.Logging;

namespace Chronicis.Shared.Extensions;

/// <summary>
/// Extension methods for ILogger that automatically sanitize user input before logging.
/// These methods should be used whenever logging data that originates from user input.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs an informational message with sanitized arguments.
    /// Use this when logging user-provided data.
    /// </summary>
    public static void LogInformationSanitized(this ILogger logger, string message, params object?[] args)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        var sanitizedArgs = args.Select(arg => Utilities.LogSanitizer.SanitizeObject(arg)).ToArray();
        logger.LogInformation(message, sanitizedArgs);
    }

    /// <summary>
    /// Logs a warning message with sanitized arguments.
    /// Use this when logging user-provided data.
    /// </summary>
    public static void LogWarningSanitized(this ILogger logger, string message, params object?[] args)
    {
        if (!logger.IsEnabled(LogLevel.Warning))
            return;

        var sanitizedArgs = args.Select(arg => Utilities.LogSanitizer.SanitizeObject(arg)).ToArray();
        logger.LogWarning(message, sanitizedArgs);
    }

    /// <summary>
    /// Logs an error message with sanitized arguments.
    /// Use this when logging user-provided data.
    /// </summary>
    public static void LogErrorSanitized(this ILogger logger, string message, params object?[] args)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;

        var sanitizedArgs = args.Select(arg => Utilities.LogSanitizer.SanitizeObject(arg)).ToArray();
        logger.LogError(message, sanitizedArgs);
    }

    /// <summary>
    /// Logs an error message with exception and sanitized arguments.
    /// Use this when logging user-provided data along with an exception.
    /// </summary>
    public static void LogErrorSanitized(this ILogger logger, Exception exception, string message, params object?[] args)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;

        var sanitizedArgs = args.Select(arg => Utilities.LogSanitizer.SanitizeObject(arg)).ToArray();
        logger.LogError(exception, message, sanitizedArgs);
    }

    /// <summary>
    /// Logs a debug message with sanitized arguments.
    /// Use this when logging user-provided data.
    /// </summary>
    public static void LogDebugSanitized(this ILogger logger, string message, params object?[] args)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
            return;

        var sanitizedArgs = args.Select(arg => Utilities.LogSanitizer.SanitizeObject(arg)).ToArray();
        logger.LogDebug(message, sanitizedArgs);
    }

    /// <summary>
    /// Logs a trace message with sanitized arguments.
    /// Use this when logging user-provided data.
    /// </summary>
    public static void LogTraceSanitized(this ILogger logger, string message, params object?[] args)
    {
        if (!logger.IsEnabled(LogLevel.Trace))
            return;

        var sanitizedArgs = args.Select(arg => Utilities.LogSanitizer.SanitizeObject(arg)).ToArray();
        logger.LogTrace(message, sanitizedArgs);
    }

    /// <summary>
    /// Logs a critical message with sanitized arguments.
    /// Use this when logging user-provided data.
    /// </summary>
    public static void LogCriticalSanitized(this ILogger logger, string message, params object?[] args)
    {
        if (!logger.IsEnabled(LogLevel.Critical))
            return;

        var sanitizedArgs = args.Select(arg => Utilities.LogSanitizer.SanitizeObject(arg)).ToArray();
        logger.LogCritical(message, sanitizedArgs);
    }

    /// <summary>
    /// Logs a critical message with exception and sanitized arguments.
    /// Use this when logging user-provided data along with an exception.
    /// </summary>
    public static void LogCriticalSanitized(this ILogger logger, Exception exception, string message, params object?[] args)
    {
        if (!logger.IsEnabled(LogLevel.Critical))
            return;

        var sanitizedArgs = args.Select(arg => Utilities.LogSanitizer.SanitizeObject(arg)).ToArray();
        logger.LogCritical(exception, message, sanitizedArgs);
    }
}
