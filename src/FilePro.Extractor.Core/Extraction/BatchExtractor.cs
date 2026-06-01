using FilePro.Extractor.Core.Diagnostics;

namespace FilePro.Extractor.Core.Extraction;

/// <summary>
/// Runs <see cref="DatasetExtractor"/> across a root path. If the root itself is a dataset
/// it is processed alone; otherwise every immediate subdirectory is processed, one report each.
/// </summary>
public sealed class BatchExtractor
{
    private readonly IExtractionLog _log;

    public BatchExtractor(IExtractionLog log) => _log = log;

    public IReadOnlyList<ExtractionReport> Run(ExtractionOptions options)
    {
        var extractor = new DatasetExtractor(_log);
        var reports = new List<ExtractionReport>();

        if (DatasetExtractor.IsDataset(options.Root))
        {
            _log.Info($"root is itself a dataset; processing '{options.Root}' directly");
            reports.Add(extractor.Extract(options.Root, options));
            return reports;
        }

        foreach (string folder in Directory.EnumerateDirectories(options.Root).OrderBy(p => p, StringComparer.Ordinal))
            reports.Add(extractor.Extract(folder, options));

        if (reports.Count == 0)
            _log.Warn($"no subdirectories found under '{options.Root}'");

        return reports;
    }
}
