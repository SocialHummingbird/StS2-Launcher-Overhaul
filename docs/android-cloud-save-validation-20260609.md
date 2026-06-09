# Android Cloud Save Validation - 2026-06-09

## Scope

ARM64 phone validation for manual Steam Cloud Push/Pull hardening after the Pull baseline.

Validated package used the local test package:

```text
package=com.sts2launcher.overhaul.fork.codexlogin
abi=arm64-v8a
```

## Key evidence

Push initially reproduced the post-Push process death:

- `artifacts/android/push-live-observation-20260609-1`
- `artifacts/android/push-flush-observation-20260609-2`
- `artifacts/android/push-pumpfix-observation-20260609-1`

The failure moved from after `Push: complete` to the first cloud-upload boundary. The decisive crash evidence showed a native Android crypto segfault:

```text
[Cloud] Uploading 105 files synchronously for manual cloud push...
libSystem.Security.Cryptography.Native.Android.so
CryptoNative_EvpDigestOneShot
Process exited due to signal 11 (Segmentation fault)
```

The fix replaced `SHA1.HashData(rawBytes)` in the cloud upload file-hash path with a managed SHA-1 implementation, avoiding the crashing Android native crypto call while preserving the Steam Cloud SHA-1 file hash.

## Validated Push result

Evidence folder:

- `artifacts/android/push-managedsha1-observation-20260609-1`
- Preferred local summary: `artifacts/android/push-managedsha1-observation-20260609-1/summary-scrubbed.md`
- Current-build manual Push evidence: `artifacts/android/push-clean3-observation-20260609-1`
- Current-build local summary: `artifacts/android/push-clean3-observation-20260609-1/summary-scrubbed.md`

Observed result:

```text
[Cloud] Uploading 105 files synchronously for manual cloud push...
[Cloud] Uploaded 105 files synchronously for manual cloud push.
[Cloud] Push: complete: 105 files queued and flushed
[Connection] Idle timeout, disconnecting
```

No app process death, `WINDOW DIED`, `signal 6`, or `signal 11` followed the Push completion. The app remained focused and running after the capture.

The validated capture used the then-current completion wording, `queued and flushed`. Current source now reports the same manual Push path as uploaded/flushed and updates the launcher status to "Push complete. Steam Cloud now reflects Android local saves."

The current `0.2.0-codexcloudfix-clean3` local build was then reinstalled on the connected ARM64 phone and manually confirmed through the launcher UI. It reached:

```text
[Cloud] Push: starting (162 files)
[Cloud] Uploading 105 files synchronously for manual cloud push...
[Cloud] Uploaded 105 files synchronously for manual cloud push.
[Cloud] Push: complete: 105 files uploaded and flushed
[Connection] Idle timeout, disconnecting
```

The process remained alive after Push completion and after the later Steam idle timeout, with no app process death markers in the capture.

## Validated Pull-after-Push result

Evidence folder:

- `artifacts/android/pull-after-push-observation-20260609-1`
- Preferred local summary: `artifacts/android/pull-after-push-observation-20260609-1/summary-scrubbed.md`

Observed result:

```text
[Cloud] Auto validation pull system property detected; starting manual Pull path.
[Cloud] Pull: starting (162 files)
[Cloud] Pull: complete: 105 downloaded, 57 not in cloud
[Connection] Idle timeout, disconnecting
```

The capture includes `105` `Pull: wrote ...` local-write lines and no app crash markers. The app remained focused and running after the capture.

## Code changes validated

- Manual Push no longer hands its batch to the background write queue and then waits for a flush; it uploads the collected batch directly before reporting completion.
- Upload file hashing now uses managed SHA-1 instead of Android native crypto.
- Push completion wording was updated to describe uploaded/flushed behavior.
- Launcher Push/Pull status text now names the data direction explicitly, and Push confirmation tells testers to Pull first and verify Android local saves exist before pushing.
- Fallback save discovery now skips runtime/cache directories (`.godot`, `cache`, `game`, `tmp`) to reduce noisy cloud-sync diagnostics.
- Scrubbed local evidence summaries were created inside the ignored artifact folders so reviewers do not need to inspect raw device logcat with unrelated phone telemetry.
- Temporary ADB property triggers used to work around unreliable Godot touch injection were removed after validation and are not part of the clean installed build.

## Remaining cloud-save release gaps

- Publish and verify a release asset that includes the managed SHA-1/manual Push hardening.
- Re-run manual UI confirmation/cancel smoke on the clean release-facing build, including the latest Pull-first/verify-local-saves Push warning text, even though the underlying Push/Pull code path and local clean3 manual Push confirmation are now validated.
- Keep overwrite risk prominent: confirmed Push can overwrite Steam Cloud state.
- Continue reducing noisy diagnostics so cloud-save success/failure markers are easier to find.
