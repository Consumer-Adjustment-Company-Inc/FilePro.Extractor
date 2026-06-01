namespace FilePro.Extractor.Core.Diagnostics;

public interface IExtractionLog
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
}

/// <summary>Wraps a log, prefixing every message with a dataset name for batch context.</summary>
public sealed class PrefixLog(IExtractionLog inner, string prefix) : IExtractionLog
{
    public void Info(string message) => inner.Info($"[{prefix}] {message}");
    public void Warn(string message) => inner.Warn($"[{prefix}] {message}");
    public void Error(string message) => inner.Error($"[{prefix}] {message}");
}
