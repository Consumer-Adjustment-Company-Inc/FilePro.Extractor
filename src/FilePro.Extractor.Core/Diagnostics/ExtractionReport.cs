namespace FilePro.Extractor.Core.Diagnostics;

public enum DatasetStatus
{
    Extracted,
    Skipped,
    Error,
}

/// <summary>Per-dataset outcome for the end-of-run summary.</summary>
public sealed class ExtractionReport
{
    public required string Dataset { get; init; }
    public DatasetStatus Status { get; set; }

    /// <summary>Skip reason or error detail; null when extracted cleanly.</summary>
    public string? Reason { get; set; }

    public long RowsRead { get; set; }
    public long RowsWritten { get; set; }
    public long Active { get; set; }
    public long Deleted { get; set; }
    public string? OutputPath { get; set; }

    /// <summary>For Alien maps: the raw <c>Alien:</c> declaration line, so the external reference stays visible.</summary>
    public string? AlienReference { get; set; }
}
