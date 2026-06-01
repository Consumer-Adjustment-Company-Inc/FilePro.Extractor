namespace FilePro.Extractor.Core.Schema;

/// <summary>
/// The first line of a FilePro <c>map</c> file:
/// <c>map:&lt;keyRecordSize&gt;:&lt;dataRecordSize&gt;:&lt;fieldsInKey&gt;:&lt;pwChecksum&gt;:&lt;encodedPw&gt;</c>.
/// Password segments are intentionally ignored — reading data does not require them.
/// </summary>
public sealed record MapHeader
{
    /// <summary>Declared bytes of field data per key record, excluding the 20-byte header.</summary>
    public required int KeyRecordSize { get; init; }

    /// <summary>Declared bytes per data record (no header). May be 0 when all fields are in the key.</summary>
    public required int DataRecordSize { get; init; }

    /// <summary>Number of map field lines that belong to the key file; the rest belong to data.</summary>
    public required int FieldsInKey { get; init; }
}
