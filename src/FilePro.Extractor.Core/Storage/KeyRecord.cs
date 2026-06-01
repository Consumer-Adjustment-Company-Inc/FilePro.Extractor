namespace FilePro.Extractor.Core.Storage;

/// <summary>
/// One record from the <c>key</c> file: its active/deleted status plus the field bytes
/// that follow the 20-byte record header.
/// </summary>
public readonly record struct KeyRecord(bool IsActive, byte[] FieldBytes);
