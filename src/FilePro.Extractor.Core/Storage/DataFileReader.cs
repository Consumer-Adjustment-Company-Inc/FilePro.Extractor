using FilePro.Extractor.Core.Diagnostics;

namespace FilePro.Extractor.Core.Storage;

/// <summary>
/// Streams fixed-width records from a FilePro <c>data</c> file. When
/// <paramref name="dataRecordSize"/> is 0 (all fields live in the key) or the file is
/// missing, the stream is empty and the caller supplies blank data fields.
/// </summary>
public static class DataFileReader
{
    public static IEnumerable<byte[]> Read(string dataPath, int dataRecordSize, IExtractionLog log)
    {
        if (dataRecordSize <= 0)
            yield break;

        if (!File.Exists(dataPath))
        {
            log.Warn($"dataRecordSize is {dataRecordSize} but no 'data' file exists; data fields will be blank");
            yield break;
        }

        long size = new FileInfo(dataPath).Length;
        long remainder = size % dataRecordSize;
        long count = size / dataRecordSize;
        if (remainder != 0)
            log.Warn($"data size {size} is not a multiple of record size {dataRecordSize}; reading {count} full records, ignoring {remainder} trailing bytes");

        using var fs = File.OpenRead(dataPath);
        for (long i = 0; i < count; i++)
        {
            byte[] record = new byte[dataRecordSize];
            fs.ReadExactly(record, 0, dataRecordSize);
            yield return record;
        }
    }
}
