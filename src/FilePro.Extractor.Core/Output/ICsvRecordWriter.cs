namespace FilePro.Extractor.Core.Output;

public interface ICsvRecordWriter : IDisposable
{
    void WriteRow(IEnumerable<string> values);
}
