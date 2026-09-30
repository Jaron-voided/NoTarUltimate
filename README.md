# NoTarUltimate

A C# command-line archiver that packs a directory tree into a single `.ntar` binary
archive and unpacks it. Built on manual stream control with a custom binary format:
128-byte header, fixed-slot file list, length-prefixed path section, and 16-byte-aligned
payloads. All multi-byte integers are little-endian.

## Format

Archives are structured in four sections:

```
Offset                          Section
────────────────────────────────────────────────────────────────────────────────
0x000                           Header        128 bytes, fixed at offset 0
0x080                           File List     FileCount × 48 bytes per entry
Align16(0x080 + FileListSize)   Path Section  length-prefixed UTF-8 filenames
PayloadOffset                   Payloads      each payload 16-byte aligned
```

**Header (128 bytes at offset 0)**

| Offset | Size | Field             | Notes                                      |
|--------|------|-------------------|--------------------------------------------|
| 0      | 8    | Magic             | `"notar170"` as uint64 LE                 |
| 8      | 2    | HeaderSize        | Always `0x0080` (128)                      |
| 10     | 1    | VersionMajor      |                                            |
| 11     | 1    | VersionMinor      |                                            |
| 12     | 4    | FileLayoutVersion |                                            |
| 16     | 8    | FeatureFlags      |                                            |
| 24     | 4    | FileCount         | Number of file-list entries                |
| 28     | 4    | FileListSize      | `FileCount × 48`                           |
| 32     | 4    | PayloadOffset     | Absolute offset of first payload byte      |
| 36     | 8    | PayloadSize       | `archive_size − PayloadOffset`             |
| 44     | 32   | PayloadHash       | Intended SHA-256; see audit for defect B5  |
| 76     | 52   | Padding           |                                            |

**File list entry (48 bytes per file)**

| Offset | Size | Field             | Notes                                      |
|--------|------|-------------------|--------------------------------------------|
| 0      | 8    | FileSize          | Payload size in bytes                      |
| 8      | 8    | CreationTime      | `DateTime.ToBinary()` — local time         |
| 16     | 8    | LastModifiedTime  | `DateTime.ToBinary()` — local time         |
| 24     | 4    | FileAttributes    | Windows `FileAttributes` flags             |
| 28     | 8    | ByteOffset        | Absolute offset of this file's payload     |
| 36     | 12   | Padding           |                                            |

**Path section** — immediately after the file list (16-byte-aligned start). One entry
per file in file-list order: `[uint16 length][UTF-8 bytes]`, no null terminator. Total
path-section size is not stored in the header; a reader must walk `FileCount` entries.

**Payload section** — starts at `PayloadOffset`. Each file's payload begins at a
16-byte-aligned offset recorded in its file-list entry's `ByteOffset` field.

## Usage

Requires .NET 8 SDK.

```
dotnet run --project NoTarUltimate -- pack   <source-directory> <output.ntar>
dotnet run --project NoTarUltimate -- unpack <input.ntar> <output-directory>
```

```
dotnet build
dotnet test
```

## Status

**Audited September 2026: 14 known defects across stream handling, path handling, and
hash verification. The pack→unpack round-trip does not currently produce correct
output.**

Fix work happens on `stabilize/notar-v1`. Tag `notar-original-baseline` marks the
pre-stabilization state of the code.

See [`docs/AUDIT_REPORT.md`](docs/AUDIT_REPORT.md) for the complete findings,
prioritized fix batches (Batches 0–5), and the regression-test plan.
