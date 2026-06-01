using System.Text;
using FilePro.Extractor.Core.Diagnostics;
using FilePro.Extractor.Core.Schema;

namespace FilePro.Extractor.Core.Storage;

/// <summary>
/// Combines the <c>key</c> and <c>data</c> readers in lockstep and slices each record into
/// trimmed, ISO-8859-1 decoded column values. The key file is authoritative for record
/// identity and active/deleted status; the data record at the same index supplies the rest.
/// </summary>
public static class DatasetReader
{
    public static IEnumerable<DatasetRecord> Read(MapDefinition map, string folder, Encoding encoding, IExtractionLog log)
    {
        FieldDef[] keyFields = map.Fields.Where(f => f.IsInKey).ToArray();
        FieldDef[] dataFields = map.Fields.Where(f => !f.IsInKey).ToArray();

        string keyPath = Path.Combine(folder, "key");
        string dataPath = Path.Combine(folder, "data");

        using IEnumerator<byte[]> dataEnum =
            DataFileReader.Read(dataPath, map.Header.DataRecordSize, log).GetEnumerator();
        bool warnedShortData = false;

        foreach (KeyRecord key in KeyFileReader.Read(keyPath, map.Header.KeyRecordSize, log))
        {
            string[] fields = new string[keyFields.Length + dataFields.Length];

            for (int k = 0; k < keyFields.Length; k++)
                fields[k] = Slice(key.FieldBytes, keyFields[k], encoding);

            if (dataFields.Length > 0)
            {
                if (dataEnum.MoveNext())
                {
                    byte[] dataBytes = dataEnum.Current;
                    for (int d = 0; d < dataFields.Length; d++)
                        fields[keyFields.Length + d] = Slice(dataBytes, dataFields[d], encoding);
                }
                else
                {
                    if (!warnedShortData)
                    {
                        log.Warn("data file has fewer records than key file; remaining data fields left blank");
                        warnedShortData = true;
                    }
                    for (int d = 0; d < dataFields.Length; d++)
                        fields[keyFields.Length + d] = string.Empty;
                }
            }

            yield return new DatasetRecord { IsActive = key.IsActive, Fields = fields };
        }
    }

    private static string Slice(byte[] record, FieldDef field, Encoding encoding)
    {
        int start = field.OffsetInRecord;
        if (start >= record.Length)
            return string.Empty;
        int len = Math.Min(field.Width, record.Length - start);
        return encoding.GetString(record, start, len).Trim();
    }
}
