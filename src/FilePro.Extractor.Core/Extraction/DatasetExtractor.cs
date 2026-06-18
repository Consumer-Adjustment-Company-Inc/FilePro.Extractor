using System.Text;
using FilePro.Extractor.Core.Diagnostics;
using FilePro.Extractor.Core.Output;
using FilePro.Extractor.Core.Schema;
using FilePro.Extractor.Core.Storage;

namespace FilePro.Extractor.Core.Extraction;

/// <summary>Extracts a single FilePro dataset folder (containing <c>map</c>/<c>key</c>/<c>data</c>) to one CSV.</summary>
public sealed class DatasetExtractor
{
    private static readonly Encoding Latin1 = Encoding.Latin1;

    private readonly IExtractionLog _log;

    public DatasetExtractor(IExtractionLog log) => _log = log;

    /// <summary>True if the folder looks like a FilePro dataset (has a <c>map</c> file).</summary>
    public static bool IsDataset(string folder) => File.Exists(Path.Combine(folder, "map"));

    public ExtractionReport Extract(string folder, ExtractionOptions options)
    {
        string name = new DirectoryInfo(folder).Name;
        var report = new ExtractionReport { Dataset = name };
        var log = new PrefixLog(_log, name);

        string mapPath = Path.Combine(folder, "map");
        if (!File.Exists(mapPath))
        {
            report.Status = DatasetStatus.Skipped;
            report.Reason = "no 'map' file";
            return report;
        }

        MapParseResult parsed = MapFileReader.Parse(mapPath, Latin1);

        // Alien maps point to external (non-FilePro) data — we can't parse them, but they
        // may be data the user cares about, so surface them loudly instead of quietly skipping.
        if (parsed.Status == MapParseStatus.Alien)
        {
            report.Status = DatasetStatus.Skipped;
            report.Reason = "Alien map (external data reference)";
            report.AlienReference = parsed.Reason;
            log.Warn($"Alien map — external data reference, NOT extracted. Declaration: {parsed.Reason}");
            log.Warn($"  folder contents (the external data may be among these): {DescribeFolderFiles(folder)}");
            return report;
        }

        if (parsed.Status != MapParseStatus.Ok || parsed.Map is null)
        {
            report.Status = DatasetStatus.Skipped;
            report.Reason = parsed.Reason ?? "unrecognized map file";
            return report;
        }

        // The key file is mandatory: it carries the per-record headers (active/deleted flags)
        // and the indexed fields, so the record count itself comes from it. A missing key means
        // a broken or partial copy — skip cleanly before opening any output file.
        if (!File.Exists(Path.Combine(folder, "key")))
        {
            report.Status = DatasetStatus.Skipped;
            report.Reason = "no 'key' file (incomplete dataset)";
            return report;
        }

        foreach (string warning in parsed.Warnings)
            log.Warn(warning);

        if (Directory.Exists(Path.Combine(folder, "qual")))
            log.Warn("qualifier subdirectory 'qual' present; not processed in v1");

        MapDefinition map = parsed.Map;
        string[] columns = ColumnNaming.Deduplicate(map.Fields.Select(f => f.Name).ToArray());
        string[] header = options.IncludeDeleted ? ["_is_deleted", .. columns] : columns;

        ICsvRecordWriter? writer = null;
        try
        {
            if (!options.DryRun)
            {
                report.OutputPath = MakeOutputPath(folder, name);
                writer = new CsvRecordWriter(report.OutputPath, options.Separator);
                writer.WriteRow(header);
            }

            foreach (DatasetRecord record in DatasetReader.Read(map, folder, Latin1, log))
            {
                report.RowsRead++;
                if (record.IsActive)
                    report.Active++;
                else
                    report.Deleted++;

                if (!options.IncludeDeleted && !record.IsActive)
                    continue;

                if (writer is not null)
                {
                    IEnumerable<string> row = options.IncludeDeleted
                        ? Prepend(record.IsActive ? "false" : "true", record.Fields)
                        : record.Fields;
                    writer.WriteRow(row);
                }
                report.RowsWritten++;
            }

            report.Status = DatasetStatus.Extracted;
            log.Info(options.DryRun
                ? $"dry-run: {report.RowsWritten} rows would be written ({report.Active} active, {report.Deleted} deleted)"
                : $"wrote {report.RowsWritten} rows to {Path.GetFileName(report.OutputPath)} ({report.Active} active, {report.Deleted} deleted)");
        }
        catch (Exception ex)
        {
            report.Status = DatasetStatus.Error;
            report.Reason = ex.Message;
            log.Error(ex.Message);
        }
        finally
        {
            writer?.Dispose();
        }

        return report;
    }

    private static IEnumerable<string> Prepend(string first, string[] rest)
    {
        yield return first;
        foreach (string value in rest)
            yield return value;
    }

    private static string DescribeFolderFiles(string folder)
    {
        try
        {
            string[] files = new DirectoryInfo(folder).EnumerateFiles()
                .Where(f => f.Name != "map")
                .OrderBy(f => f.Name, StringComparer.Ordinal)
                .Select(f => $"{f.Name} ({f.Length} bytes)")
                .ToArray();
            return files.Length == 0 ? "(no other files)" : string.Join(", ", files);
        }
        catch (IOException)
        {
            return "(unreadable)";
        }
    }

    private static string MakeOutputPath(string folder, string datasetName)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMddTHHmmss");
        string path = Path.Combine(folder, $"{datasetName}_{timestamp}.csv");
        int n = 1;
        while (File.Exists(path))
            path = Path.Combine(folder, $"{datasetName}_{timestamp}_{n++}.csv");
        return path;
    }
}
