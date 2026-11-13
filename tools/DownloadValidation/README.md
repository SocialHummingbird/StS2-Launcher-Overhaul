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
