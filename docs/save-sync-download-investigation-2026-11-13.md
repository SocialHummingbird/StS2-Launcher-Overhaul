# Save sync and game download investigation

Investigated on 13 November 2026 at source commit `111162be552bba05edc9e2e7cee1c5b860769724`.

The source confirms several mechanisms that add work: game updates operate on whole files, chunks within a file are sequential, interrupted partial files are discarded, save uploads are followed by read-back downloads, and every reconciliation captures the complete local save set. Their contribution to the reported delays remains unmeasured. No raw transfer logs, Steam depot manifests, installed game archives or real save inventories were found in the searched local evidence.

The existing save-safety probe passed **12/12 scenarios**, with exit code **0**. It exercises a fake remote, not Steam Cloud or Android networking. No device or Steam session was used. The connected Samsung was excluded. No launcher instrumentation, transfer changes or fixes were made.

## Evidence baseline

### Source, versions and local artifacts

The working tree was clean before the investigation. The source baseline is the commit above on `main`; the .NET SDK used by the existing probe is **9.0.315**. The documented release is **0.2.437 / 437002**, whose release notes identify APK source commit `9066e566dca7b131cfc9cddbeada4a97f935e607`. This investigation identifies source behaviour at `111162b`; it does not identify an installed app build. The default `0.2.0 / 200` in `android/gradle.properties` is not the documented release version. [Release provenance][release]

The local inventory visited **2,319 files**, excluding Git data, third-party/tool caches, build outputs, external source trees and this investigation's generated evidence. The exclusion rules and complete candidate list are retained in `tmp/save-sync-download-investigation-20261113/inventory.json`.

| Evidence class | Available evidence | Interpretation |
| --- | --- | --- |
| Existing raw log/report candidates | 0 matching `.log`, `.jsonl`, `.out`, or log/support/diagnostic `.txt` files in the inventory scope | No transfer event timestamps to analyse. Historical Markdown records are listed separately below. |
| Depot manifests | 0 `.manifest` files | No consecutive-version or cross-branch chunk comparison is possible. |
| Game archives | No `SlayTheSpire2.pck`; only the 1,336-byte `android/assets/bootstrap.pck` | The bootstrap pack cannot stand in for a Steam game archive. |
| Saves and sync state | 0 `.save` files; no `sync-state.json` or `sync-status.json` | Actual save counts, local bytes and remote hash coverage are unknown. |
| Installed branch state | No `steam_branch.txt` or `installation_state.json` | No completed installation can be associated with the inspected source. |
| Interrupted files | 0 `.downloading` files | No partial-download progress can be reconstructed. |
| Potential ZIP evidence archives | 0 in the inventory scope | No archived log or manifest set was available there. |
| Remote attachments | One JPEG | No transfer log or archive was supplied through the attachment directory. |
| Historical documentation | September startup investigation; current Android testing record | Secondary observations of connection failure, not current successful-transfer benchmarks. |
| Newly generated fixture evidence | `tmp/save-sync-download-investigation-20261113/save-safety-probe.log` | Twelve deterministic scenarios passed; compilation emitted CS8632 warnings. |

The expected `artifacts` directory is absent. Historical documents refer to files under that directory; those references are not substitutes for the missing raw files. Cache/source exclusions also mean the inventory is a search of relevant local evidence, not a claim about every file on the computer.

### Binary inspection

The staged `android/assets/dotnet_bcl/SteamKit2.dll` reports version **3.4.0**, with product revision `1c7bc9c41a529e8fbb1e6890f1e4dbcdc5200cb7`. ILSpy successfully decompiled its CDN client, depot chunk processor and crypto helper without modifying the binary. These are staged-file observations; no final release APK was available for comparison.

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| Staged `SteamKit2.dll` | 6,850,048 | `24cedf2c6ae19aed443b0f57d512794db68e6f8847d1a46443350185f6f42d70` |
| Staged `STS2Mobile.dll` | 1,526,784 | `9ab95049cf46c9ed033ecd93b1033e956d931856273a69a5b7801c38e0ae76a2` |
| Current-source Release `STS2Mobile.dll` produced by the probe build | 1,526,784 | `cc96088066f6d4ada5d7e9abe648abe3544503ed34cb1a77ce1222ff836219ec` |

The staged and freshly built launcher assemblies have different hashes. This alone does not establish different source behaviour: build/packaging transformations can change assembly bytes. No exact released-APK provenance is assigned to the staged files.

## Findings for I1–I8

“Confirmed in source” establishes the executed branch's semantics, not that the branch occurred during the user's slow operation. Performance impact is unmeasured for every issue below.

| ID | Behaviour verdict | Reproduction/evidence limit | Confidence |
| --- | --- | --- | --- |
| I1 | Prepared-PCK hash conflict follows from the code when preparation changes bytes | No original/prepared game PCK pair or corresponding manifest; not reproduced on a real archive | High for the conditional mechanism; occurrence unknown |
| I2 | Three Android file workers; one chunk request at a time per file | No request-overlap trace | High for the concurrency limit |
| I3 | Whole-file replacement; no unchanged-chunk or other-branch reuse in the game download path | No manifest pairs to calculate redundant bytes | High for the policy; overlap unknown |
| I4 | Incomplete file progress is discarded; committed complete files may be reused | No depot interruption/restart reproduction | High for cleanup semantics |
| I5 | Save transfers and transport operations are serial; uploads include read-back | Fake uploads bypass the real transport; dominance is unproven | High for the request sequence |
| I6 | Full snapshots occur before deciding whether anything changed; Pull performs additional snapshots | Existing probe covers basic Push/Pull safety, not scan cost or burst coalescing | High for scan counts |
| I7 | Invalid/missing hashes or negative sizes cause comparison downloads | Fake metadata always supplies valid hashes and sizes; live frequency unknown | High for the conditional branch |
| I8 | Polling and main-thread-dispatched Java crypto are present; game HTTP bodies already use files | Staged SteamKit calls inspected; no Android timing/thread trace | High for the inspected paths; cost unknown |

### I1 — Prepared PCK can be classified as corrupt

After downloads, the launcher patches the installed PCK in place. `ApplyPckEntryPatch` overwrites entry bytes without changing their length; rules include replacing `autoload/SentryInit` with `disabled/SentryInit`. A PCK containing an applicable rule consequently differs from its original Steam SHA-1. [Preparation][pck-prepare] · [Byte writes and rules][pck-writes]

When the downloader next runs for that branch, an unchanged manifest entry is checked against its original Steam file hash. A mismatch selects `CorruptNeedsDownload`; the file worker checks the same file again, then downloads every chunk if it still fails. No Android-prepared-digest exception exists in this selection path. For a same-length prepared PCK, this path also reads the existing archive for SHA-1 in both selection and the worker's reuse check. [Selection][selection] · [Worker and verification][file-writer]

This is a conditional source finding. It applies when the downloader runs and an applicable patch changed the PCK. It does not establish that every update check or game launch redownloads it. A genuinely changed PCK manifest already requests replacement for a separate reason; an unrelated corruption produces the same mismatch log. The bootstrap pack was not used as a surrogate game archive.

### I2 — Large files have only one active chunk request

Android's maximum is three workers, and `WorkerCount` is `min(maximum, file count)`. Each worker takes one file and awaits every chunk in offset order before taking another file. Depots also run sequentially. With one large archive remaining, that file has at most **one outstanding logical chunk download**, irrespective of the other two file workers. A log saying “with 3 threads” reports the configured maximum, not measured active network concurrency. [Limits and depot order][download-limits] · [Workers][workers] · [Chunk loop][file-writer] · [Log message][depot-plan]

The limit is confirmed. Bandwidth saturation, idle time, server connection reuse and achievable throughput cannot be determined without request-level observations.

### I3 — Manifest comparison is a file delta, not a chunk delta

`ManifestEntryChanged` compares whole-file hashes. Once a file needs replacement, the writer iterates all its chunks; there is no lookup of reusable old chunks in that path. The state store retains manifests and IDs, not a content-addressed chunk cache. Game and download-state paths belong to the selected branch, and this path does not search another installed branch for source chunks. [Selection][selection] · [Chunk loop][file-writer] · [Manifest state][manifest-state] · [Branch paths][branch-paths]

No redundant-byte total can be calculated from the available files. The reproducible calculation, once real manifest pairs exist, is: for each queued changed file, sum the target chunks' `CompressedLength` where the same chunk ID and uncompressed length exist in the old manifest. Report repeated chunk occurrences and unique chunk IDs separately. Repeat with another branch's manifest as a separate comparison. A manifest match establishes potential reuse; source bytes, especially in a prepared PCK, still need verification before calling them usable.

### I4 — Cancellation loses the current partial file, not the entire install

Before writing, the worker deletes its `.downloading` file. Its `finally` deletes that file after success, failure or cancellation; downloader startup also cleans stale `.downloading` files left by a process exit. No completed-chunk bitmap is restored. A retry therefore restarts an unfinished file, but already committed files can pass their whole-file verification and be reused. [File lifecycle][file-writer] · [Stale cleanup][stale-cleanup] · [Restart order][depot-plan]

User cancellation is checked between chunks; the caller token is not forwarded into `DownloadDepotChunkAsync`. A current chunk may therefore settle before that cancellation is observed. [Chunk call][chunk-call]

The passing “interrupted Pull leaves local saves intact” fixture concerns transactional **save Pull**, not game depot downloads. It establishes rollback of that fake save transfer and cleanup of staging, not chunk resume. No real installation was interrupted. [Existing fixture][pull-fixture]

### I5 — Upload verification adds a serialized download

The service awaits uploads one file at a time. The transport holds a single-operation semaphore across each upload, including verification; Cloud RPCs additionally share a send lock. A normal new upload performs `ClientBeginFileUpload`, sequential HTTP block requests, `ClientCommitFileUpload`, `ClientFileDownload`, then an HTTP GET and hash comparison. With one block and a nonempty download URL, that is **three Cloud RPCs plus two HTTP requests per changed file**, excluding enumeration, authentication and retries. These are request counts, not necessarily separate connections. [Service loop][mirror] · [Upload and read-back][upload] · [Upload protocol][upload-protocol] · [Transport lock][transport-lock] · [RPC lock][cloud-rpc]

The `DuplicateRequest` path skips block upload and commit but still performs read-back. Deletes also issue a second Cloud RPC to verify a tombstone. The fake's `UploadAsync` directly copies bytes into an in-memory dictionary, so the passing Push scenarios do not execute compression, real transport locks, RPCs, HTTP or read-back. [Fake remote][fake-remote]

The raw byte count logged for an upload is not its network payload size: upload preparation uses ZIP data when smaller, and downloaded bytes are logged after decompression. No upload-to-verification duration can be extracted without actual transfer timestamps. [Compression][compression]

### I6 — Full capture precedes the no-change decision

Every reconciliation enumerates remote files, discovers all managed local save paths, reads every local file and calculates a managed SHA-1 before deciding NoChange, Pull, Push or conflict. There is no per-file dirty-hash cache in that capture. Each stable-capture attempt may repeat the complete scan, up to three attempts if local changes are observed. [Reconciliation][sync-entry] · [Capture][capture] · [Local read][local-read]

For a successful stable run, the code accounts for the following content work. Let `S_before` and `S_after` be total managed local save bytes before and after Pull, and `C` the total raw bytes of changed uploads:

| Path | Local snapshot reads/hashes | Additional content hashing |
| --- | --- | --- |
| NoChange | One full snapshot: `S_before` | Remote fallback hashing only if I7 applies |
| Push | One full snapshot: `S_before` | Normally `3 × C`: prepare upload hash, hash local bytes again for read-back comparison, hash read-back bytes |
| Pull | Two pre-apply snapshots and one post-apply snapshot: `2 × S_before + S_after` | Changed incoming files are hashed by `EnsureRemoteBytesAsync` and again after staging read-back; I7 can add another remote hash |
| Snapshot instability | Up to three full attempts per stable capture; an automatic retry starts a new reconciliation | Depends on which phase was reached |

These are code-derived byte formulas, not measured file inventories or timings. Pull's second pre-apply snapshot detects local changes; its final snapshot and staging hashes verify the transaction. [Pull sequence][pull] · [Remote byte validation][remote-validate] · [Upload hashes][upload] · [Upload preparation][compression]

Automatic Push already coalesces commits received during an active worker into a pending flag, then rechecks pending/dirty state before another pass. It has no initial quiet-period debounce. Each local mutation also persists dirty state before queueing. The existing gameplay-Push scenario exercises one mutation while holding its upload; it does not establish the effectiveness of coalescing a burst. Actual mutation-to-sync ratios are unavailable. [Worker/coalescing][coalescing] · [Existing gameplay fixture][gameplay-fixture]

### I7 — Metadata can force downloads before the decision

`CaptureRemoteSetAsync` downloads a managed remote file when its hash cannot be normalized or its size is negative. It hashes those bytes to create the comparison entry. This happens before the synchronization decision, so even a final NoChange or overwrite-confirmation result can have downloaded data for comparison. Accepted hashes are 40 hexadecimal characters, or Base64 decoding to exactly 20 bytes. [Fallback][capture] · [Normalization][hash-normalization]

Enumeration's “content hashes” count checks only whether `file_sha` is nonempty; it does not validate that hash. A nonzero count cannot prove the fallback was avoided. Every successful fake enumeration returns a valid hexadecimal hash and nonnegative length, so the existing fixture does not exercise missing/invalid-hash fallback. [Enumeration][enumeration] · [Fake metadata][fake-remote]

For a nonempty fallback file with a usable download URL, the extra comparison involves one download-metadata RPC and one HTTP GET, followed by a managed hash. Those captured bytes can subsequently be reused by Pull; they are not necessarily downloaded a second time. Frequency and total bytes remain unknown.

### I8 — Bridge work is real, but its elapsed cost is unknown

The managed HTTP handler starts a Java request, polls immediately, and sleeps **50 ms after an empty poll**. Start and poll calls pass through the dispatcher. The Java network operation runs on its own worker; it is not performed on the Godot thread. When the normal Android dispatcher is registered, queued bridge actions and synchronous crypto run on its registered Godot thread. The polling interval is neither a mandatory 50 ms per request nor a bound on total delay, because dispatcher scheduling adds an unknown wait. [HTTP polling][http-poll] · [Java worker][java-worker] · [Dispatcher][dispatcher]

The staged SteamKit depot processor directly calls `AndroidJavaCrypto.AesDecryptEcb` for its IV and `AesDecryptCbc` for payload decryption. The launcher then verifies the uncompressed chunk through Java SHA-1. That gives **three crypto bridge calls per successfully processed game chunk**, besides HTTP start/poll calls and other work. Whole-file SHA-1 verification also synchronously calls a Java method that reads the complete file. Base64 conversion is present in these AES and byte-hash bridges. The staged decompilation files retained with the evidence identify the exact call sites. [Crypto patch rules][crypto-patches] · [Chunk verification][file-hashes] · [Crypto dispatch][crypto-dispatch] · [Java digest/AES][java-crypto]

There are two important limits on the earlier broad hypothesis. First, Java streams `/chunk/` and `/manifest/` HTTP responses to temporary files; they are not shipped back as Base64 bodies. That introduces a file write/read path whose time is also unknown. Second, save-file content hashes use `ManagedSha1`, so Java per-chunk SHA cost must not be attributed to every save hash. [HTTP response files][response-files] · [Save hashing][hash-normalization]

## Timelines, competing explanations and ranking

### Available timing samples

| Scenario | Comparable raw samples | Timing result |
| --- | ---: | --- |
| Unchanged sync | 0 | Connection, enumeration and local capture unmeasured |
| Live Pull | 0 | Transfer, staging and application unmeasured |
| Live Push/read-back | 0 | Upload-to-verification interval unmeasured |
| Automatic gameplay sync | 0 | Mutation-to-sync ratio and burst timing unavailable |
| Initial download, update, interruption/retry | 0 each | No request overlap, network throughput or repeated-byte measurement |

No median, range or current transfer timeline is defensible with zero raw samples. The September startup document reports a roughly **31-second failed Cloud connection**, but its referenced raw Stage 9 logs are absent. It concerns an earlier local candidate and is not a successful-transfer timing or current-release benchmark. The current Android testing record also describes an **offline TimeoutException**, without a duration. These observations keep connection failure relevant without attributing the current report to it. [Historical record][historical] · [Current testing record][current-status]

### Other mechanisms that could explain apparent slowness

- **Connection and retry waits:** source settings include a 30-second connect timeout, backoff capped at 32 seconds, 45-second Cloud RPC timeout and three-minute Cloud operation/startup bounds. These are configured bounds, not observed delays. A retained save transport can reuse its connection; the configured 90-second idle timeout does not mean every file logs in again. [Connection constants][connection] · [Cloud RPC][cloud-rpc] · [Transport lifecycle][transport-lock] · [Startup boundary][startup]
- **CDN choice:** servers are ordered by advertised weighted load; the current server changes on failure. A slow successful server is not independently measured or replaced for low throughput. There are up to five logical download attempts. No local trace establishes retries or a poor endpoint. [CDN selection][cdn-selection]
- **A request/body timeout interaction:** the inspected SteamKit client sets a 10-second request timeout before awaiting `SendAsync(ResponseHeadersRead)`, then switches to a 60-second body timer after it returns. The Android handler returns only after Java has consumed the full response body. Thus the first timer also covers Java body transfer and bridge delivery in this integration. No override of those SteamKit timeout properties was found in launcher source. This is a source/binary-derived competing hypothesis for retries, not an observed failure. The Java cancellation method suppresses delivery but does not disconnect an in-flight HTTP connection; actual overlapping abandoned transfers remain unverified. [Handler return boundary][http-handler] · [Java response/cancellation][java-worker]
- **Local work after/before download:** free-space checks, complete-file SHA-1 verification, PCK preparation and final installation identity work are distinct from network transfer. PCK preparation explicitly flushes to disk. Their durations cannot be recovered from the available evidence. [File writer][file-writer] · [Preparation flush][pck-flush]

The progress counter adds uncompressed chunk bytes and credits whole-file bytes when a file is reused. It resets per depot. It is therefore not a wire-byte counter, and its UI percentage cannot establish download speed. [Chunk accounting][progress] · [Reuse credit][file-writer] · [Reset and total][progress-reset]

**Measured-contributor ranking: unavailable.** There is no evidence-supported ordering by seconds or percentage of delay. The source establishes avoidable/repeated transfer mechanisms for I3–I5, repeated local processing for I6, and a concurrency constraint for I2. I1 and I7 require particular inputs whose occurrence is unknown; I8 and the timeout interaction have no isolated cost measurement. These groups are evidence categories, not a speed ranking, and overlapping work must not be added together as separate elapsed delays.

## Checks and reproduction commands

Run commands from `C:\Development\StS2-Launcher`. They inspect local files or execute the existing fake-remote probe; none require a device or Steam credentials.

```powershell
git rev-parse HEAD
git status --porcelain=v1
dotnet --version
pwsh -NoProfile -File scripts\test-local-gameplay-save-safety.ps1
python tmp\save-sync-download-investigation-20261113\inventory.py

$evidence = 'tmp\save-sync-download-investigation-20261113'
.\.tools\ilspy\ilspycmd.exe --disable-updatecheck -t SteamKit2.CDN.Client android\assets\dotnet_bcl\SteamKit2.dll > "$evidence\staged-steamkit-cdn-client.cs"
.\.tools\ilspy\ilspycmd.exe --disable-updatecheck -t SteamKit2.CDN.DepotChunk android\assets\dotnet_bcl\SteamKit2.dll > "$evidence\staged-steamkit-depot-chunk.cs"
.\.tools\ilspy\ilspycmd.exe --disable-updatecheck -t SteamKit2.CryptoHelper android\assets\dotnet_bcl\SteamKit2.dll > "$evidence\staged-steamkit-crypto.cs"

Get-FileHash -Algorithm SHA256 -LiteralPath android\assets\dotnet_bcl\SteamKit2.dll,android\assets\dotnet_bcl\STS2Mobile.dll
rg -n -g 'DepotDownloader*.cs' 'WorkerCount|WriteChunksAsync|CleanupStaleDownloadTemps|VerifyFileHash|RecordChunkWritten|ComputeTotalDownloadBytes' src\STS2Mobile\Steam
rg -n 'CaptureStableLocalSet|CaptureLocalSet|MirrorLocalToSteamAsync|CaptureRemoteSetAsync|NormalizeRemoteHash' src\STS2Mobile\Steam\SaveSyncService.cs
```

The probe's passing cases cover containment, atomic local writes, sync decisions, status presentation, interrupted Pull, retryable failed Push, pre-load ordering, gameplay Push queueing, shared manual sync, mod/version persistence and the startup guard. Its remote always supplies valid hashes; it does not emulate SteamCloudTransport. The probe command exited 0; all three final decompilation commands and the inventory script also exited 0. An initial decompilation used the wrong namespace (`SteamKit2.DepotChunk`) and exited 70; listing types identified `SteamKit2.CDN.DepotChunk`, whose decompilation succeeded. No type or assembly was changed to resolve that lookup.

Generated inventory, fixture output and decompilations are retained under the ignored `tmp/save-sync-download-investigation-20261113` directory as evidence. The inventory excludes that directory so its newly generated log is not counted as pre-existing evidence. Its visit count describes the recorded snapshot and may vary after later build activity. Only this report is added to tracked files.

## Remaining evidence needed

| Issue | Smallest missing evidence that would materially narrow the result |
| --- | --- |
| I1 | One branch's expected PCK hash/manifest, original and prepared PCK copies, and the download-selection log for that unchanged manifest. This distinguishes preparation from corruption and a genuine version change. |
| I2 | An existing request-level trace from a large-file download, identifying overlap and response durations. File-level UI logs alone cannot measure active requests. |
| I3 | Two adjacent manifests for one branch; a second branch's manifest for the separate cross-branch calculation. Actual source chunks are needed to verify availability after Android preparation. |
| I4 | One matched cancellation/restart log pair and corresponding partial/committed-file inventory from an allowed test environment. |
| I5 | An existing timestamped real Push trace including upload completion and read-back completion, plus raw/payload sizes. Actual live upload performance remains outside this run. |
| I6 | A managed-save file-count/size inventory and an existing I/O/CPU trace of an unchanged sync or burst. Current general logs do not isolate hashing time; extra logs with no phase boundaries would still be insufficient. |
| I7 | A read-only remote metadata snapshot with sizes and hash representations, paired with a sync log that can identify comparison downloads. Private paths/content should be omitted from shared evidence. |
| I8 | An existing thread/CPU and request trace separating Java transfer, dispatcher waits, crypto and file I/O. The current polling constant cannot substitute for that measurement. |
| Competing explanations | A timestamped failing/slow request trace with build provenance, error/retry events and connection state. This could distinguish endpoint latency, timeout/retry behaviour and post-download preparation. |

All eight issues have an evidence-backed disposition above. Their mechanisms are established to the stated extent; attribution of the user's actual delays remains inconclusive within the existing-logs, fixture-only and device-exclusion boundaries. This report contains findings and evidence gaps, without an implementation design or fix plan.

[release]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/docs/release-notes/v0.2.437.md#L42
[pck-prepare]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/DepotDownloader.cs#L88
[pck-writes]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/DepotDownloader.PckPatch.cs#L103
[selection]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/DepotDownloader.FileManifest.cs#L49
[file-writer]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/DepotDownloader.FileSystem.cs#L189
[download-limits]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/DepotDownloader.cs#L18
[workers]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/DepotDownloader.FileManifest.cs#L171
[depot-plan]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/DepotDownloader.DepotDownload.cs#L89
[manifest-state]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/DepotDownloader.StateStore.cs#L39
[branch-paths]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/SteamGameInstallPaths.cs#L32
[stale-cleanup]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/DepotDownloader.FileManifest.cs#L164
[chunk-call]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/DepotDownloader.CdnDownloadOperation.cs#L110
[pull-fixture]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/tools/LocalGameplaySaveSafetyProbe/Program.cs#L581
[mirror]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/SaveSyncService.cs#L1240
[upload]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/SteamCloudTransport.cs#L277
[upload-protocol]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/SteamCloudTransport.cs#L356
[transport-lock]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/SteamCloudTransport.cs#L642
[cloud-rpc]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/SteamConnection.CloudMessages.cs#L12
[fake-remote]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/tools/LocalGameplaySaveSafetyProbe/Program.cs#L1533
[compression]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/SteamCloudTransport.cs#L503
[sync-entry]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/SaveSyncService.cs#L611
[capture]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/SaveSyncService.cs#L1283
[local-read]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/AndroidLocalSaveStore.FileIo.cs#L11
[pull]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/SaveSyncService.cs#L946
[remote-validate]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/SaveSyncService.cs#L1363
[coalescing]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/SaveSyncService.cs#L367
[gameplay-fixture]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/tools/LocalGameplaySaveSafetyProbe/Program.cs#L722
[hash-normalization]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/SaveSyncService.cs#L1509
[enumeration]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/SteamCloudTransport.cs#L157
[http-poll]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/AndroidJavaHttpMessageHandler.Request.cs#L76
[java-worker]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/android/src/com/game/sts2launcher/GodotApp.java#L2636
[dispatcher]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/AndroidBridgeDispatcher.cs#L51
[crypto-patches]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/tools/SteamKitAndroidPatch/Program.cs#L298
[file-hashes]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/DepotDownloader.FileSystem.cs#L288
[crypto-dispatch]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/AndroidJavaCrypto.cs#L36
[java-crypto]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/android/src/com/game/sts2launcher/GodotApp.java#L3066
[response-files]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/android/src/com/game/sts2launcher/GodotApp.java#L2945
[historical]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/docs/startup-responsiveness-2026-09-07.md#L13
[current-status]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/docs/current-android-status.md#L56
[connection]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/SteamConnection.cs#L35
[startup]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Launcher/LauncherStartupFlow.Saves.cs#L12
[cdn-selection]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/DepotDownloader.CdnDownloadOperation.cs#L139
[http-handler]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/AndroidJavaHttpMessageHandler.cs#L104
[progress]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/DepotDownloader.CdnDownloadOperation.cs#L218
[pck-flush]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/DepotDownloader.PckPatch.cs#L68
[progress-reset]: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/blob/111162be552bba05edc9e2e7cee1c5b860769724/src/STS2Mobile/Steam/DepotDownloader.FileManifest.cs#L270
