using FilePro.Extractor.Core.Diagnostics;
using FilePro.Extractor.Core.Extraction;

string? root = null;
string? logPath = null;
bool excludeDeleted = false;
bool dryRun = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--root":
            root = NextValue(args, ref i, "--root");
            break;
        case "--log":
            logPath = NextValue(args, ref i, "--log");
            break;
        case "--exclude-deleted":
            excludeDeleted = true;
            break;
        case "--dry-run":
            dryRun = true;
            break;
        case "-h":
        case "--help":
            PrintUsage();
            return 0;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            PrintUsage();
            return 2;
    }
}

if (string.IsNullOrWhiteSpace(root))
{
    Console.Error.WriteLine("Error: --root is required.");
    PrintUsage();
    return 2;
}

if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"Error: root path does not exist: {root}");
    return 2;
}

var options = new ExtractionOptions
{
    Root = root,
    IncludeDeleted = !excludeDeleted,
    DryRun = dryRun,
};

using var logger = new ExtractionLogger(logPath);
logger.Info($"FilePro.Extractor starting — root='{root}', includeDeleted={options.IncludeDeleted}, dryRun={dryRun}");

IReadOnlyList<ExtractionReport> reports = new BatchExtractor(logger).Run(options);

PrintSummary(reports);
PrintAlienCallout(reports);

return reports.Any(r => r.Status == DatasetStatus.Error) ? 1 : 0;

static string NextValue(string[] args, ref int i, string flag)
{
    if (i + 1 >= args.Length)
    {
        Console.Error.WriteLine($"Error: {flag} requires a value.");
        Environment.Exit(2);
    }
    return args[++i];
}

static void PrintUsage()
{
    Console.WriteLine("""
        Usage: filepro-extract --root <path> [--exclude-deleted] [--dry-run] [--log <path>]

          --root <path>       Directory containing FilePro dataset subdirectories,
                              or a single dataset folder (one with a 'map' file).
          --exclude-deleted   Skip deleted records and omit the _is_deleted column.
          --dry-run           Parse and count but write no CSV files.
          --log <path>        Append the run log to this file (in addition to console).
          -h, --help          Show this help.
        """);
}

static void PrintSummary(IReadOnlyList<ExtractionReport> reports)
{
    Console.WriteLine();
    Console.WriteLine("=== Extraction Summary ===");

    int nameWidth = Math.Max(7, reports.Count == 0 ? 0 : reports.Max(r => r.Dataset.Length));
    Console.WriteLine($"{"DATASET".PadRight(nameWidth)}  {"STATUS",-9}  {"READ",8}  {"WRITTEN",8}  {"ACTIVE",8}  {"DELETED",8}  NOTE");

    foreach (ExtractionReport r in reports)
    {
        string status = r.Status switch
        {
            DatasetStatus.Extracted => "extracted",
            DatasetStatus.Skipped => "skipped",
            _ => "error",
        };
        string read = r.Status == DatasetStatus.Extracted ? r.RowsRead.ToString() : "-";
        string written = r.Status == DatasetStatus.Extracted ? r.RowsWritten.ToString() : "-";
        string active = r.Status == DatasetStatus.Extracted ? r.Active.ToString() : "-";
        string deleted = r.Status == DatasetStatus.Extracted ? r.Deleted.ToString() : "-";
        Console.WriteLine($"{r.Dataset.PadRight(nameWidth)}  {status,-9}  {read,8}  {written,8}  {active,8}  {deleted,8}  {r.Reason ?? ""}");
    }

    int extracted = reports.Count(r => r.Status == DatasetStatus.Extracted);
    int skipped = reports.Count(r => r.Status == DatasetStatus.Skipped);
    int errored = reports.Count(r => r.Status == DatasetStatus.Error);
    Console.WriteLine();
    Console.WriteLine($"Totals: {extracted} extracted, {skipped} skipped, {errored} error");
}

static void PrintAlienCallout(IReadOnlyList<ExtractionReport> reports)
{
    List<ExtractionReport> aliens = reports.Where(r => r.AlienReference is not null).ToList();
    if (aliens.Count == 0)
        return;

    Console.WriteLine();
    Console.WriteLine($"=== Alien maps ({aliens.Count}) — external data references, NOT extracted; review these ===");
    foreach (ExtractionReport a in aliens)
        Console.WriteLine($"  {a.Dataset}: {a.AlienReference}");
}
