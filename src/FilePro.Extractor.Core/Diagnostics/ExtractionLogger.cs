namespace FilePro.Extractor.Core.Diagnostics;

/// <summary>Writes timestamped lines to the console and, optionally, to a log file.</summary>
public sealed class ExtractionLogger : IExtractionLog, IDisposable
{
    private readonly TextWriter? _file;

    public ExtractionLogger(string? logPath = null)
    {
        if (!string.IsNullOrWhiteSpace(logPath))
            _file = new StreamWriter(logPath, append: true);
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
        Console.WriteLine(line);
        _file?.WriteLine(line);
        _file?.Flush();
    }

    public void Dispose() => _file?.Dispose();
}
