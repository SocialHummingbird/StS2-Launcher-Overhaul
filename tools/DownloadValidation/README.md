# Controlled Android chunk-download validation

This test-only harness runs against the authorised Fold `R3GL70DKNKB` / `SM-F971B`.
It keeps every download under the launcher's private `files/download-validation/<run>/`.
The signed instrumentation helper backs up saves and preferences and hashes installed
versions and the original encrypted credential file. It never decrypts or exports credentials.
All host evidence lives in ignored `tmp/chunk-download-work/device/`; the `private/`
subdirectory contains save and preference backups and must not be published.

The optional MSBuild target injects generated copies of the current controller, UI
lifecycle and depot-operation files with strict single-occurrence replacement guards.
It bypasses normal session startup, automatic save sync, and automatic
game launch even when no validation request exists. Production sources have no test hooks.
The runner copies only encrypted credential bytes into its private sandbox, then uses the
normal Keystore reader and on-demand session code there. It never configures SaveSyncService.

Build the small helper without Gradle:

```powershell
./tools/DownloadValidation/build-helper.ps1
```

Before installing it, capture the installed launcher APK using **only**
`adb -s R3GL70DKNKB shell pm path com.sts2launcher.overhaul.fork.local` and `adb pull`.
Use `Get-AndroidApkIdentity` from `scripts/android-signing-utils.ps1` to compare the
helper's signer with the installed launcher's signer. Install the helper with `install -r`.
Capture and pull the baseline snapshot before launching or updating the launcher:

```powershell
./tools/DownloadValidation/run-device.ps1 -Action Snapshot -Name before
```

Build the validation APK using the ordinary build safeguards and a higher version code:

```powershell
./scripts/build-android-local.ps1 -VersionName 0.2.438-downloads-validation -VersionCode 438001 `
  -Abi arm64-v8a -ManagedPublishTargets "$PWD/tools/DownloadValidation/DownloadValidation.targets"
```

Run `verify-android-apk.ps1` and `verify-android-update-compat.ps1` against the captured
installed APK before any candidate install. Install the candidate with `install -r`;
never uninstall or clear launcher data. A normal final APK must use a higher version
code than this test-only APK because the validation build disables normal launcher actions.

Run a real empty-root download, cancel after completed payload, and retain the result:

```powershell
./tools/DownloadValidation/run-device.ps1 -Action Start -RunId fresh -CancelAfterBytes 33554432
./tools/DownloadValidation/run-device.ps1 -Action Status -RunId fresh
./tools/DownloadValidation/run-device.ps1 -Action Pull -RunId fresh -Name cancelled
./tools/DownloadValidation/run-device.ps1 -Action Start -RunId fresh
```

`Status` reads external JSON only and does not stop the app. Instrumentation starts a
target process, so `Start` deliberately force-stops before replacing the private request.
Once resumed payload is advancing, `ForceStop` and then `Launch` exercise abrupt process
death with the existing request and staging. Poll `Status`, and pull results after completion.
Require `status=passed`, a ready installation matching its GameIdentity, final raw-file
manifest verification, and prepared PCK verification by its ready identity. Depot summary
logs report reused/resumed/downloaded/planned bytes. `depot-progress.json` and
`raw-progress.jsonl` retain raw progress even on interrupted runs. `chunk-events.jsonl`
records every logical chunk-fetch start/completion/cancellation with its content identity
and compressed payload size; `chunk-summary.json` retains totals and peak concurrency.
These observations wrap the production chunk fetch only in the validation build. CDN
retries within one logical fetch are reported by ordinary downloader logs. A successful cancellation must report
`installationReady=false` and retained partial files.

Additional operations:

```powershell
./tools/DownloadValidation/run-device.ps1 -Action Start -RunId copied-update -Operation copied-update
./tools/DownloadValidation/run-device.ps1 -Action Start -RunId layout-fixture -Operation fixture
```

`copied-update` copies the selected real installation and manifests into the sandbox and
runs the ordinary downloader against the live Steam branch. Its evidence distinguishes
the captured source from the live target by manifest IDs in the logs. The fixture uses a
captured installed manifest and real content, renames the target, rotates chunk offsets,
and corrupts one range in a copied donor. It requires the final SHA-1, intact-chunk reuse,
and exactly one real CDN chunk request. Its target layout/manifest ID is synthetic and is
explicitly labelled; this is not evidence that a new live Steam release exists.

Finish by pulling each run, taking a final snapshot, and comparing protected hashes:

```powershell
./tools/DownloadValidation/run-device.ps1 -Action Snapshot -Name after
./tools/DownloadValidation/run-device.ps1 -Action Compare -Name after
./tools/DownloadValidation/run-device.ps1 -Action Cleanup
```

Cleanup deletes only the two named test-owned device directories after a local backup
and a comparison with zero semantic user-data differences exist. The comparison retains
raw file differences. It compares every typed `sts2mobile.xml` user preference except
the two native APK/runtime metadata keys `installed_version_code` and
`assembly_cache_runtime_id`, and identifies the exact `files/profileInstalled` AndroidX
runtime marker separately from saves. It never excludes the whole preferences file.
The exact `IDSCount` int in `android.app.ActivityThread.IDS.xml` is also recognised as
framework observation metadata. Ownership was verified against the Fold's own
`framework.jar`: `android.app.IdsController.closeIdsWindow()` increments that key and
writes it with `putInt/apply`. Every other typed field in that file is still compared.
Remove the helper package separately after it is
no longer needed. Keep the launcher's app data and original installed game versions.
The results below distinguish raw-file fixture checks from full installation acceptance.
The injected runner does not exercise tapping the normal launcher Pause/Resume controls or gameplay.

The optional `-Provider akamai` request selects the discovered `steampipe.akamaized.net` only in
the validation build and skips the optional CDN-directory WebAPI lookup. Normal Steam
ownership, app-info, depot-key and manifest request-code checks still run. This explicit
override must be stated with any resulting live-test evidence; normal APK discovery and
rotation are unchanged. `http-events.jsonl` records only scheme, host, port and bridge
start/poll/completion/cancellation timing, without URLs, headers or authentication data.

## Managed and packaged verification

- Managed regression suite: **256 passed, 0 failed**, including delta reuse,
  shared worker limits, corrupt/truncated sources, pause and commit recovery,
  prepared-PCK authority, branch lifecycle, selected-branch recovery and Workshop safeguards.
- Existing local gameplay/save-safety probe: **12/12** deterministic scenarios.
  Its fake remote does not establish live Steam Cloud behavior.
- Rendered Godot interaction profiles: **412×915 compact and 1280×800 desktop passed**,
  including Pause/Resume, busy controls and payload/reused/resumed text fitting.
- Normal local APK: `0.2.438-downloads-local / 438008`, ARM64 contents, Android crypto
  and update-signature checks passed. The packaged assembly contains no validation
  harness; transfer-engine IL matches the signed validation APK used on the Fold.

The local logs are under ignored `tmp/chunk-download-work/`: `final-managed.log`,
`save-safety.log`, `ui-preview/interaction-final-*.log`, `normal-apk-build.log`,
`normal-update-compat.log` and `packaged-engine-audit.json`.

## Fold results, 13 November 2026

The same-signed helper captured the installed `0.2.437 / 437002` APK and a private
baseline before any launcher launch/update. The baseline backed up 731 save,
preference and metadata files and hashed 1,177 files, including 453 installed-version
files (6,070,584,871 bytes) and the original encrypted credential file. No original
credential bytes were exported. Only Fold `R3GL70DKNKB` was targeted.

Live acceptance remains incomplete. The default-provider empty-root download and
copied-installation update each exhausted five manifest requests at approximately
10 seconds per request, before any payload. The corrected real-content fixture
verified 14,215,464 local bytes and planned one 281,184-byte compressed CDN chunk,
then exhausted the same five timeouts. Standalone Java probes obtained that exact
chunk with HTTP 200 over HTTP and HTTPS in about 30 seconds; later probes observed
`EAI_NODATA` DNS failures. These observations do not establish a single cause.

An unsuitable test-only `steamcdn-a.akamaihd.net` selector returned 404 and was removed.
Its HTTP telemetry showed successful Godot dispatch/polling and responses within
64–536 ms. The final explicit selector used the actual discovered
`steampipe.akamaized.net` endpoint with HTTPS, port 443, matching Host/VHost and no
directory lookup; it still exhausted five 10-second chunk timeouts. The shipping
provider discovery/rotation was never changed.

The offline Android fixture completed controlled cancellation, process death,
relaunch, verified range reuse and final raw-file promotion. It used captured Steam
manifest `4733892780027453168`, a synthetic rotated layout, three local-source workers
with an explicit test delay, and **zero network payload**. It bypassed credential and
Steam-session loading.

| Attempt | Outcome | Verified resumed bytes | Newly copied local bytes | Peak workers | Completed IDs refetched |
| --- | --- | ---: | ---: | ---: | ---: |
| 1 | Cancelled; one partial retained | 0 | 3,145,728 | 3 | 0 |
| 2 | Force-stopped; PID absence checked | 3,145,728 | 3,145,728 | 3 | 0 |
| 3 | Relaunched and completed | 6,291,456 | 8,972,584 | 3 | 0 |

All 15 distinct completed chunks reconstructed 15,264,040 bytes. There was one
promotion after verification; final raw-file SHA-1 was
`BE9B0959AA3221ACA0122F340D33FCBE8169731B`. This proves raw-file engine behavior on
Android using a local verified fixture. It does not prove a full live Steam download,
network delta savings, Android PCK preparation, or final GameIdentity readiness.

Still needed on a working Steam CDN connection: a complete empty-root installation,
an installed-version update with prepared-PCK retention, Pause/Resume and process-death
recovery during actual CDN transfers, and final installation readiness/file checks.
No elapsed-time improvement is claimed without a comparable baseline.

The post-validation snapshot found zero semantic user-data changes: all 453 installed
file hashes, original credential hash, saves and four typed user preference keys
matched the baseline. The validation-only snapshot retained two raw file differences:
native APK/cache keys in `sts2mobile.xml` and the AndroidX `profileInstalled` marker. No stale runtime
metadata was restored. Normal launcher Pause/Resume tapping and gameplay were not
tested by this injected harness.

Inspectable, ignored evidence includes `device/offline-summary.json`, per-attempt
JSONL chunk/progress events, `device/preservation-after-validation.json`, signed APK
identities and failed live-request outputs. Private backups stay under `device/private/`
and must not be published.

Final normal APK `0.2.438-downloads-local / 438008` was installed as a compatible
update. Wi-Fi/mobile data were temporarily disabled, `Active default network: none`
was verified, and the normal Home/Play Modded/Game version UI and controller-ready
marker were observed. The app was force-stopped before restoring connectivity.
Recorded Wi-Fi/mobile/airplane settings were restored exactly. The final snapshot
retained a third expected raw difference: the framework-owned `IDSCount` counter.
No Steam Cloud network work was possible during the normal UI smoke check.

Final comparison reported zero semantic user-data differences and three retained
framework/runtime metadata file differences. The installed normal APK was pulled back
and matched the verified artifact byte-for-byte:
`9723ea26c04c443ecd6c4d570f772dade72dd715bb3db9fb3ee6cee1e0d4d257`.
All test-owned private/external directories were removed after evidence was pulled,
the helper package was uninstalled, and the normal launcher remained installed and
stopped. Final Wi-Fi/mobile/airplane values matched their recorded original state.

Remaining live acceptance gaps are a complete empty-root Steam download, network
payload cancellation/restart with verified CDN IDs not refetched, a completed
copied-installation update/delta comparison, and final PCK/GameIdentity readiness
through the live downloader. Offline file-fixture success does not fill these gaps.

## CDN investigation continuation, 13 November 2026

This campaign was investigation and testing only. The 256 managed tests, 12 save-safety
fixtures and previous offline Fold results above are retained; they were not rerun.
No production C# or Java source changed during this campaign. The frozen inventory
checks 523 production source files against their starting SHA-256 values. The source
baseline is commit `111162be552bba05edc9e2e7cee1c5b860769724` plus the existing, uncommitted
chunk implementation. The private starting diff and working-tree inventory are retained.

Only SM-F971B `R3GL70DKNKB` was used. Installed build `438008` was verified and pulled
before execution. A fresh snapshot backed up 731 files and hashed 1,178 files, including
installed versions and the original encrypted credential file. Credential bytes were
not exported. The original branch was `public-beta`. Original Wi-Fi/mobile/airplane and
private-DNS/connectivity observations are retained privately; provider selection, DNS,
VPN and network policy were not changed for transfers.

Test APKs `438009`, `438010` and `438011` used the existing signer and compatible updates.
The first diagnostic build grouped Java request timing, managed bridge timing, SDK
request metadata and matched-resource replay support. After the live gate passed, the
planned opt-in sandbox UI mode was added. Its first run exposed an observer error on
absent installation state before any content request; that test-tool error was corrected
in `438011`. A pre-install review also removed a potential observer/dispatcher lock
inversion. Neither change touched the transfer engine. These are test-tool corrections,
not evidence of launcher defects.

Generated test-build sources add observations at the existing HTTP boundaries. Normal
builds omit both `ManagedPublishTargets` and `ValidationGradleInitScript`. Request URLs,
headers, authentication tokens and manifest request codes stay out of exported evidence.
Direct replay retains exact URI/header/settings only in process memory. Java worker
events and managed bridge events use their own elapsed clocks. Actual DNS/TLS cannot be
separated from `HttpURLConnection` connection/header time by these observations; separate
DNS lookups run afterward, without warming the measured request.

### Gate and request measurements

The default production path authenticated depot `2868841`, beta manifest
`5756684179086186957`, downloaded its 226-entry manifest, and decrypted/decompressed/
SHA-1-verified three distinct target chunks. Each succeeded on its first attempt at
HTTPS `cache12-lhr1.steamcontent.com:443`, with identical Host and VHost. No provider,
deadline or retry override was used. The gate passed.

| Resource | Java accepted-to-worker-exit | Managed dispatch-to-response | Received body bytes |
| --- | ---: | ---: | ---: |
| Target manifest | 93 ms | 128 ms | 142,070 |
| `A4F080DE45F7627E468B169EA5F3B7FB15465824` | 24 ms | 76 ms | 308,336 |
| `DB154907CC8804236A9853FC565B01C4A528BFAF` | 34 ms | 64 ms | 290,624 |
| `EFE19B6E9AED3A585F2D87BAC2CF4FB8A88AF95D` | 32 ms | 64 ms | 275,632 |

These are individual observations, not repeated performance trials. Successful compressed
chunk payload totalled 874,592 bytes. The three probe operations were sequential; they
do not establish three-worker utilisation. Java peak concurrency was one and all Java
workers exited. There were no managed cancellation events, HTTP errors or late workers.
SDK request timeout remained 10 seconds, body timeout 60 seconds, Java connect/read
timeouts 120 seconds, bridge dispatcher deadline 180 seconds and polling interval 50 ms.
The separate post-request DNS probes succeeded in 0–1 ms; that does not measure actual
request DNS latency. No direct-Java failure comparison or 60-second deadline experiment
was needed or run because the prior CDN failure did not recur. Its historical cause
remains unresolved; this successful sample does not explain earlier timeouts/DNS errors.

### Confirmed installation blocker

On build `438011`, an actual tap of **Download Selected Version** in an empty sandbox
used the production view, `LauncherDownloadCoordinator` and `LauncherModel`. Only
download-related events were bound. Normal session startup, save sync and gameplay
launch were unbound. A generated `AppPaths` override kept managed mutable paths in the
sandbox and did not write the original branch preference.

The operation authenticated and received the same manifest (HTTP 200, 142,070 bytes;
Java worker 75 ms), then failed with `IOException: Invalid depot file path.` before any
game-content request. The UI displayed **Retry Download**, not a requested Pause.
The failure event recorded `IsReady=false`, `running=false`, `pending=true` and zero
partial content files. The pending manifest survived; no downloaded chunks existed to
retain. Tap-to-failure was 7.416 seconds, including connection, app-info and manifest work.

This is a production regression, not a CDN failure. Captured manifests contain
NUL-terminated names such as `controller_config\0` and
`controller_config\\game_actions_2868840.vdf\0`.
[`DepotChunkTransfer.BuildTargets`](../../src/STS2Mobile/Steam/DepotChunkTransfer.cs)
passes raw names to `DepotDownloadStaging.Resolve` before skipping directory entries.
[`Resolve`](../../src/STS2Mobile/Steam/DepotDownloadStaging.cs) rejects any NUL.
Regular-file names are affected as well. The existing downloader's
[`NormalizeManifestPath`](../../src/STS2Mobile/Steam/DepotDownloader.FileSystem.cs)
handles NUL terminators, but this planner does not use it at that boundary.

The Fold exception stack identifies `Resolve → BuildTargets → RunAsync →
DownloadDepotAsync`. A read-only desktop probe invokes the actual compiled validator
against the captured manifests:

| Manifest | Entries | Rejected NUL-terminated names | Of which regular files |
| --- | ---: | ---: | ---: |
| Public `4733892780027453168` | 220 | 186 | 184 |
| Beta `5756684179086186957` | 226 | 198 | 196 |

The live captured-content fixture also failed before content transfer. Its source-file
selection calls the same production path validator while examining captured names.
It was not altered to bypass the blocker. No production fix or manifest sanitisation
was applied to make acceptance pass.

### Scenario dispositions and remaining prerequisites

| Scenario | Disposition | Evidence / exact remaining prerequisite |
| --- | --- | --- |
| Default-provider target manifest and three verified chunks | **Passed** | `cdn-campaign/diagnosis.json`, SDK/Java/managed request events; default deadlines and provider. |
| Historical CDN failure localisation | **Unavailable: not reproduced** | Current responses succeeded. Need the same timing capture during an actual recurrence to distinguish DNS, connection, body and post-cancellation work. Actual DNS/TLS subphase timing remains unavailable. |
| Fresh download through actual UI/model/coordinator | **Failed** | `live-fresh/result.json`, `ui-events.jsonl`, HTTP events and private exception stack; NUL-name rejection before content. |
| Pause after 32 MiB, drain, resume another 32 MiB, force-stop/relaunch/complete | **Blocked** | Fresh download cannot reach payload. Requires separately fixed manifest-path handling and a repeat from an empty sandbox. |
| Verified CDN IDs not fetched again; one large archive uses three logical workers | **Blocked** | No content workers started in the full install. Prior offline three-worker/zero-refetch results remain separate. Requires the preceding live interruption run. |
| Unchanged installation and prepared-PCK retention | **Blocked** | No completed test-owned installation was produced; the same full-manifest planning defect applies. Requires a completed sandbox donor and unchanged authenticated target. |
| Accessible changed same-branch release | **Unavailable** | Current beta target equals the captured installed beta manifest. The captured public manifest is a different branch and is not a same-branch update. Requires an accessible different beta manifest. |
| Captured layout/moved-range fixture over CDN | **Failed before transfer** | `live-layout-fixture/result.json`; same path-validator dependency during fixture source selection. Requires resolving that dependency without changing production behaviour solely for the test. |
| Corrupt copied PCK, raw verification, preparation, GameIdentity/runtime readiness | **Blocked** | Requires full-manifest download/reconstruction to run and a completed test-owned donor. No original PCK was corrupted. |
| Three paired baseline/candidate fresh and delta trials | **Blocked; not run** | The intended captured-content fixture fails before delivery, so a valid comparable workload was not established. The pinned baseline checkout was verified but no baseline APK or timing pair was run. Requires a working unchanged test workload after the blocking defect is addressed separately. |

There is no elapsed-time improvement or percentage claim. Full live acceptance remains
incomplete. The normal APK intentionally retains the tested production implementation,
including the documented blocker; production fixes are separate work.

### Reproduction and evidence

Campaign evidence is under ignored `tmp/cdn-validation-20261113/`; prior evidence under
`tmp/chunk-download-work/` is unchanged. Private backups, connectivity dumps and raw
logcat remain private. Exported JSONL contains only sanitised request information.

```powershell
./tools/DownloadValidation/test-campaign-tooling.ps1
./tools/DownloadValidation/prepare-java.ps1 -OutputDirectory "$PWD/tmp/cdn-validation-20261113/java"
# Add both explicit inputs to build-android-local.ps1 for a validation APK only:
# -ManagedPublishTargets "$PWD/tools/DownloadValidation/DownloadValidation.targets"
# -ValidationGradleInitScript "$PWD/tools/DownloadValidation/validation-java.init.gradle"
./tools/DownloadValidation/run-device.ps1 -Action Start -RunId cdn-campaign -Operation cdn-diagnostics -Branch public-beta -EvidenceDirectory "$PWD/tmp/cdn-validation-20261113/device"
./tools/DownloadValidation/run-device.ps1 -Action Start -RunId live-fresh -Operation sandbox-ui -Scenario fresh -Branch public-beta -EvidenceDirectory "$PWD/tmp/cdn-validation-20261113/device"
# Tap the actual Download control; ui.json supplies its visible bounds.
./tools/DownloadValidation/run-device.ps1 -Action Pull -RunId live-fresh -Name failed -EvidenceDirectory "$PWD/tmp/cdn-validation-20261113/device"
python tools/DownloadValidation/analyse-cdn-evidence.py tmp/cdn-validation-20261113/device/results/completed/cdn-campaign
dotnet run --project tools/DownloadValidation/ManifestPathProbe/ManifestPathProbe.csproj -- "$PWD/tmp/cdn-validation-20261113/device/private/before/snapshot.zip" "$PWD/tmp/cdn-validation-20261113/managed-bin/STS2Mobile.dll"
```

The tooling's snapshot/compare/cleanup commands accept the same explicit
`EvidenceDirectory`, so this campaign does not overwrite earlier preservation results.
The two small analysis tools are read-only. The manifest probe invokes the compiled
validator without creating its illustrative installation root.

### Closeout

Normal APK `0.2.438-downloads-local / 438012` is installed and stopped. The installed
APK was pulled back and matches the verified artifact byte-for-byte:
`592e2c108fd26b760ca7cb21c97036c3edb85f00666a070d0b1ec9b8b4165551`.
Update signing, ARM64 contents and Android crypto checks passed. The normal APK contains
no managed validation harness, sandbox path override or Java diagnostic hooks. The
chunk-transfer and staging IL match the tested APK; SteamKit, WebSocket crypto-patched
assembly and Zstd dependencies also match byte-for-byte.

The first normal packaging attempt retained an unused diagnostic Java class in Gradle's
incremental output. The binary isolation check rejected it before installation. The
generated Gradle build output was cleaned, the normal APK rebuilt, and the expanded
[`verify-packaged-engine.ps1`](verify-packaged-engine.ps1) then passed. It also fails
against the retained rejected artifact. This caught packaging issue is separate from
the live manifest-path blocker.

```powershell
./tools/DownloadValidation/verify-packaged-engine.ps1 -ValidationApk "$PWD/tmp/cdn-validation-20261113/sandbox-438011.apk" -NormalApk "$PWD/tmp/cdn-validation-20261113/normal-438012.apk"
./tools/DownloadValidation/run-device.ps1 -Action Snapshot -Name after -EvidenceDirectory "$PWD/tmp/cdn-validation-20261113/device"
./tools/DownloadValidation/run-device.ps1 -Action Compare -Name after -EvidenceDirectory "$PWD/tmp/cdn-validation-20261113/device"
./tools/DownloadValidation/run-device.ps1 -Action Cleanup -EvidenceDirectory "$PWD/tmp/cdn-validation-20261113/device"
```

For normal startup, Wi-Fi/mobile data were temporarily disabled and **Active default
network: none** was verified before launch. The normal Home/Play Modded/Game version
UI and controller-ready markers were observed. The Home status displayed `Syncing`
while offline; that label does not establish a Steam Cloud transfer. The app was
force-stopped before connectivity was restored. No gameplay was launched and no
Steam Cloud network operation was possible during this smoke check.

The fresh before/after comparison checked all 1,178 protected hashes and found **zero
semantic user-data differences**. Original credential and installed-version hashes,
saves and all four typed user preference keys matched. Raw differences are retained
for exactly the already documented `sts2mobile.xml` runtime keys, AndroidX
`profileInstalled` marker and framework-owned `IDSCount` counter; no wider exception
was introduced and no stale metadata was restored.

The helper removed only the explicitly named test directories and recorded test-owned
response files (none remained), then was uninstalled. The test-only baseline checkout
was clean and removed. Original Wi-Fi, mobile, airplane and private-DNS settings match
their fresh baseline. `final-device-state.json` confirms the helper is absent, external
test evidence is removed and the normal launcher is installed and stopped. Private
backups and exported measurements remain on the development machine.

The final read-only review found a bookkeeping error in the unexecuted fallback: after
a successful first probe and failed later probe, Java would replay the latest chunk
but C# would verify against the first chunk. The tooling now tracks the current attempted
chunk. The managed harness compiles; this failure branch was not exercised on the Fold.
The successful gate evidence is unaffected. Focused generator/build-input checks passed.

The remaining findings are a confirmed manifest-name regression that blocks full live
acceptance and an unreproduced historical CDN problem. The smallest prerequisites are
separately resolved production manifest-path handling for installation/fixture tests,
a different accessible same-branch manifest for a real update, and a timestamped request
capture during a CDN failure recurrence for transport diagnosis. Until those observations
exist, live pause/restart, PCK/readiness and paired performance claims remain incomplete.

## Manifest filename fix and focused Fold verification, 13 November 2026

The NUL-name rejection documented above is fixed. The chunk engine now uses the existing
Steam filename normaliser, interpreting the first NUL as the terminator and converting
separators before validating paths. Staging, selected/cross-version donors, superseded
partials and legacy partials resolve the canonical name **before** appending
`.downloading`. Selected-donor comparisons use the same normalisation. Empty names,
rooted paths, escaping paths and duplicate canonical targets remain rejected.
Stored manifests and `staging-v1` were not rewritten or migrated. Production changes
are limited to four Steam path/transfer files; no public API, SteamKit, networking,
retry/deadline or worker-limit change was made.

Eight new filename regressions reproduced seven failures before the fix; the existing
256 tests and the additional invalid-path cases passed at that point. After the fix,
the complete suite passed **265/265**, including a new test of read-only staging
inspection. Coverage includes padded directories/files, mixed names/separators, retained
suffixes, pause/restart, installed and partial donors, corrupt ranges, unchanged
authoritative prepared PCKs, and canonical-path collisions. The staging-inspector test
rejects corrupt, missing and preallocated-hole ranges and checks that inspection does
not alter bytes. Fixtures construct physical paths independently of the resolver.

The read-only probe against both captured manifests now reports **zero rejected names**:
public `4733892780027453168` has 220 entries and beta `5756684179086186957` has 226.
The earlier rejection counts were 186 and 198. Private snapshots were not added to tests.
The prior save-safety and offline fixture results remain historical evidence; they were
not repeated because save-sync behaviour was unchanged. A fresh read-only code review
reported no material findings in the scoped fix and validation dependencies.

### Live transfer and interruption results

Only SM-F971B `R3GL70DKNKB` was used. Installed `438012` and its signer were rechecked,
then a fresh baseline backed up 731 files and hashed 1,178 protected files. Original
selection was `public-beta`. Test APK `438013` was a compatible signed update. All
transfers used test-owned directories, default provider selection, existing deadlines
and retries, and three Android workers. No Steam Cloud or gameplay flow was bound.

The default manifest/three-distinct-verified-chunk gate passed again. The real Download,
Pause and Resume controls then exercised beta manifest `5756684179086186957`:

| Scenario | Disposition | Recorded evidence |
| --- | --- | --- |
| Empty-root real download enters transfer | **Passed** | 224 regular files, 3,160,619,181 target logical bytes; no filename rejection. |
| Pause after at least 32 MiB compressed payload | **Passed** | Tap at 36,436,880 verified compressed bytes; drained in 84 ms after the tap; 224 partial files retained, pending=true, running=false, ready=false. |
| Resume retains verified content | **Passed** | Read-only inspection found 214 valid ranges / 212 distinct identities / 99,391,114 logical bytes; none of those identities was fetched on either subsequent transfer. |
| Another 32 MiB, then force-stop | **Passed** | Separate transfer ID; trigger at 34,646,944 verified compressed bytes during Downloading with logical requests active; PID absence checked. |
| Relaunch and actual Resume routing | **Passed** | Resume offered without reset. Inspection found 389 valid ranges / 387 distinct identities / 214,110,048 logical bytes, with ready=false. None of those identities was fetched again. |
| Shared concurrency, including one large PCK | **Passed** | Logical peak and maximum observed active requests=3; 2,102 PCK-exclusive requests reached three concurrently. Java-worker peak=3, no managed HTTP cancellations/exceptions, recorded drain/completion endpoints had zero Java workers. |
| Complete file transfer, hashes and Android PCK preparation | **Passed** | Remaining 1,384,429,184 compressed bytes completed; all raw staged files verified before promotion; explicit raw Steam PCK check passed before preparation; preparation reached `android-pck-v2`. |
| Final raw-file/GameIdentity evidence | **Passed** | 223 non-PCK files / 1,011,316,840 bytes matched Steam manifests. Prepared PCK SHA-256 `f63682865a3db9ae97dc9730accf4956009f89e587b7c3627c924ec703fc1756`; saved ready-identity gate accepted the completed identity. |
| Full sandbox runtime readiness | **Blocked: harness native-directory boundary** | Final result reports ready=false; native cache identity check reads the normal app directory rather than the managed sandbox. See below. |
| Captured-content moved-range fixture over real CDN | **Passed** | Reused 14,215,464 logical bytes, requested exactly one missing 281,184-byte compressed chunk, reconstructed 15,264,040 bytes; SHA-1 `BE9B0959AA3221ACA0122F340D33FCBE8169731B`. |

Each downloader invocation has its own trace ID. The three invocations recorded
36,555,440, 35,335,904 and 1,384,429,184 completed verified compressed bytes respectively.
These are payload counters, not total wire traffic. Three requests were in flight at
process death; identity reuse assertions apply to ranges independently verified in
staging, not merely HTTP bodies received or fetches started. Across the captured Java
events, 3,258 chunk workers exited with HTTP 200 and no Java exception. No percentage
speed improvement or comparison with the pre-change downloader is claimed.

### Remaining runtime-readiness limitation

The complete transferred generation reached PCK v2, and its game-file identity was
accepted. Full runtime readiness then rejected the native assembly-cache boundary:
expected sandbox identity `6b0248d04f7a46d8b8111442204689d2e1bce0d64c6b0cad55fb22699baab981`
and pack `public-beta-dfddd26d3faa`, but native code read identity
`382709224ebcccd9db35b78585dc8974948ed70f3cb3d28ccc9f8d250172a5e5`
and pack `public-beta-b71a6d961b6b`.

This is a demonstrated limitation of the sandbox validation setup, not an established
production runtime failure. `RuntimeAssemblyCachePreparer.Prepare` receives `dataDir`,
but the Android bridge passes only branch/identity/pack to Java.
`GodotApp.prepareRuntimePackForLaunch` derives its directory from `getFilesDir()` and
the native selected branch, which still belong to the normal app. It returns on the
identity mismatch **before** calling `AndroidAssemblyBootstrapper.prepare()`.
The managed-only sandbox override cannot provide native-cache isolation.

The guard was not bypassed, native runtime behaviour was not changed, and no success
was substituted. The final run remains recorded as `status=failed, ready=false` despite
the successful transfer/verification stages. Completing full readiness needs a safely
isolated native-cache test context and a repeat of that check; it must not activate the
sandbox runtime in the user's original cache. Separate PCK corruption/retention cases,
real-update acceptance and paired benchmarks were outside the selected focused scope.

### Reproduction, evidence and closeout

New evidence is in ignored `tmp/manifest-filename-fix-20261113/`; both earlier campaigns
are retained. `managed-red.log`, `managed-final.log`, `manifest-paths-fixed.json`,
`resume-integrity.json`, `concurrency-evidence.json`, `transfer-summary.json` and
`device/results/` support the dispositions. The payload trigger script and trigger
records retain actual tap/force-stop thresholds. Per-checkpoint staging JSON includes
the hashes/identities verified from bytes on disk. Private backups, raw logcat and
connectivity observations remain private.

```powershell
dotnet run --project tests/STS2Mobile.GameIdentityTests/STS2Mobile.GameIdentityTests.csproj -c Release
dotnet run --project tools/DownloadValidation/ManifestPathProbe/ManifestPathProbe.csproj -- "$PWD/tmp/manifest-filename-fix-20261113/device/private/before/snapshot.zip" "$PWD/tmp/manifest-filename-fix-20261113/managed-bin/STS2Mobile.dll"
# Add --chunk-index to export content-to-file identities for the PCK concurrency analysis.
./tools/DownloadValidation/prepare-java.ps1 -OutputDirectory "$PWD/tmp/manifest-filename-fix-20261113/java"
$env:STS2_DOWNLOAD_VALIDATION_JAVA_ROOT="$PWD/tmp/manifest-filename-fix-20261113/java"
# Validation APK only: pass ManagedPublishTargets and ValidationGradleInitScript as above.
./tools/DownloadValidation/run-device.ps1 -Action Start -RunId filename-gate -Operation cdn-diagnostics -Branch public-beta -EvidenceDirectory "$PWD/tmp/manifest-filename-fix-20261113/device"
./tools/DownloadValidation/run-device.ps1 -Action Start -RunId filename-live -Operation sandbox-ui -Scenario fresh -Branch public-beta -EvidenceDirectory "$PWD/tmp/manifest-filename-fix-20261113/device"
# Use visible bounds in ui.json for actual Download/Pause/Resume taps.
./tools/DownloadValidation/run-device.ps1 -Action Start -RunId filename-cdn-fixture -Operation fixture -Branch public-beta -EvidenceDirectory "$PWD/tmp/manifest-filename-fix-20261113/device"
./tools/DownloadValidation/verify-packaged-engine.ps1 -ValidationApk "$PWD/tmp/manifest-filename-fix-20261113/validation-438013.apk" -NormalApk "$PWD/tmp/manifest-filename-fix-20261113/normal-438014.apk"
```

Normal `0.2.438-downloads-local / 438014` was built from clean Gradle output without
validation inputs. Signing/update compatibility, ARM64 content and crypto checks passed.
The normal APK contains no managed harness, staging inspector, sandbox override or Java
diagnostic hooks. Chunk-transfer/staging IL matches the tested APK; SteamKit, WebSocket
crypto-patched assembly and Zstd dependencies match byte-for-byte.

Offline startup was observed only after `Active default network: none` was confirmed.
Normal Home/Play Modded/Game version UI and controller-ready markers appeared. Its
`Syncing` label while offline does not establish a Cloud transfer. The app was stopped
before network restoration; no gameplay was launched. The fresh comparison checked
1,178 protected entries and found **zero semantic user-data differences**: credentials,
installed versions, saves and all four typed user preference keys matched. Two raw
differences were retained: the existing APK/cache metadata keys in `sts2mobile.xml` and
AndroidX `profileInstalled`. No new comparison exception was added.

The helper removed only test-owned data and was uninstalled. Original Wi-Fi, mobile,
airplane and private-DNS settings match the baseline. Installed normal APK SHA-256 was
pulled back and matched the verified artifact:
`dd0b5457293cb177cb937038022f06515124c640e990774da49504ba10239b2e`.
The normal launcher is installed and stopped. The filename fix, transfer/resume and CDN
reuse checks passed; full sandbox runtime readiness remains explicitly incomplete for
the native-directory reason above.

## Save-upload batch implementation and Fold campaign — 13 November 2026

The unreleased APK was built from the `codex/save-upload-batch` working tree, based on
`0a32b88789aec0bd5f1fce458696709c76def5bd`. It replaces per-file service uploads
with one exclusive batch containing three fixed thread-pool workers. Cloud RPCs
remain serialized; the normal HTTP transfers can overlap. Each active file retains
its three-minute budget, including best-effort failed-upload notification.

Verification now requires a successful commit and matching Steam-reported SHA-1
and explicitly present raw size. Unusable metadata falls back to the same response's
body URL and verified decompression. Contradictions fail. Missing fields cannot be
interpreted as an empty file, and late completion cannot bypass the file deadline.
On failure, the original error is captured and siblings cancelled before awaiting
failed-upload notification; all managed workers drain before transport teardown.

### Local verification

- **281 managed tests passed**, including 15 transport cases using the production
  transport with fake RPC/HTTP boundaries. These establish request sequencing,
  verification, bounded workers and failure handling, not Steam server behaviour.
- **15/15 save-safety scenarios passed**, including partial batch retry/conflict,
  deferred deletion, gameplay changes and deferred credential refresh.
- **56 Android/JVM tests passed**, no failures/errors/skips. Android lint retains
  **15 errors and 39 warnings**; production Android Java was not changed.
- Scoped formatting, helper compilation, validation generator checks and the
  managed analyzer build passed. Final review found no remaining actionable issues.
- Observed failing regressions before their fixes: unnecessary read-back; raw ZIP
  prefix mishandling; late metadata completion; invented empty contents; and new
  files starting while failed-upload notification was delayed (expected 3 starts,
  observed 5). All subsequently passed.

Reproduce the managed checks from the repository root:

```powershell
dotnet run --project tests/STS2Mobile.GameIdentityTests/STS2Mobile.GameIdentityTests.csproj -c Release -- 'cloud upload'
dotnet run --project tests/STS2Mobile.GameIdentityTests/STS2Mobile.GameIdentityTests.csproj -c Release
./scripts/test-local-gameplay-save-safety.ps1
./tools/DownloadValidation/test-campaign-tooling.ps1
```

### Live Fold observations

Only SM-F971B `R3GL70DKNKB` was used. The SM-F711B and emulator were excluded.
The fresh baseline was normal build **438014**; preservation captured 731 backup
files and hashes for 1,178 protected files. Candidate **438016** passed signing and
update checks before installation. Normal startup, automatic sync and gameplay
were bypassed throughout the validation campaign.

The campaign enumerated and backed up **617 existing Cloud files** before permitting
any writes. Every mutation was restricted to nine exact names under a new
`sts2-launcher-validation/<guid>/` ledger. Six files contained 1 MiB of incompressible
content, two contained 1 MiB of compressible content, and one was empty.

The fresh batch encountered **HTTP 403 on the empty file's PUT**. All eight nonempty
files received successful commit acknowledgements; six metadata checks completed,
while two pending checks were cancelled after the failure. There were **zero
verification-body GETs** in that attempted batch. The primary HTTP error was retained
when the failed-upload notice also failed with `AsyncJobFailedException`.

This does not establish the cause of the 403, nor that it is caused by concurrency.
The batch did not report success. Unchanged/duplicate, replacement, requested
cancellation/retry, gated parallelism and independent owned-file audit scenarios
were **blocked** after this failure. Their fixture results do not replace live results.

The process subsequently aborted during the post-test comparison. Android exit
information records PID `9897`, `SIGNALED`, status `6`, at 15:55:39 UTC. The retained
stack shows an unhandled `AndroidBridgeDispatcher` timeout from SteamKit's scheduled
CM heartbeat through `AndroidJavaCrypto.AesEncryptEcb`. This is a second observed
blocker; its cause and lifecycle conditions were not resolved by this upload change.

Before restarting, private recovery export succeeded: **621 archive entries**, including
all **617 original Cloud file backups**, the baseline inventory and exact owned-file
ledger. Shareable run results and raw private data are stored separately. The interrupted
Java trace contains 669 worker starts and exits, observed peak **two** Java workers,
and zero active workers/zero lost events at the saved pre-cleanup checkpoint. Three-worker
capacity passed fixtures; three simultaneous live HTTP transfers were not established.

A **cleanup-only** run started at 16:03:23 UTC using the original ledger and baseline.
It was still checking existing files when the user needed to remove the Fold. The device
was already disconnected when the stop/normal-APK restoration command was attempted;
its initial availability check failed, so **no restoration command executed**. The cleanup
may continue on-device, but its final outcome has not been observed. Do not report
existing Cloud preservation or test-file deletion as passed, and do not remove the
recovery ledger or helper until they are confirmed.

| Remaining live scenario | Disposition / prerequisite |
| --- | --- |
| Complete fresh nine-file batch | **Failed**: empty PUT returned HTTP 403. Need a sanitized error-body/request-length observation before attributing the cause. |
| Unchanged/duplicate, replacement, requested cancellation/retry, gated parallelism, final owned-file byte audits | **Blocked** by the failed fresh batch; fixtures are separate evidence. |
| Existing Cloud preservation and owned-file removal | **Passed on reconnection**: full byte comparison of 617 original files, then fresh matching metadata; all nine owned paths independently returned `FileNotFound`. No explicit tombstones were returned. See closeout below. |
| Final local preservation, offline normal startup and helper/test-data removal | **Passed on reconnection** on the authorised Fold only. Normal 438021 is installed/stopped, test data/helper removed, and five network settings restored. |

### Normal APK and handoff state

Normal **0.2.438-save-upload-local / 438017** was built from clean Gradle output without
validation inputs. Contents, crypto, signing and update compatibility checks passed.
Its packaged Cloud/chunk engine IL and transfer dependencies match candidate 438016;
managed and Java validation hooks are absent. Engine IL SHA-256:
`91828d7dc13035a9a902674a8253b4bf16b99bf93964bb0cf76e97dfd4997f40`.

The normal APK is `artifacts/android/StS2Launcher-v0.2.438-save-upload-local-arm64-v8a.apk`,
SHA-256 `8ce05b31b2fd573803cb582e7552eaec22902a11d36e1a8adc4b18d36dfcc026`.
At the interrupted handoff, that APK **was not installed**, and the last verified
phone build was validation 438016. Reconnected closeout below supersedes that state:
normal **438021** is now installed and stopped. Normal startup and preservation were
checked separately from packaging; no uninstall or data clearing was performed.

Source baseline, dirty feature-branch state and production source hashes accompany the
normal APK metadata. The campaign did not push to GitHub or publish a release. Raw logcat,
existing Cloud bytes and remote filenames remain in ignored private evidence.

### Recovery and evidence commands

The new helper cleanup gate refuses to remove recovery data after Cloud writes
until both `ownedCleanupVerified` and `existingCloudUnchanged` are true. Private
Cloud backup export is separate from ordinary shareable run results and includes
known atomic-write temporary files for interruption recovery.

```powershell
$evidence = "$PWD/tmp/save-upload-batch-20261113/device"
./tools/DownloadValidation/run-device.ps1 -Action Status -RunId cloud-batch-live -EvidenceDirectory $evidence
./tools/DownloadValidation/run-device.ps1 -Action Pull -RunId cloud-batch-live -Name final -EvidenceDirectory $evidence
# After the campaign finishes; this stops the validation process and exports private backups.
./tools/DownloadValidation/run-device.ps1 -Action CloudBackup -RunId cloud-batch-live -EvidenceDirectory $evidence
# Only if owned Cloud cleanup is incomplete; retains the original ledger and baseline.
./tools/DownloadValidation/run-device.ps1 -Action Start -RunId cloud-batch-live -Operation cloud-upload-cleanup -EvidenceDirectory $evidence
```

Host evidence is under `tmp/save-upload-batch-20261113/`. Existing game downloads,
Pull/conflict policy, local scan/hash caching, Android bridge cancellation and public
release publishing remain outside this change. No percentage speed claim is made.


### Reconnected Cloud cleanup and normal-APK closeout

On reconnection, no completed cleanup result was available. A resumed full read/hash
comparison finished with **all 617 existing Cloud files unchanged**. The initial
production delete verifier then failed because an acknowledged `ClientDeleteFile`
was followed by `ClientFileDownload: FileNotFound`, rather than an explicit tombstone.
A repeated delete of that already removed test path returned `DuplicateRequest`.
These are additional observed protocol responses; production deletion behaviour was
not changed or declared passing.

The validation-only cleanup now inspects exact ledger-owned paths first. Already
absent paths require no delete; present paths are deleted and independently checked.
Only same-app explicit tombstones or the metadata RPC's exact `FileNotFound` response
establish absence. A repeated-delete response is accepted only after that independent
check. Unowned paths, wrong-app/present metadata, failed deletes and access errors
cannot be reported as clean.

A completed full-byte comparison can be reused for interrupted cleanup only when its
saved before/after records agree and a new complete remote listing matches every
original path, usable hash, raw size and timestamp. New/missing/unrelated files or
unusable hashes force a new full byte comparison. The report labels this mode
`previous-full-byte-comparison-plus-current-metadata`; it does not claim another full
content download. Exact owned test paths are the only exclusions.

Two focused regressions were observed failing and then passing for absence checking
and checkpoint reuse; idempotent cleanup cases were also exercised. The complete
managed suite now passes **283 tests**. Original production fingerprints are unchanged,
so the earlier **15 save-safety scenarios**, **56 JVM tests**, and unchanged **15-error /
39-warning** lint record remain applicable. Review found no remaining mutation-safety
issue in the validation changes. Fixtures remain separate from the live observations.

Final cleanup-only run `c671423f6ce546028784b6163bee1901` ran from **18:12:14.543 to
18:12:38.940 UTC** (24.397 seconds). It confirmed the completed preservation comparison
against fresh metadata, sent seven deletes for the remaining present test files,
and verified **all nine test paths absent**. The other two paths were already absent.
All nine final absence results were `FileNotFound`; zero explicit tombstones were
reported. The zero-byte PUT 403 and the production delete-verifier limitation therefore
remain failed live cases, rather than being hidden by test-tool cleanup.

Final normal APK **0.2.438-save-upload-closeout2 / 438021** was built from clean Gradle
output without validation inputs, passed contents/crypto/signing and update checks
against validation **438020**, and was installed in place. Its packaged production
Cloud/chunk engine IL and SteamKit/WebSocket/Zstd dependencies match the candidate;
all managed/Java validation hooks and closeout helper types are absent. SHA-256:
`af373019db225774667701730c85b7d20012b9ad748f09ec8d817ae725a16eec`.
The engine IL SHA-256 remains
`91828d7dc13035a9a902674a8253b4bf16b99bf93964bb0cf76e97dfd4997f40`.

With **Active default network: none**, the normal APK rendered Home and its Play Modded
control, preserved public-beta selection and four enabled mods, and logged controller
startup completion at 477 ms. No game was launched. The app was stopped before connectivity returned.

Final local comparison covered **1,178 protected entries** with **zero semantic user-data
differences**. Saves, encrypted credentials, installed versions and four typed user
preferences matched. The two retained raw differences were `sts2mobile.xml` runtime
metadata and `profileInstalled`; no other framework preference keys changed.
Guarded cleanup then removed only the test-owned directories and helper. All five
captured network settings match their reconnected baseline. Final package inspection
confirmed **normal 438021 installed, stopped=true**. Existing Cloud backups, private
before/after snapshots and raw differences remain on the host under ignored private
evidence. No public release was published.

Reproduction/evidence for this closeout is in `cleanup-red.log`,
`cleanup-idempotent-red.log`, `cleanup-green.log`, `closeout-managed.log`,
`closeout-package-audit.json`, `device/results/final-cloud/`,
`device/private/cloud-preserved-final.zip`, `final-local-compare.log`,
`final-helper-cleanup.log` and `final-device-closeout.json` under
`tmp/save-upload-batch-20261113/`.

## Follow-up: empty Cloud PUT and stopped-frame crypto timeout

Evidence is under ignored `tmp/cloud-failure-investigation-20261113/`. Source HEAD was
`ce46ced8c629cf7e52121e4e41deea0b5178ed76` on `codex/save-upload-closeout`, with the
previous closeout tooling/documentation still uncommitted. This investigation changes
test tooling only. Production upload, HTTP, crypto, dispatcher and connection code are
unchanged. Only `SM-F971B / R3GL70DKNKB` was used; the other Samsung was excluded.
Installed baseline was normal **438021**. Fresh local preservation captured 731 backup
files and 1,178 protected hashes before installing diagnostics.

### Empty-file HTTP 403: cause still inconclusive

The original failed request was Java request **627**, to the Azure Cloud upload host.
It received HTTP 403 after 185 ms, read **876 error-body bytes**, and exited at 187 ms.
The managed call reported failure at 249 ms. Requests 619 and 622 had already received
201 from the same Azure host; the other six nonempty uploads used AWS/Google hosts and
succeeded. These observations do not establish a general account or Azure outage, nor
whether the empty-file failure depends on concurrency. The old evidence retained the
error-body length, but not its content or service error code.

One plausible source-level explanation was tested and rejected for the tested wire
path: `performHttpRequest` skips supplied `Content-Length` and only opens an output
stream for nonempty data. A standalone dex probe on the Fold extracted that exact
production method, used an Android loopback HTTP server to inspect requests, and
compared empty/one-byte PUTs with a diagnostic variant that also opens an empty output
stream. **All four requests had the correct Content-Length and body bytes**: 0 or 1.
Android supplied the zero length automatically. Both empty variants were identical
for those fields. This is an Android HTTP/1.1 loopback test under shell UID, not a
successful authenticated Azure/TLS upload; it does not prove every signed header is
correct on the live path. Production code was not altered to adopt the variant.

The new `cloud-empty-investigation` operation attempts a one-byte control, then an
empty file **sequentially through the production batch implementation**. An observer
retains any error XML privately and exports only the service error code, field names
and sizes. The existing full-backup and exact owned-ledger gates still precede writes.

Live attempt `71000b6e6fe94e92aecbe0d7dc0ce327` on validation **438022** ran
19:03:58.758–19:08:16.920 UTC. Enumeration returned 617 files, but the first backup GET
to `steamcloud-london.storage.googleapis.com` returned no headers/body before managed
cancellation at 120,034 ms. Its Java worker remained active after the additional
130-second drain allowance; zero telemetry events were lost. The process was stopped
only after the failed report and raw evidence were retained. **No begin-upload,
commit, PUT or delete occurred.** The one-byte and empty-file probes are therefore
**blocked**, not failed upload attempts. The new error-body capture path was compiled
but not exercised. The incomplete backup/ledger was exported privately.

Smallest remaining evidence: a completed read-only preservation backup, followed by
that sequential control/empty test's sanitized service error code and canonical
request/header comparison. Until that exists, neither an empty-file handling fix nor
a concurrency/signature diagnosis is justified. The new cancellation/drain failure
is separate evidence; it does not explain the earlier 403.

### Bridge timeout: stopped-frame crypto failure reproduced

The original process 9897 logged activity pause at **15:52:39.181**, stop at
**15:52:39.790**, then an unhandled bridge timeout at **15:55:39.865**: **180.075 s after
stop**. Its stack is specifically SteamKit's CM heartbeat → encrypted TCP send →
`AndroidJavaCrypto.AesEncryptEcb` → `AndroidBridgeDispatcher.Run/Wait`.

The source and packaged dependencies establish the mechanism:

- Android Steam connections use TCP. The patched encryption helper dispatches Java
  crypto through `AndroidBridgeDispatcher`, waits synchronously, and has a fixed
  180,000 ms deadline.
- That queue is serviced by Godot process-frame callbacks on the registered engine
  thread. The packaged Godot 4.5.1 library's `GodotVulkanRenderView.onActivityStopped`
  calls `pauseRenderThread`; restart resumes it. `GodotApp.onPause/onStop` does not
  cancel queued bridge work or establish an exception boundary around heartbeats.
- The packaged SteamKit `ScheduledFunction.Tick` directly invokes its action, with
  **zero exception handlers**. Its CM heartbeat callback also has no exception handler.
  A dispatcher timeout escaping that callback can terminate the process.

A managed fixture links the **unchanged dispatcher source** and supplies only an
Android-platform seam. A serviced request completed; an unserviced timer request
threw an unhandled timeout at **180,021 ms** without executing its queued action. This
establishes queue/callback behaviour, not Android crypto speed.

Two read-only Fold observations enumerated Cloud metadata without permitting writes.
The short background/resume control kept the same PID but dropped its Steam connection
after about five seconds. A fresh uninterrupted background observation also dropped
the connection and did not reproduce the heartbeat fatal timeout in the observed
window. Thus backgrounding alone is not a sufficient reproduction: an in-flight
callback depending on the stopped engine thread matters. These are not successful
upload/cancellation acceptance tests.

An offline `bridge-crypto` fixture isolated that boundary on the Fold. It first checked
a known AES block on a worker, then armed a diagnostic timer using the same production
crypto/dispatcher calls and the same unhandled-callback behaviour. It used no Steam
session, saved credentials, Cloud operations or production lifecycle changes.

On validation **438024**, process **29129** verified the foreground worker's AES output
at **19:24:41.224 UTC**. It armed the timer at 19:24:41.249, the activity stopped at
19:24:41.791, and the timer entered encryption at **19:24:51.253**. No crypto-completed
event followed. Android subsequently froze the background process; `dumpsys activity
processes` recorded **isFrozen=true** before resume. At **19:28:25.373** the activity
resumed; at **19:28:25.400** the worker logged the same unhandled dispatcher timeout,
**27 ms after resume**. The stack includes the production
`AndroidJavaCrypto.AesEncryptEcb` and dispatcher, then the diagnostic timer callback.
Android exit information records **SIGNALED / status 6** at 19:28:25.467.

**Verdict: confirmed lifecycle/bridge failure mechanism, high confidence.** A crypto
call can depend on a stopped Godot frame thread, outlive its 180-second deadline and
escape an unhandled timer callback. OS process freezing can defer when the fatal
exception surfaces; this device fixture is not a measurement of a 180-second AES
operation. The real SteamKit heartbeat crash remains the original separate observation;
the offline timer fixture establishes the shared failure mechanism, not the frequency
or every live connection condition. No production fix was implemented.

### Final device closeout

Normal **438025 / 0.2.438-cloud-investigation-closeout2** was built from clean Gradle
output, verified for signing/update compatibility, and installed without uninstalling
or clearing launcher data. Its SHA-256 is
`a6d0ee10a4766fa524a807100253e82a11a659604ff541b2c543f7bec03c5dfe`.
The packaged Cloud/chunk engine IL matches validation 438024 and the earlier tested
engine (`91828d7dc13035a9a902674a8253b4bf16b99bf93964bb0cf76e97dfd4997f40`);
SteamKit/WebSocket/Zstd dependencies match. Managed/Java test hooks, including the
offline timer fixture, are absent from normal packaging.

With **Active default network: none**, normal startup rendered Home, retained
public-beta and four enabled mods, and logged controller-ready at 480 ms. No gameplay
was launched. The normal UI was stopped before connectivity returned. Final comparison
again covered **1,178 protected entries with zero semantic user-data differences**:
saves, credentials, installed versions and four typed user preferences matched. The
two raw runtime-metadata differences are retained privately. Guarded helper cleanup
removed only its owned directories; the helper, loopback dex and device screenshot
were removed. Instrumentation had cleared Android's stopped flag even though no normal
process remained; a final explicit force-stop restored it. Final inspection confirms
**438025, stopped=true, no launcher process**, with all five network settings restored.

This campaign sent **zero Cloud mutation requests**. Its failed backup cannot establish
a fresh full Cloud byte comparison, so the earlier successful 617-file preservation
comparison remains separate historical evidence. Empty-file upload acceptance is still
blocked. No GitHub release was published.

### Reproducible checks and retained evidence

```powershell
python tmp/cloud-failure-investigation-20261113/probe-empty-http.py
# javac/d8 compile the generated EmptyHttpProbe.java; push only its dex to
# /data/local/tmp/sts2-empty-http-probe.dex on R3GL70DKNKB, then:
adb -s R3GL70DKNKB shell 'CLASSPATH=/data/local/tmp/sts2-empty-http-probe.dex app_process /system/bin EmptyHttpProbe'
dotnet run --project tmp/cloud-failure-investigation-20261113/bridge-fixture/BridgeFixture.csproj -c Release -- pumped
dotnet run --project tmp/cloud-failure-investigation-20261113/bridge-fixture/BridgeFixture.csproj -c Release -- unserviced-timer
python tmp/cloud-failure-investigation-20261113/analyse.py
dotnet run --project tests/STS2Mobile.GameIdentityTests/STS2Mobile.GameIdentityTests.csproj -c Release
./tools/DownloadValidation/test-campaign-tooling.ps1
```

The device operations use the normal signed validation-build procedure above with
strictly increasing version codes. Explicit harness operations are
`-Operation cloud-empty-investigation`, `-Operation bridge-lifecycle` and
`-Operation bridge-crypto`. The crypto fixture is deliberately fatal when its queued
call cannot complete: capture its raw events/logcat and Android exit information,
rather than changing the interrupted outer `result.json` to a successful result.

The managed suite passed **283/283**. Generator guards passed; validation and normal APK
builds passed. The later harness compile passed with 85 warnings/zero errors. Raw
fixture, Java-worker, lifecycle, backup failure and package-audit records are retained
alongside `analysis.json`, `source.json`, `android-loopback-results.json`,
`steamkit-heartbeat-il.txt` and the packaged Godot `javap` output. Private snapshots,
APK copies and Cloud ledger exports remain separate from shareable run results.
`device/results/crypto-reproduced/`, `device/private/crypto-exit-info.txt`,
`final-package-audit.json`, `local-compare.log`, `final-device-closeout.json` and
`device/installed-final-stopped.txt` establish the final reproduction and closeout.
