namespace FilePro.Extractor.Core.Extraction;

public static class ColumnNaming
{
    /// <summary>
    /// Makes CSV header names unique. The first occurrence keeps its name; each later
    /// duplicate is suffixed <c>_1</c>, <c>_2</c>, … so FilePro's repeated field names
    /// (e.g. eleven "Template MENU" columns) stay distinct.
    /// </summary>
    public static string[] Deduplicate(IReadOnlyList<string> names)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new string[names.Count];
        for (int i = 0; i < names.Count; i++)
        {
            string name = names[i];
            if (counts.TryGetValue(name, out int seen))
            {
                counts[name] = seen + 1;
                result[i] = $"{name}_{seen}";
            }
            else
            {
                counts[name] = 1;
                result[i] = name;
            }
        }
        return result;
    }
}
