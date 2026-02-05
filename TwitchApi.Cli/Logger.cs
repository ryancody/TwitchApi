using Microsoft.Extensions.Logging;
using TwitchApi;

public class Logger : ILogger<TwitchClient>
{
    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var msg = $"[{logLevel}] TwitchApiTest: {formatter(state, exception)}";
            if (exception != null) msg += $"\n{exception}";

            switch (logLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    Console.WriteLine(msg);
                    break;
                case LogLevel.Warning:
                    Console.WriteLine(msg);
                    break;
                default:
                    Console.WriteLine(msg);
                    break;
            }
        }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}