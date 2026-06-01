using System.Text;

namespace FilePro.Extractor.Core.Output;

/// <summary>
/// RFC 4180 writer: every field is quoted, embedded quotes are doubled, rows end with CRLF,
/// and the file is UTF-8 with a BOM so Excel opens accented characters cleanly.
/// </summary>
public sealed class CsvRecordWriter : ICsvRecordWriter
{
    private readonly StreamWriter _writer;

    public CsvRecordWriter(string path)
    {
        var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        _writer = new StreamWriter(path, append: false, utf8Bom) { NewLine = "\r\n" };
    }

    public void WriteRow(IEnumerable<string> values)
    {
        bool first = true;
        foreach (string value in values)
        {
            if (!first)
                _writer.Write(',');
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
