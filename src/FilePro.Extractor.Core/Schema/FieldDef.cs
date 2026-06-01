namespace FilePro.Extractor.Core.Schema;

/// <summary>
/// One non-dummy column in a FilePro dataset. Width-0 placeholder fields are
/// not represented here — they are dropped during map parsing.
/// </summary>
public sealed record FieldDef
{
    public required string Name { get; init; }

    public required int Width { get; init; }

    public required string EditType { get; init; }

    /// <summary>True if the field lives in the <c>key</c> file, false if in <c>data</c>.</summary>
    public required bool IsInKey { get; init; }

    /// <summary>
    /// Byte offset of this field within its own file's record. For key fields this is
    /// relative to the field bytes that follow the 20-byte record header; for data
    /// fields it is relative to the start of the data record.
    /// </summary>
    public required int OffsetInRecord { get; init; }
}
