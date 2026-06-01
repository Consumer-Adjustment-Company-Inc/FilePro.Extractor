namespace FilePro.Extractor.Core.Storage;

/// <summary>One fully-decoded record: its active/deleted status plus column values in schema order.</summary>
public sealed class DatasetRecord
{
    public required bool IsActive { get; init; }
    public required string[] Fields { get; init; }
}
