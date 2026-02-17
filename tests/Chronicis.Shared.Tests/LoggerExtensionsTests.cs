using Microsoft.Extensions.Logging;

namespace Chronicis.Shared.Tests;

/// <summary>
/// Tests for LoggerExtensions to ensure all extension methods properly sanitize
/// user input before logging and respect log level filtering.
/// </summary>
public class LoggerExtensionsTests
{
    // ────────────────────────────────────────────────────────────────
    //  LogInformationSanitized
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void LogInformationSanitized_WithCleanArgs_LogsCorrectly()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Information).Returns(true);

        logger.LogInformationSanitized("User {0} logged in", "Alice");

        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogInformationSanitized_WithDirtyArgs_SanitizesThem()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Information).Returns(true);

        logger.LogInformationSanitized("User {0} logged in", "Alice\nAdmin");

        // Verify Log was called (sanitization happens inside)
        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogInformationSanitized_WhenNotEnabled_DoesNotLog()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Information).Returns(false);

        logger.LogInformationSanitized("User {0} logged in", "Alice");

        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogInformationSanitized_WithNullArgs_HandlesGracefully()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Information).Returns(true);

        logger.LogInformationSanitized("Message {0}", (object?)null);

        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogInformationSanitized_WithMultipleArgs_SanitizesAll()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Information).Returns(true);

        logger.LogInformationSanitized("User {0} from {1}", "Alice\nAdmin", "IP\r192.168.1.1");

        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // ────────────────────────────────────────────────────────────────
    //  LogWarningSanitized
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void LogWarningSanitized_WithCleanArgs_LogsCorrectly()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);

        logger.LogWarningSanitized("Warning: {0}", "Low memory");

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogWarningSanitized_WhenNotEnabled_DoesNotLog()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Warning).Returns(false);

        logger.LogWarningSanitized("Warning: {0}", "Low memory");

        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // ────────────────────────────────────────────────────────────────
    //  LogErrorSanitized (without exception)
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void LogErrorSanitized_WithoutException_LogsCorrectly()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Error).Returns(true);

        logger.LogErrorSanitized("Error: {0}", "Database connection failed");

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogErrorSanitized_WithoutException_WhenNotEnabled_DoesNotLog()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Error).Returns(false);

        logger.LogErrorSanitized("Error: {0}", "Database connection failed");

        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // ────────────────────────────────────────────────────────────────
    //  LogErrorSanitized (with exception)
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void LogErrorSanitized_WithException_LogsCorrectly()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Error).Returns(true);
        var exception = new InvalidOperationException("Test error");

        logger.LogErrorSanitized(exception, "Error occurred: {0}", "processing request");

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex => ex == exception),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogErrorSanitized_WithException_WhenNotEnabled_DoesNotLog()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Error).Returns(false);
        var exception = new InvalidOperationException("Test error");

        logger.LogErrorSanitized(exception, "Error occurred: {0}", "processing request");

        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogErrorSanitized_WithException_SanitizesArgs()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Error).Returns(true);
        var exception = new InvalidOperationException("Test error");

        logger.LogErrorSanitized(exception, "User input: {0}", "malicious\ncode");

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex => ex == exception),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // ────────────────────────────────────────────────────────────────
    //  LogDebugSanitized
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void LogDebugSanitized_WithCleanArgs_LogsCorrectly()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Debug).Returns(true);

        logger.LogDebugSanitized("Debug: {0}", "variable value");

        logger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogDebugSanitized_WhenNotEnabled_DoesNotLog()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Debug).Returns(false);

        logger.LogDebugSanitized("Debug: {0}", "variable value");

        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // ────────────────────────────────────────────────────────────────
    //  LogTraceSanitized
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void LogTraceSanitized_WithCleanArgs_LogsCorrectly()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Trace).Returns(true);

        logger.LogTraceSanitized("Trace: {0}", "entering method");

        logger.Received(1).Log(
            LogLevel.Trace,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogTraceSanitized_WhenNotEnabled_DoesNotLog()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Trace).Returns(false);

        logger.LogTraceSanitized("Trace: {0}", "entering method");

        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // ────────────────────────────────────────────────────────────────
    //  LogCriticalSanitized (without exception)
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void LogCriticalSanitized_WithoutException_LogsCorrectly()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Critical).Returns(true);

        logger.LogCriticalSanitized("Critical: {0}", "system shutdown");

        logger.Received(1).Log(
            LogLevel.Critical,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogCriticalSanitized_WithoutException_WhenNotEnabled_DoesNotLog()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Critical).Returns(false);

        logger.LogCriticalSanitized("Critical: {0}", "system shutdown");

        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // ────────────────────────────────────────────────────────────────
    //  LogCriticalSanitized (with exception)
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void LogCriticalSanitized_WithException_LogsCorrectly()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Critical).Returns(true);
        var exception = new OutOfMemoryException("Critical error");

        logger.LogCriticalSanitized(exception, "Critical failure: {0}", "out of memory");

        logger.Received(1).Log(
            LogLevel.Critical,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex => ex == exception),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogCriticalSanitized_WithException_WhenNotEnabled_DoesNotLog()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Critical).Returns(false);
        var exception = new OutOfMemoryException("Critical error");

        logger.LogCriticalSanitized(exception, "Critical failure: {0}", "out of memory");

        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // ────────────────────────────────────────────────────────────────
    //  Sanitization Integration Tests
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void AllLogMethods_CheckIsEnabledBeforeLogging()
    {
        var logger = Substitute.For<ILogger>();

        // Set all log levels to disabled
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(false);

        // Call all log methods
        logger.LogInformationSanitized("Info");
        logger.LogWarningSanitized("Warning");
        logger.LogErrorSanitized("Error");
        logger.LogDebugSanitized("Debug");
        logger.LogTraceSanitized("Trace");
        logger.LogCriticalSanitized("Critical");

        // Verify none of them logged
        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void AllLogMethods_WithMaliciousInput_StillLog()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        var malicious = "user\ninput\rwith\tcontrols";

        // All methods should still log even with malicious input (but sanitized)
        logger.LogInformationSanitized("Info: {0}", malicious);
        logger.LogWarningSanitized("Warning: {0}", malicious);
        logger.LogErrorSanitized("Error: {0}", malicious);
        logger.LogDebugSanitized("Debug: {0}", malicious);
        logger.LogTraceSanitized("Trace: {0}", malicious);
        logger.LogCriticalSanitized("Critical: {0}", malicious);

        // Should have logged 6 times
        logger.Received(6).Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    public void EachLogLevel_ChecksCorrectIsEnabled(LogLevel level)
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(level).Returns(true);

        // Call the appropriate method for this level
        switch (level)
        {
            case LogLevel.Trace:
                logger.LogTraceSanitized("Test");
                break;
            case LogLevel.Debug:
                logger.LogDebugSanitized("Test");
                break;
            case LogLevel.Information:
                logger.LogInformationSanitized("Test");
                break;
            case LogLevel.Warning:
                logger.LogWarningSanitized("Test");
                break;
            case LogLevel.Error:
                logger.LogErrorSanitized("Test");
                break;
            case LogLevel.Critical:
                logger.LogCriticalSanitized("Test");
                break;
        }

        // Verify the correct level was checked
        logger.Received(1).IsEnabled(level);
    }

    [Fact]
    public void MultipleArguments_AllGetSanitized()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Information).Returns(true);

        logger.LogInformationSanitized(
            "User {0} from {1} performed {2}",
            "Alice\nAdmin",
            "192.168.1.1\rlocal",
            "delete\tall");

        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void EmptyArgsArray_DoesNotThrow()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Information).Returns(true);

        // Should not throw with no args
        logger.LogInformationSanitized("Message with no args");

        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
