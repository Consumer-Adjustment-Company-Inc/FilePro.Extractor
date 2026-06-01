using System.Text;

namespace FilePro.Extractor.Core.Schema;

public enum MapParseStatus
{
    Ok,
    Alien,
    NotAMap,
}

/// <summary>Parsed schema for one dataset: header plus the ordered, non-dummy fields.</summary>
public sealed class MapDefinition
{
    public required MapHeader Header { get; init; }

    /// <summary>Non-dummy fields, key fields first (in order) then data fields (in order).</summary>
    public required IReadOnlyList<FieldDef> Fields { get; init; }
}

public sealed class MapParseResult
{
    public required MapParseStatus Status { get; init; }
    public MapDefinition? Map { get; init; }
    public string? Reason { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>
/// Parses a FilePro <c>map</c> file. The partition of fields into key vs data is purely
/// positional: the first <c>fieldsInKey</c> lines after the header belong to the key file —
/// including width-0 dummy lines, which still consume a slot even though they are dropped
/// from the output schema.
/// </summary>
public static class MapFileReader
{
    public static MapParseResult Parse(string mapPath, Encoding encoding)
    {
        string[] lines = File.ReadAllLines(mapPath, encoding);
        if (lines.Length == 0)
            return new MapParseResult { Status = MapParseStatus.NotAMap, Reason = "map file is empty" };

        string headerLine = lines[0];
        if (headerLine.StartsWith("Alien:", StringComparison.Ordinal))
            return new MapParseResult { Status = MapParseStatus.Alien, Reason = headerLine.Trim() };
        if (!headerLine.StartsWith("map:", StringComparison.Ordinal))
            return new MapParseResult { Status = MapParseStatus.NotAMap, Reason = "first line is not a 'map:' header" };

        string[] head = headerLine.Split(':');
        if (head.Length < 4
            || !int.TryParse(head[1], out int keyRecordSize)
            || !int.TryParse(head[2], out int dataRecordSize)
            || !int.TryParse(head[3], out int fieldsInKey))
        {
            return new MapParseResult { Status = MapParseStatus.NotAMap, Reason = "malformed 'map:' header line" };
        }

        var warnings = new List<string>();
        var fields = new List<FieldDef>();

        // Key fields: exactly fieldsInKey lines, dummies counted.
        int keyOffset = 0;
        int summedKeyWidth = 0;
        for (int i = 0; i < fieldsInKey; i++)
        {
            int lineIdx = 1 + i;
            string line = lineIdx < lines.Length ? lines[lineIdx] : string.Empty;
            (string name, int width, string edit) = ParseFieldLine(line);
            if (width <= 0)
                continue; // dummy: drops out of the schema but still consumed a slot above
            fields.Add(new FieldDef { Name = name, Width = width, EditType = edit, IsInKey = true, OffsetInRecord = keyOffset });
            keyOffset += width;
            summedKeyWidth += width;
        }

        // Data fields: everything after the key lines, to EOF.
        int dataOffset = 0;
        int summedDataWidth = 0;
        for (int lineIdx = 1 + fieldsInKey; lineIdx < lines.Length; lineIdx++)
        {
            string line = lines[lineIdx];
            if (line.Length == 0)
                continue;
            (string name, int width, string edit) = ParseFieldLine(line);
            if (width <= 0)
                continue;
            fields.Add(new FieldDef { Name = name, Width = width, EditType = edit, IsInKey = false, OffsetInRecord = dataOffset });
            dataOffset += width;
            summedDataWidth += width;
        }

        if (summedKeyWidth != keyRecordSize)
            warnings.Add($"sum of key field widths ({summedKeyWidth}) != header keyRecordSize ({keyRecordSize}); slicing by field widths, record stride by header");
        if (dataRecordSize != 0 && summedDataWidth != dataRecordSize)
            warnings.Add($"sum of data field widths ({summedDataWidth}) != header dataRecordSize ({dataRecordSize})");

        var map = new MapDefinition
        {
            Header = new MapHeader { KeyRecordSize = keyRecordSize, DataRecordSize = dataRecordSize, FieldsInKey = fieldsInKey },
            Fields = fields,
        };
        return new MapParseResult { Status = MapParseStatus.Ok, Map = map, Warnings = warnings };
    }

    private static (string Name, int Width, string EditType) ParseFieldLine(string line)
    {
        string[] parts = line.Split(':');
        string name = Sanitize(parts.Length > 0 ? parts[0] : string.Empty);
        int width = parts.Length > 1 && int.TryParse(parts[1].Trim(), out int w) ? w : 0;
        string edit = parts.Length > 2 ? parts[2].Trim() : string.Empty;
        return (name, width, edit);
    }

    /// <summary>Keep printable ASCII only — FilePro field names sometimes carry stray control bytes.</summary>
    private static string Sanitize(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            if (c is >= (char)0x20 and <= (char)0x7E)
                sb.Append(c);
        }
        return sb.ToString().Trim();
    }
}
