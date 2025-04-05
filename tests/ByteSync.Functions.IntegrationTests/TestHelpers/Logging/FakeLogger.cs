using FakeItEasy;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.IntegrationTests.TestHelpers.Logging;

public class FakeLogger<T> : ILogger<T>
{
    private readonly ILogger<T> _fakeLogger = A.Fake<ILogger<T>>();

    public IDisposable BeginScope<TState>(TState state) => _fakeLogger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => _fakeLogger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        _fakeLogger.Log(logLevel, eventId, state, exception, formatter);
    }

    public ILogger<T> GetLogger() => _fakeLogger;
}