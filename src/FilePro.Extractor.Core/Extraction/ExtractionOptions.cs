namespace FilePro.Extractor.Core.Extraction;

public sealed class ExtractionOptions
{
    public required string Root { get; init; }

    /// <summary>When true, deleted records are written with an <c>_is_deleted</c> column. Default on.</summary>
    public bool IncludeDeleted { get; init; } = true;

    /// <summary>When true, parse and count but write no CSV files.</summary>
    public bool DryRun { get; init; }

    /// <summary>Field delimiter for the CSV output. Defaults to a comma.</summary>
    public char Separator { get; init; } = SeparatorOption.Default;
}
