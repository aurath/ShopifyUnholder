using Microsoft.Extensions.Logging;

namespace ShopifyUnhold.Cli;

internal class FileLoggerProvider(StreamWriter logFileWriter) : ILoggerProvider
{
    private readonly StreamWriter _logFileWriter = logFileWriter ?? throw new ArgumentNullException(nameof(logFileWriter));

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _logFileWriter);

    public void Dispose() => _logFileWriter.Dispose();
}

internal class FileLogger(string categoryName, StreamWriter logFileWriter) : ILogger
{
    private readonly string _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
    private readonly StreamWriter _logFileWriter = logFileWriter ?? throw new ArgumentNullException(nameof(logFileWriter));

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    // Ensure that only information level and higher logs are recorded
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel) is false) return;

        var message = formatter(state, exception);

        _logFileWriter.WriteLine($"[{logLevel}] [{_categoryName}] {message}");
        _logFileWriter.Flush();
    }
}
