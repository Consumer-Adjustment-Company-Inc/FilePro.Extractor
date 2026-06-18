# FilePro.Extractor

A .NET 10 command-line tool that bulk-exports legacy **filePro** databases to CSV by
reading their raw on-disk files (`map`, `key`, `data`) directly — **no filePro
installation required**.

It walks a root directory of filePro dataset folders, parses each one, and writes a
timestamped CSV per dataset. Both active and deleted records are included by default,
distinguished by an `_is_deleted` column.

## Background — the filePro on-disk format

filePro stores each dataset as three files. This description is based on the on-disk
format observations documented by **Jim Storch** in the README of
[`jimstorch/python-read-filepro`](https://github.com/jimstorch/python-read-filepro):

- **`map`** — the schema (plain text). The first line is a colon-delimited header
  declaring the key-record size, the data-record size, and how many fields live in the
  key file; each remaining line defines one field (name, width, edit type). A map whose
  first line begins with `Alien:` points to external (non-filePro) data and is skipped.
- **`key`** — a fixed-length binary file. Each record is a 20-byte header followed by the
  **indexed** field values. The first header byte flags the record: `0x00` = deleted,
  any non-zero value = active. The total record count is derived from this file.
- **`data`** — a fixed-length binary file holding the **non-indexed** fields, one record
  per key record, in the same order, with no per-record header. It is absent or empty
  when every field is indexed in the key file.

## What it does

- Reads the filePro 5.x `map`/`key`/`data` format directly.
- Processes a single dataset folder, or iterates every immediate subdirectory of a root.
- Writes one timestamped CSV per dataset (`<dataset>_<yyyyMMddTHHmmss>.csv`) into that
  dataset's own folder; never overwrites a previous export.
- Includes active **and** deleted records by default, flagged by a leading `_is_deleted`
  column. `--exclude-deleted` drops deleted rows and omits the column.
- Decodes input as ISO-8859-1 and writes UTF-8 (with BOM), quoted-field CSV (RFC 4180
  when the delimiter is the default comma; `--separator` can override it).
- Skips non-datasets (no `map`, `Alien:` maps, incomplete folders missing `key`) with a
  logged reason, surfaces any `Alien:` references for review, and prints a per-run summary.

## How to use

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
# build
dotnet build -c Release

# extract every dataset under a root directory
dotnet run --project src/FilePro.Extractor.Cli -- --root /path/to/filepro

# preview what would be extracted, writing nothing
dotnet run --project src/FilePro.Extractor.Cli -- --root /path/to/filepro --dry-run

# active records only (no _is_deleted column)
dotnet run --project src/FilePro.Extractor.Cli -- --root /path/to/filepro --exclude-deleted

# use a different delimiter (single char, or '\t'/'tab' for TSV)
dotnet run --project src/FilePro.Extractor.Cli -- --root /path/to/filepro --separator ';'

# also append the run summary to a log file
dotnet run --project src/FilePro.Extractor.Cli -- --root /path/to/filepro --log run.log
```

| Flag | Description |
|---|---|
| `--root <path>` | Directory of filePro dataset folders, or a single dataset folder. **Required.** |
| `--exclude-deleted` | Skip deleted records and omit the `_is_deleted` column. |
| `--dry-run` | Parse and count, but write no CSV files. |
| `--log <path>` | Append the run log to a file in addition to the console. |
| `--separator <char>` | Override the CSV field delimiter. A single character, or `\t`/`tab` for TSV. Default is a comma. Cannot be `"`, CR, or LF. |

> **Always run against a _copy_ of your filePro data, never the live files.**