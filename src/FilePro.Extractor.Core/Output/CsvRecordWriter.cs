using System.Text;

namespace FilePro.Extractor.Core.Output;

/// <summary>
/// Quoted-field CSV/DSV writer (RFC 4180 when the delimiter is a comma): every field is
/// quoted, embedded quotes are doubled, rows end with CRLF, and the file is UTF-8 with a BOM
/// so Excel opens accented characters cleanly. The delimiter is fixed for the whole file and
/// must not be a double quote or CR/LF, which would collide with the quoting/row-termination.
/// </summary>
public sealed class CsvRecordWriter : ICsvRecordWriter
{
    private readonly StreamWriter _writer;
    private readonly char _separator;

    public CsvRecordWriter(string path, char separator = ',')
    {
        var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        _writer = new StreamWriter(path, append: false, utf8Bom) { NewLine = "\r\n" };
        _separator = separator;
    }

    public void WriteRow(IEnumerable<string> values)
    {
        bool first = true;
        foreach (string value in values)
        {
            if (!first)
                _writer.Write(_separator);
            _writer.Write(Quote(value));
            first = false;
        }
        _writer.Write("\r\n");
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    public void Dispose()
    {
        _writer.Flush();
        _writer.Dispose();
    }
}
