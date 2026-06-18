namespace FilePro.Extractor.Core.Extraction;

/// <summary>
/// Parses the CLI <c>--separator</c> value into a single CSV field delimiter.
/// Accepts a literal single character, the escape <c>\t</c>, or the name <c>tab</c>
/// (so TSV is reachable without shell-specific quoting). Rejects characters that would
/// corrupt the output — the double-quote field wrapper and the CR/LF row terminators —
/// since <see cref="Output.CsvRecordWriter"/> quotes every field and ends rows with CRLF.
/// </summary>
public static class SeparatorOption
{
    /// <summary>The default delimiter when <c>--separator</c> is not supplied.</summary>
    public const char Default = ',';

    /// <summary>
    /// Attempts to parse a raw CLI argument into a delimiter character.
    /// Returns false with a user-facing <paramref name="error"/> when the value is
    /// missing, not a single character, or a delimiter that would corrupt the CSV.
    /// </summary>
    public static bool TryParse(string? raw, out char separator, out string? error)
    {
        separator = Default;
        error = null;

        if (string.IsNullOrEmpty(raw))
        {
            error = "--separator requires a value: a single character, '\\t', or 'tab'.";
            return false;
        }

        char candidate;
        if (raw == "\\t" || string.Equals(raw, "tab", StringComparison.OrdinalIgnoreCase))
            candidate = '\t';
        else if (raw.Length == 1)
            candidate = raw[0];
        else
        {
            error = $"--separator must be a single character, '\\t', or 'tab' (got: \"{raw}\").";
            return false;
        }

        if (candidate is '"' or '\r' or '\n' or '\0')
        {
            error = "--separator cannot be a double quote, CR, LF, or NUL — those corrupt the CSV.";
            return false;
        }

        separator = candidate;
        return true;
    }
}
