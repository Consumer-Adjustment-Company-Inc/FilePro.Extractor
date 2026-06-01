using FilePro.Extractor.Core.Diagnostics;

namespace FilePro.Extractor.Core.Storage;

/// <summary>
/// Streams records from a FilePro <c>key</c> file. Each record is a 20-byte header followed
/// by <paramref name="keyRecordSize"/> bytes of field data. Header byte 0 is the in-use flag:
/// <c>0x00</c> means deleted, any non-zero value means active (matching the reference reader).
/// </summary>
public static class KeyFileReader
{
    public const int HeaderSize = 20;

    public static IEnumerable<KeyRecord> Read(string keyPath, int keyRecordSize, IExtractionLog log)
    {
        int stride = HeaderSize + keyRecordSize;
        if (stride <= 0)
            yield break;

        long size = new FileInfo(keyPath).Length;
        long remainder = size % stride;
        long count = size / stride;
        if (remainder != 0)
            log.Warn($"key size {size} is not a multiple of record stride {stride}; reading {count} full records, ignoring {remainder} trailing bytes (possible FTP line-ending corruption)");

        using var fs = File.OpenRead(keyPath);
        byte[] block = new byte[stride];
        for (long i = 0; i < count; i++)
        {
            fs.ReadExactly(block, 0, stride);
            bool isActive = block[0] != 0x00;
            byte[] fieldBytes = new byte[keyRecordSize];
            Array.Copy(block, HeaderSize, fieldBytes, 0, keyRecordSize);
            yield return new KeyRecord(isActive, fieldBytes);
        }
    }
}
