using System.Text;
using FilePro.Extractor.Core.Output;

namespace FilePro.Extractor.Tests;

public class CsvRecordWriterTests
{
    private static string WriteAndRead(char? separator, params string[][] rows)
    {
        string path = Path.Combine(Path.GetTempPath(), $"csvwriter_{Guid.NewGuid():N}.csv");
        try
        {
            using (ICsvRecordWriter writer = separator is null
                       ? new CsvRecordWriter(path)
                       : new CsvRecordWriter(path, separator.Value))
            {
                foreach (string[] row in rows)
                    writer.WriteRow(row);
            }

            // UTF-8 read strips the BOM; keep the raw bytes test separate.
            return File.ReadAllText(path, Encoding.UTF8);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void WriteRow_DefaultConstructor_UsesComma()
    {
        string content = WriteAndRead(null, ["a", "b", "c"]);

        Assert.Equal("\"a\",\"b\",\"c\"\r\n", content);
    }

    [Fact]
    public void WriteRow_CustomSeparator_UsesIt()
    {
        string content = WriteAndRead(';', ["a", "b", "c"]);

        Assert.Equal("\"a\";\"b\";\"c\"\r\n", content);
    }

    [Fact]
    public void WriteRow_TabSeparator_ProducesTsv()
    {
        string content = WriteAndRead('\t', ["a", "b"]);

        Assert.Equal("\"a\"\t\"b\"\r\n", content);
    }

    [Fact]
    public void WriteRow_EmbeddedQuotes_AreDoubled()
    {
        string content = WriteAndRead(',', ["say \"hi\""]);

        Assert.Equal("\"say \"\"hi\"\"\"\r\n", content);
    }

    [Fact]
    public void WriteRow_FieldContainingSeparator_IsProtectedByQuoting()
    {
        // A value containing the delimiter stays a single field because every field is quoted.
        string content = WriteAndRead(';', ["a;b", "c"]);

        Assert.Equal("\"a;b\";\"c\"\r\n", content);
    }

    [Fact]
    public void WriteRow_SingleField_HasNoLeadingSeparator()
    {
        string content = WriteAndRead(';', ["only"]);

        Assert.Equal("\"only\"\r\n", content);
    }

    [Fact]
    public void WriteRow_RowsEndWithCrlf()
    {
        string content = WriteAndRead(',', ["r1"], ["r2"]);

        Assert.Equal("\"r1\"\r\n\"r2\"\r\n", content);
    }

    [Fact]
    public void Constructor_WritesUtf8Bom()
    {
        string path = Path.Combine(Path.GetTempPath(), $"csvwriter_{Guid.NewGuid():N}.csv");
        try
        {
            using (var writer = new CsvRecordWriter(path))
                writer.WriteRow(["x"]);

            byte[] bytes = File.ReadAllBytes(path);
            Assert.True(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
                "file should start with a UTF-8 BOM");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
