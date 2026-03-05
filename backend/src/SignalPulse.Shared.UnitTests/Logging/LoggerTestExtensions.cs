using FakeItEasy;
using Microsoft.Extensions.Logging;

namespace SignalPulse.Shared.UnitTests.Logging;

public static class LoggerTestExtensions
{
    /// <summary>
    /// Asserts that the fake logger logged at the given level, optionally with the expected exception.
    /// </summary>
    public static void MustHaveLogged<T>(this ILogger<T> logger, LogLevel level, Exception? exception = null, int times = 1)
    {
        A.CallTo(logger).Where(call => call.Method.Name == "Log" &&
        call.GetArgument<LogLevel>(0) == level &&
        (exception == null || call.GetArgument<Exception>(3) == exception))
            .MustHaveHappened(times, Times.Exactly);
    }
}