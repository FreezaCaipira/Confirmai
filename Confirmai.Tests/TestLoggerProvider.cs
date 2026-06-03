using Microsoft.Extensions.Logging;

namespace Confirmai.Tests;

internal sealed class TestLoggerProvider : ILoggerProvider
{
    public List<string> Messages { get; } = new();

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(Messages);
    }

    public void Dispose()
    {
    }

    private sealed class TestLogger : ILogger
    {
        private readonly List<string> _messages;

        public TestLogger(List<string> messages)
        {
            _messages = messages;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _messages.Add(formatter(state, exception));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}

