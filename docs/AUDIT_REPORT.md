# NoTar Audit Report

**Repository**: NoTarUltimate  
**Audited by**: Claude Code static analysis  
**Date**: 2026-09-18  
**Status**: Complete. Stabilization not yet authorized.

---

## 1. Build and Test Status

**Build**: Succeeded with 3 warnings.

```
dotnet build --configuration Debug
Build succeeded.
  CS7022: NUnit Test SDK entry point conflicts with top-level statements
  CS8604: Possible null reference for 'relativeTo' in SerializePath (NotarPackage.cs:69)
  CS8618: Non-nullable 'FilePath' not initialized in constructor (NotarFile.cs:10)
  3 Warning(s), 0 Error(s)
```

**Tests**: Zero tests run.

```
dotnet test
No test is available in NoTarUltimate.dll
```

The only test file (`NotarHeaderTests.cs`) is entirely commented out. It also references
a `DirectoryCount` field that no longer exists in `NotarHeader`. NUnit and the Test SDK
are referenced in the production project rather than a separate test project.

**Smoke test — Pack**: Produced a 426-byte archive from 3 test files. Exit 0.

**Smoke test — Unpack**: Broken. Three 0-byte files were written to the current working
directory instead of the specified output directory. Details in §5 (B1–B4).

---

## 2. Repository Structure

```
NoTarUltimate/
├── NoTarUltimate.sln
├── NotarHeaderTests.cs          (entirely commented out; lives in solution root,
│                                 not a test project)
└── NoTarUltimate/
    ├── Program.cs               (3 lines; delegates to CommandOrchestrator)
    ├── CommandOrchestrator.cs   (CLI dispatch: pack / unpack)
    ├── NotarPackage.cs          (top-level type; Pack + UnPack)
    ├── NotarHeader.cs           (archive header; 128 bytes)
    ├── NotarFile.cs             (per-file metadata + path; 48 bytes per entry)
    ├── NotarFileList.cs         (list of NotarFile; computes FileListSize)
    ├── PayloadHash.cs           (4×ulong struct for 32-byte hash field)
    ├── Utils.cs                 (Align16, RunPack, RunUnpack)
    ├── TestFiles/               (3 test files: another.txt, hello.txt,
    │                             TestDir/goodbye.txt)
    ├── TestFilesPacked.notar    (pre-committed archive of TestFiles)
    ├── ImHexNotar/              (empty — pattern file never created)
    ├── Notes.txt                (two TODO items)
    ├── Questions.txt            (6 in-progress design questions)
    └── global.json              (pins SDK 8.0.408)
```

---

## 3. NoTar On-Disk Format

Every claim cites source file and line. **Verified** means confirmed from a live hex
dump or Python parse of a freshly packed archive.

### 3.1 Archive Layout

```
Offset                          Section
────────────────────────────────────────────────────────────────────────────────
0x000                           Archive Header     128 bytes, fixed at offset 0
0x080                           File List          FileCount × 48 bytes
Align16(0x080 + FileListSize)   Path Section       variable length
PayloadOffset                   Payload Section    16-byte aligned start
(no marker)                     End of archive
```

**Source**: `NotarPackage.cs:46–115` (Pack), `NotarPackage.cs:117–165` (UnPack).  
**Verified**: Python parse of a freshly packed 3-file, 426-byte archive.

### 3.2 Archive Header — 128 bytes at offset 0

| Offset | Size | Type        | Field             | Packed value                              |
|--------|------|-------------|-------------------|-------------------------------------------|
| 0      | 8    | uint64 LE   | Magic             | `0x3037317261746F6E` = ASCII `"notar170"` |
| 8      | 2    | uint16 LE   | HeaderSize        | `0x0080` = 128                            |
| 10     | 1    | uint8       | VersionMajor      | 1                                         |
| 11     | 1    | uint8       | VersionMinor      | 1                                         |
| 12     | 4    | uint32 LE   | FileLayoutVersion | 1                                         |
| 16     | 8    | uint64 LE   | FeatureFlags      | 1 (no flags currently defined)            |
| 24     | 4    | uint32 LE   | FileCount         | n                                         |
| 28     | 4    | uint32 LE   | FileListSize      | n × 48                                    |
| 32     | 4    | uint32 LE   | PayloadOffset     | Align16(end of path section)              |
| 36     | 8    | uint64 LE   | PayloadSize       | `stream.Length − PayloadOffset`           |
| 44     | 32   | 4×uint64 LE | PayloadHash       | SHA-256, truncated — see Bug B5           |
| 76     | 52   | —           | Padding           | zero on write; seek-skipped on read       |

**Source**: `NotarHeader.cs:7–48`, `NotarHeader.cs:51–68`.  
**Verified**: all field values confirmed against hex dump.

Magic bytes on disk: `6e 6f 74 61 72 31 37 30` = ASCII `"notar170"`. `IsValidHeader()`
at `NotarHeader.cs:71–74` checks both magic and HeaderSize but is `private` with no
callers — dead code (Bug B7).

### 3.3 File List — 48 bytes per entry, starts at offset 128

| Offset in entry | Size | Type      | Field                                        |
|-----------------|------|-----------|----------------------------------------------|
| 0               | 8    | uint64 LE | FileSize                                     |
| 8               | 8    | int64 LE  | CreationTime (`DateTime.ToBinary()`)         |
| 16              | 8    | int64 LE  | LastModifiedTime (`DateTime.ToBinary()`)     |
| 24              | 4    | uint32 LE | FileAttributes (Windows `FileAttributes`)    |
| 28              | 8    | uint64 LE | ByteOffset (absolute payload offset)         |
| 36              | 12   | —         | Padding (seek-skipped; not zeroed — Bug B14) |

**Source**: `NotarFile.cs:11–16`, `NotarFile.cs:22–35`, `NotarFile.cs:49–58`.  
**Verified**: FileSize and ByteOffset confirmed against Python parse.

Entry size is exactly 48 = `NotarFile.NotarFileInfoSize` (`NotarFile.cs:18`). 48 is a
multiple of 16, so the file list always ends on a 16-byte boundary.

### 3.4 Path Section

Starts at `Align16(128 + FileListSize)`. Entries are in the same order as the File
List with no alignment padding between them.

Each entry:

```
[uint16 LE]  path length in bytes
[N bytes]    UTF-8 encoded path, no null terminator
```

**Source**: `NotarFile.cs:38–47` (SerializePath), `NotarFile.cs:60–65` (DeserializePath).  
**Verified**: Python parse confirmed lengths and values.

**Important**: paths contain only the bare filename, not the path relative to the
archive root. This is Bug B1; directory structure is unconditionally lost.  
**Source**: `NotarPackage.cs:67–70`.

The total size of the path section is not stored in the header; a reader must walk
exactly `FileCount` entries to find the end.

### 3.5 Payload Section

Starts at `PayloadOffset`. Before writing each file's content, the stream is advanced
to the next 16-byte boundary; `ByteOffset` records the resulting aligned start offset.

The gap between the path section end and `PayloadOffset` is 0–15 bytes. It is zero
when the path section ends exactly on a 16-byte boundary, which occurs when the total
byte count of all path entries is a multiple of 16. The gap in the 3-file test archive
is 11 bytes; a constructed example with path entries totalling 16 bytes gives a gap
of zero.

Gaps between consecutive files in the payload are also 0–15 bytes and are not
explicitly zeroed. For a new output file they read as zero; for a reused output path
they may contain stale bytes from a prior pack (Bug B12).

**Source**: `NotarPackage.cs:74–87`.

### 3.6 Alignment Function

```csharp
(value + 0xF) & ~0xF    // Utils.cs:9
```

Rounds `value` up to the next multiple of 16. Uses `int` arithmetic; overflows
silently for values ≥ `Int32.MaxValue − 14` (Bug B13).

### 3.7 Byte Order

Little-endian throughout. .NET `BinaryWriter`/`BinaryReader` always use little-endian.
**Verified** from all field parses.

### 3.8 PayloadHash Field

Nominally a 32-byte SHA-256 digest across four `ulong` fields (`PayloadHash.cs:6–9`).
In practice only 20 bytes are stored (Bug B5):

- `PayloadHashPartA` — SHA-256 bytes 0–7 ✓
- `PayloadHashPartB` — SHA-256 bytes 8–15 ✓
- `PayloadHashPartC` — SHA-256 bytes 16–19 only (`BitConverter.ToUInt32`, not
  `ToUInt64`); upper 4 bytes zero
- `PayloadHashPartD` — never assigned; all 8 bytes zero

SHA-256 bytes 20–31 are silently discarded; the stored field contains 20 meaningful
bytes followed by 12 zero bytes.  
**Verified**: Python parse confirmed `stored_hash[20:]` = 12 zero bytes.

Hash is computed over exactly `PayloadSize` bytes starting at `PayloadOffset`
(`NotarPackage.cs:168–183`). It is never verified during UnPack (Bug B6).

### 3.9 End-of-Archive

No end-of-archive marker. The archive ends after the last payload byte. Truncation is
not signaled explicitly but is partially detectable through declared bounds — see §4.5.

---

## 4. Archive Identification and Recovery Capability

### 4.1 Archive Identification at Offset 0

**Supported. False-positive rate for identification: ~1/2^80.**

The archive header at offset 0 carries an 8-byte ASCII magic `"notar170"` (uint64 LE)
and a 2-byte HeaderSize that must equal `0x0080`. The probability of a random 10-byte
sequence satisfying both checks is 1/2^64 × 1/2^16 = 1/2^80 ≈ 8 × 10^-25.

This figure applies to recognizing a NoTar archive at a known candidate offset — for
example, testing whether a given file is a NoTar archive, or locating one embedded in
a larger binary blob by scanning at each candidate position. When scanning a larger
blob, the expected false-positive count per gigabyte (~67 M candidate positions) is
~5.6 × 10^-17, which is negligible.

This figure does not apply to file-list entry validation or scan-based
resynchronization. See §4.3 and §4.4.

**Source**: `NotarHeader.cs:7–8`, `NotarHeader.cs:71–74`.

### 4.2 Recovery with Intact Header and File List

**Supported.**

When the archive header is readable and FileCount is trustworthy, each file-list entry
occupies a known slot at `128 + i × 48`. A reader can seek directly to any slot.
`ByteOffset` and `FileSize` in each entry give the exact payload location for that file
independently of all other entries. Every file whose entry is intact can be extracted
without sequential stream state.

**Source**: `NotarFile.cs:11–16`, `NotarPackage.cs:153–164`.

### 4.3 Recovery with Partially Corrupted File List — Header and FileCount Intact

**Supported for intact entries; corrupted entries are individually unrecoverable.**

File-list entries occupy fixed 48-byte slots. In-place corruption of one entry's field
values — without inserting or deleting bytes — does not shift the boundary of any other
entry. When FileCount is trustworthy, a reader evaluates each slot independently:

- **Slot passes validity checks** (ByteOffset is 16-byte aligned, within
  `[PayloadOffset, archive_size)`, `ByteOffset + FileSize ≤ archive_size`, FileAttributes
  uses only defined bits, timestamps in a plausible range) → extract payload from
  `ByteOffset`.
- **Slot fails validity checks** → that file is unrecoverable; advance to the next slot
  at `128 + (i+1) × 48`.

This is slot-based evaluation using known fixed boundaries, not scan-based
resynchronization. Path-section correspondence may also be damaged; path recovery from
an orphaned but otherwise valid file-list entry is not defined in v1.

### 4.4 Scan-Based Resynchronization

**Not yet shown to be reliable in NoTar v1.**

Scan-based resynchronization is the relevant recovery mode when slot boundaries cannot
be computed from the header:

1. **FileCount is corrupted** — the number of entries is unknown; the fixed-offset
   formula cannot locate slots.
2. **Bytes have been inserted or deleted** — any insertion or deletion within or before
   the file-list section displaces all subsequent slot offsets; the formula
   `128 + i × 48` no longer locates valid entries.
3. **The archive header is unreadable** — both the start of the file list and FileCount
   are unknown.

In these scenarios a reader must scan candidate positions for valid-looking file-list
entries. The critical unknown is how reliably a valid 48-byte entry can be distinguished
from an arbitrary 48-byte sequence at a candidate position. That is what an empirical
false-positive experiment must measure.

The experiment should scan a region containing file content rather than file-list
entries — for example, the payload section of a healthy archive — and count candidate
positions that pass all available slot validity checks. Two measurements are needed:

- **Every byte offset**: gives the general false-positive rate and is the relevant
  baseline when insertion or deletion has destroyed the original 16-byte alignment of
  entry starts.
- **16-byte-aligned offsets only**: gives the rate relevant to in-place corruption
  scenarios where entry alignment survives.

Note: 48 is the entry *size*, not an alignment boundary. Do not confuse 48-byte-aligned
scanning with entry detection.

- **FPR near zero** — scan-based resync is viable; confidence scoring handles edge cases.
- **FPR materially above zero** — resynchronization is heuristic at best; NoTar v2 would
  need a per-entry magic value or checksum before reliable scan-based recovery is possible.

NoTar v1 has not yet been shown to support reliable scan-based resynchronization. The
empirical false-positive experiment must be completed before scan-based resynchronization
is relied upon.

### 4.5 Truncation Detection

No end-of-archive marker exists. Truncation is partially detectable through bounds
checks:

| Evidence                                         | What it bounds                                |
|--------------------------------------------------|-----------------------------------------------|
| `PayloadOffset + PayloadSize > archive_size`     | Payload region extends past end of file       |
| `ByteOffset + FileSize > archive_size` per entry | Individual payload extends past end of file   |
| `128 + FileListSize > archive_size`              | File list declared longer than file           |
| `FileCount × 48 > archive_size − 128`            | File-list entries cannot all be read          |
| Payload hash mismatch (once B5 + B6 fixed)       | Payload bytes changed from declared state     |

Not detectable: truncation that removes only trailing bytes of the last file's payload
when `FileSize` is also corrupted to match the truncated length.

---

## 5. Confirmed Bugs

### Critical — Round-Trip Is Broken

**B1 — Path storage loses directory structure**  
`NotarPackage.cs:67`: `Path.GetDirectoryName(file.FilePath)` computes each file's
immediate parent directory and passes it as the `relativeTo` base for `SerializePath`.
For a file at `TestFiles/TestDir/goodbye.txt`, `relativeTo` becomes `TestFiles/TestDir`,
so the stored path is `goodbye.txt` rather than `TestDir/goodbye.txt`. All subdirectory
information is unconditionally discarded.  
**Verified**: hex dump and Python parse confirm bare filenames with no path component.

**B2 — Payload read from wrong stream position during UnPack**  
`NotarPackage.cs:153–164`: after path deserialization the stream is at the end of the
last path entry (0x135 in the test archive). The payload loop reads sequentially from
there without seeking to `file.ByteOffset` (0x140 for file 0). In the test archive
this consumes 11 bytes of alignment padding before any payload is read; subsequent
files are shifted further. `ByteOffset` is read from the file list and stored in each
`NotarFile` but is never used during extraction.  
**Code-level proof**: no `Stream.Seek(file.ByteOffset, ...)` call exists anywhere in
the payload loop. The shift is nonzero whenever the alignment gap between path section
end and `PayloadOffset` is nonzero, which is the common case.

**B3 — writeStream never closed or disposed**  
`NotarPackage.cs:154`: `FileStream writeStream = new(...)` has no `using` statement
and no `Close()` or `Dispose()` call in `UnPack`. The FileStream internal write buffer
is not explicitly flushed. .NET does not guarantee GC finalizers complete before process
exit; buffered data may never reach disk.  
**Code-level proof**: no `using`, `Close()`, or `Dispose()` on `writeStream` anywhere
in `UnPack`.  
**Note on observed zero-byte result**: the zero-byte files observed in testing are
consistent with B3 (data never flushed) but do not independently prove it — B2 alone
would produce wrong-content files rather than zero-byte files. The correct diagnostic
sequence is: fix B3 first, rerun — the result then independently reveals B2's
wrong-content effect.

**B4 — Output directory ignored during extraction**  
`NotarPackage.cs:37–43`: `RelativeTo` is assigned the caller-supplied output directory
but never used inside `UnPack`. After `DeserializePath`, `file.FilePath` is a bare
filename; `new FileStream(file.FilePath, ...)` creates files in the process working
directory.  
**Verified**: extracted files appeared in the repo root, not in the specified output
directory.

### Medium — Integrity and Correctness

**B5 — PayloadHash stores only 20 of 32 SHA-256 bytes**  
`NotarPackage.cs:188–194`: `MakeHash` assigns `PayloadHashPartC` via
`BitConverter.ToUInt32(hash, 16)`, which reads SHA-256 bytes 16–19 (4 bytes) and
zero-extends to `ulong`. SHA-256 bytes 20–23 (the upper 4 bytes of PartC) and all
8 bytes of `PayloadHashPartD` are never set. The stored field contains 20 meaningful
bytes followed by 12 zero bytes.  
**Verified**: Python parse confirmed `stored_hash[20:]` = 12 zero bytes; bytes 0–19
match the recomputed SHA-256 exactly.

**B6 — Hash never verified during UnPack**  
`NotarPackage.cs:117–165`: `UnPack` neither calls `ComputePayloadHash` nor compares
any stored value. Payload corruption is undetectable at the archive level.

**B7 — IsValidHeader is dead code**  
`NotarHeader.cs:71–74`: `private`, no callers, never invoked during `Deserialize`.
Magic and HeaderSize checks are not applied at read time.

**B8 — PayloadHash.Deserialize called on property-getter copy; values discarded**  
`NotarHeader.cs:64`: `PayloadHash` is a struct property. `PayloadHash.Deserialize(stream)`
retrieves a copy via the getter, deserializes into that copy, then discards it. The
stream advances 32 bytes correctly but the deserialized hash data is lost. If hash
verification were added to `UnPack` without fixing this, it would always compare
against all-zeros.

### Low

**B9 — HandlePack off-by-one argument check**  
`CommandOrchestrator.cs:33`: checks `args.Length < 2` but accesses `args[2]`. Passing
only one argument after `pack` throws `IndexOutOfRangeException` instead of printing
usage.

**B10 — Dead store of FileSize in AddFile**  
`NotarFileList.cs:17`: FileSize is assigned on line 17 and immediately overwritten on
lines 18–20. The first assignment is dead.

**B11 — FileSize silently truncated to uint**  
`NotarFileList.cs:19`: the ternary evaluates to `file.Length` (`FileInfo.Length`,
type `long`) or `0`. The entire result is cast to `uint` (32 bits). For files larger
than 4 GiB the upper 32 bits are discarded. The truncated value is then zero-extended
into the `ulong FileSize` field — no further loss occurs in the write path — but the
value recorded in the archive is wrong. Example: a 5 GiB file
(`file.Length = 5,368,709,120 = 0x1_4000_0000`) yields `(uint) = 0x4000_0000 =
1,073,741,824` (1 GiB stored for a 5 GiB file); the pack loop reads only 1 GiB of
payload, producing a silently truncated archive.

**B12 — Pack uses FileMode.OpenOrCreate; stale bytes on reuse**  
`NotarPackage.cs:58`: re-packing to an existing output path that was previously longer
leaves old bytes beyond the new content. Should use `FileMode.Create`.

**B13 — Align16 uses int arithmetic; overflow near 2 GiB**  
`Utils.cs:9`: `(value + 0xF) & ~0xF` overflows silently for
`value ≥ Int32.MaxValue − 14`. Latent for archives approaching 2 GiB.

**B14 — FileList padding seek does not write zeros**  
`NotarFile.cs:33`: `stream.Seek(...)` advances the pointer without writing. For
`FileMode.OpenOrCreate` on an existing file, the 12-byte padding per entry may contain
stale bytes from a prior archive.

---

## 6. Extraction Security Risks

The current UnPack performs no validation on paths or offsets before using them. Items
C1–C5, C6a, and C7 are minimum safe extraction requirements to be addressed during
stabilization (§9 Batches 1–2). Item C6b represents more advanced filesystem-aware
hardening deferred to a future reader implementation.

**C1 — Absolute path overwrite** (`NotarFile.cs:60–65`, `NotarPackage.cs:154`)  
A crafted archive can store a path beginning with `/` or a drive root such as `C:\`.
`new FileStream(file.FilePath, ...)` writes to that absolute path, overwriting files
outside the intended output directory. Currently masked by B1 (paths are bare
filenames), but exposed immediately when B1 is fixed. Must be addressed in the same
stabilization batch as B1.

**C2 — Directory traversal via `..`** (`NotarFile.cs:60–65`)  
A stored path such as `../../.ssh/authorized_keys` passes `DeserializePath` without
validation. After joining with the output directory, the resolved path may escape the
output tree. Reject any path containing a `..` segment before further processing.

**C3 — Integer overflow in ByteOffset + FileSize** (`NotarPackage.cs:153–164`)  
Both fields are `ulong`. Their sum may overflow `ulong` or, after cast to `long` for
stream operations, yield a negative value. Verify
`ByteOffset ≤ archive_size − FileSize` using overflow-safe arithmetic before any seek
or read.

**C4 — ByteOffset outside archive bounds** (`NotarPackage.cs:153–164`)  
A `ByteOffset ≥ archive_size` causes `Stream.Seek` to succeed on a writable stream but
produces an empty or garbage read. Validate `PayloadOffset ≤ ByteOffset < archive_size`
before seeking.

**C5 — FileSize larger than remaining archive bytes** (`NotarPackage.cs:161`)  
`new byte[file.FileSize]` allocates without a prior bounds check. A value of 2^62
throws `OutOfMemoryException`; a value larger than available bytes causes `Stream.Read`
to return fewer bytes than requested, silently truncating the extracted file. Validate
`ByteOffset + FileSize ≤ archive_size` before allocating.

**C6a — Lexical path confinement** (minimum required; stabilization scope)  
After constructing the full output path with `Path.Combine(outputDir, relativePath)`,
call `Path.GetFullPath` on the result and assert that it begins with
`Path.GetFullPath(outputDir) + Path.DirectorySeparatorChar`. This catches rooted paths
and traversal sequences that survive the `..` check (C2) by canonicalizing before
comparison. It does not protect against symlinks or reparse points.

**C6b — Symlink and reparse-point confinement** (out of scope for stabilization)  
A path that passes lexical confinement (C6a) may still escape the output tree if
intermediate directories contain symlinks or reparse points that resolve outside it.
Reliable protection requires per-component filesystem resolution checks, not a
string-prefix comparison.

**C7 — FileCount drives unbounded loop and allocation** (`NotarPackage.cs:129–137`)  
The file-list loop iterates `FileCount` times without checking that
`FileCount × 48 ≤ archive_size − 128`. A malformed `FileCount = 10,000,000` allocates
and reads 480 MB before any other validation. Validate before entering the loop.

---

## 7. Suspicious / Undefined Behavior

**S1 — Local-time timestamp ambiguity** (`NotarFile.cs:28–29`)  
`FileInfo.CreationTime` and `FileInfo.LastWriteTime` return `DateTime` values with
`Kind = Local` on Windows. `DateTime.ToBinary()` serializes the `DateTimeKind` flag in
bits 62–63 and ticks (100-ns intervals since 0001-01-01) in bits 0–61. The binary
encoding is consistent across .NET platforms; the ambiguity is semantic. When an archive
created on a machine in one timezone is read on a machine in a different timezone,
`DateTime.FromBinary()` reconstitutes the same tick count as a local time in the
*reading* machine's timezone, representing a different UTC instant than the original.
Verified from a live archive: stored timestamps carry Kind = 2 (Local).

**S2 — Non-deterministic file enumeration order** (`NotarPackage.cs:29`)  
`Directory.EnumerateFiles` does not guarantee order. Two packs of the same directory
may produce differently-ordered archives. The pre-committed `TestFilesPacked.notar`
differs in file order from a freshly generated archive.

**S3** — No defined behavior for zero-byte files (FileSize = 0). Not tested.

**S4** — No defined behavior for duplicate paths. Not tested.

**S5 — FeatureFlags semantics undefined** (`NotarHeader.cs:16`, `NotarPackage.cs:105`)  
Always packed as 1; no flags are defined or consulted anywhere.

**S6 — FileLayoutVersion never checked on read** (`NotarHeader.cs:15`,
`NotarPackage.cs:104`)  
Always packed as 1; deserialization does not validate the value.

**S7 — PayloadSize cast overflows for large archives** (`NotarPackage.cs:175`)  
`(int)Header.PayloadSize` in `ComputePayloadHash` truncates for archives larger than
2 GiB.

**S8 — Inconsistent API shape** (`NotarPackage.cs:19–44`)  
`FromDirectory` is an instance method returning `this`; `ToDirectory` is static. The
pattern is inconsistent.

---

## 8. Test Coverage

**Active tests**: zero.

**Tests to write before each stabilization batch:**

| Test                                                          | Validates  | Before batch |
|---------------------------------------------------------------|------------|--------------|
| Round-trip: pack → unpack → byte-compare each file            | B1–B4      | Batch 0      |
| Header serialize/deserialize round-trip, all fields           | B8         | Batch 0      |
| Extracted file written under specified output directory       | B4         | Batch 1      |
| Payload content matches source when read from ByteOffset      | B2         | Batch 1      |
| FileCount × 48 exceeding available bytes rejected             | C7         | Batch 1      |
| ByteOffset out of archive bounds rejected                     | C4         | Batch 1      |
| ByteOffset + FileSize overflow or out-of-bounds rejected      | C3, C5     | Batch 1      |
| Path stored relative to archive root, not file parent         | B1         | Batch 2      |
| Absolute/rooted stored path rejected                          | C1         | Batch 2      |
| Path containing `..` rejected                                 | C2         | Batch 2      |
| Resolved path outside output root rejected (lexical)          | C6a        | Batch 2      |
| All 32 hash bytes stored and survive round-trip               | B5, B8     | Batch 3      |
| Corrupted payload rejected by hash verification               | B6         | Batch 4      |
| Wrong archive magic rejected                                  | B7         | Batch 4      |
| Wrong HeaderSize rejected                                     | B7         | Batch 4      |
| Pack to existing shorter path truncates file                  | B12        | Batch 5      |
| Zero-byte file in archive                                     | S3         | Batch 5      |

---

## 9. Stabilization Plan

**NoTar's existing `UnPack` implementation will be repaired through test-first
stabilization checkpoints.** The bugs are numerous but bounded; each has a clear fix,
and the format layout produced by `Pack` is correct and verified. Repair is the
appropriate approach for stabilizing the existing tool and freezing the format.

*Stabilization is not yet authorized as of this report.*

### Batch 0 — Test Infrastructure (no fixes)

1. Write round-trip test: pack `TestFiles` → unpack to temp dir → byte-compare each
   file. Run it; confirm it fails for a reason traceable to B1–B4.
2. Write header round-trip test: known fields → serialize → deserialize → assert all
   fields match. Run it; confirm it fails due to B8.

### Batch 1 — Extraction Correctness + Minimum Bounds Safety (B2, B3, B4, C3, C4, C5, C7)

B2, B3, and B4 are tightly coupled: all three must be fixed before the round-trip test
can produce correct output at the correct location. Write per-bug isolation tests before
fixing.

3. Write test: extracted file appears at the specified output directory (isolates B4).
4. Write test: payload content matches source when read from ByteOffset (isolates B2).
5. Write tests: FileCount × 48 exceeding available bytes rejected (C7); ByteOffset out
   of bounds rejected (C4); ByteOffset + FileSize overflow or out-of-bounds rejected
   (C3, C5).
6. Fix B3: add `using` to `writeStream` (`NotarPackage.cs:154`).
7. Fix B4: use `Path.Combine(outputDir, file.FilePath)` when opening `writeStream`.
8. Add C7: validate `FileCount × 48 ≤ archive_size − 128` before the file-list loop.
9. Add C4: validate `PayloadOffset ≤ ByteOffset < archive_size` per entry before seeking.
10. Add C3/C5: validate `ByteOffset + FileSize ≤ archive_size` (overflow-safe) per entry
    before allocating.
11. Fix B2: seek to `file.ByteOffset` before each payload read.
12. Run all Batch 1 tests — expect correct files at the correct location; expect
    round-trip failure on directory structure (B1 still unfixed).

### Batch 2 — Pack Path Correctness + Extraction Path Safety (B1, C1, C2, C6a)

B1 exposes C1 and C2 immediately: once proper relative paths are packed and extracted,
absolute and traversal paths become reachable. Fix B1 and minimum path safety in the
same batch.

13. Write test: stored path is relative to archive root, not file parent (isolates B1).
14. Write test: absolute/rooted stored path rejected (C1).
15. Write test: stored path containing `..` rejected (C2).
16. Write test: resolved path outside output root rejected after `Path.GetFullPath`
    check (C6a).
17. Fix B1: replace `Path.GetDirectoryName(file.FilePath)` with `this.RelativeTo`
    in `NotarPackage.cs:67`.
18. Add C1: reject stored paths beginning with a directory separator or drive root.
19. Add C2: reject stored paths containing a `..` segment.
20. Add C6a: after `Path.Combine(outputDir, relativePath)`, call `Path.GetFullPath`
    and assert the result begins with `Path.GetFullPath(outputDir) +
    Path.DirectorySeparatorChar`.
21. Run path tests, safety tests, and round-trip test — all must pass.

### Batch 3 — Hash Correctness (B5, B8)

22. Write hash round-trip test: pack → read back PayloadHash field → independently
    compute SHA-256 of payload → compare all 32 bytes.
23. Fix B5: use `BitConverter.ToUInt64(hash, 16)` for `PayloadHashPartC`; add
    `PayloadHashPartD = BitConverter.ToUInt64(hash, 24)`. (`NotarPackage.cs:188–194`)
24. Fix B8: use a local variable in `NotarHeader.Deserialize`: `var h = new PayloadHash();
    h.Deserialize(stream); PayloadHash = h;` (`NotarHeader.cs:64`)
25. Run hash test — must pass.

### Batch 4 — Hash Verification and Header Validation (B6, B7)

26. Write test: archive with one flipped payload byte is rejected by hash verification
    (isolates B6).
27. Write test: archive with wrong magic value is rejected (isolates B7 magic check).
28. Write test: archive with wrong HeaderSize value is rejected (isolates B7 size check).
29. Make `IsValidHeader` `internal`; call it at the top of `Deserialize`; throw
    `InvalidDataException` on failure. (`NotarHeader.cs:51`, `NotarHeader.cs:71`)
30. Add payload hash verification to `UnPack`: compare all 32 bytes against the stored
    hash. Fix the `(int)PayloadSize` cast in `ComputePayloadHash` (`NotarPackage.cs:175`)
    in the same step.
31. Run all Batch 4 tests — must pass.

### Batch 5 — Minor Correctness (B9, B11, B12, B13, B14)

32. Fix B9: change `args.Length < 2` to `args.Length < 3` in `HandlePack`
    (`CommandOrchestrator.cs:33`).
33. Fix B11: change `(uint)file.Length` to `(ulong)file.Length` on `NotarFileList.cs:19`;
    remove the dead assignment on line 17.
34. Fix B12: change `FileMode.OpenOrCreate` to `FileMode.Create` in
    `NotarPackage.cs:58`.
35. Fix B13: promote the `Align16` parameter from `int` to `long` in `Utils.cs:9`.
36. Fix B14: replace the padding seek with explicit zero writes in `NotarFile.Serialize`
    (`NotarFile.cs:33`).

*C6b (symlink and reparse-point confinement) is out of scope for stabilization.*

---

## 10. NOTAR_FORMAT.md Outline

1. Overview — format purpose, version identifier, design goals
2. Archive Layout — section table, 16-byte alignment rule
3. Alignment Function — exact formula, int vs. long considerations
4. Archive Header — field table with offsets, sizes, types, and semantics
5. File List — field table per entry, FileListSize derivation, fixed-slot structure,
   padding policy
6. Path Section — encoding (UTF-8, length-prefix, no null), entry order, relative-to-root
   semantics, total size not stored in header
7. Payload Section — ByteOffset semantics, per-file 16-byte alignment, gap policy
8. PayloadHash — intended full 32-byte SHA-256 encoding, field mapping
9. End-of-Archive — no marker; bounds checking sufficient for partial truncation detection
10. Open Questions — see §11

---

## 11. Open Questions

These cannot be answered from the source code and must be resolved before
`NOTAR_FORMAT.md` is finalized.

**Q1** — What does `FeatureFlags = 1` mean? Is bit 0 "payload hash present"? Is 0
a valid value?

**Q2** — What does `FileLayoutVersion = 1` mean? Should readers reject unknown values?

**Q3** — Is storing paths as bare filenames intentional, or was relative-to-root
intended but not yet implemented? (`RelativeTo` exists on `NotarPackage` but is unused
in `SerializePath`.)

**Q4** — What is the defined behavior for zero-byte files?

**Q5** — What is the defined behavior for duplicate paths?

**Q6** — Is there a maximum path length? `uint16` allows up to 65,535 bytes.

**Q7** — Is `PayloadSize = stream.Length − PayloadOffset` the exact intended definition,
including alignment padding after the last file?

**Q8** — Should `DateTime.ToBinary()` remain the timestamp encoding, or should a
platform-neutral format be adopted?

**Q9** — Are there constraints on `FileAttributes`? Test archives show both `0x20`
(Archive) and `0x80` (Normal).

**Q10** — Must the 12-byte padding per file-list entry be zero on write? A conforming
reader must tolerate nonzero values; should a conforming writer be required to zero them?
