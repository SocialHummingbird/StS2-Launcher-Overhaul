using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed class DownloadValidationRequest
{
    public string RunId { get; set; }
    public string Operation { get; set; }
    public string Branch { get; set; }
    public string Provider { get; set; }
    public long CancelAfterBytes { get; set; }
    public string SourceRunId { get; set; }
    public string Scenario { get; set; }
}

internal static class DownloadValidationRunner
{
    internal static bool Enabled => true;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, IncludeFields = true };

    internal static bool TryStart(LauncherModel model, LauncherView view, Action<Action> mainThread)
    {
        // This entire source file and both call sites exist only in an explicitly injected validation build.
        // Always intercept startup, including a missing/invalid request: never start session/cloud/game flows.
        view.SetStatus("Download validation build; normal launcher actions are disabled.", LauncherStatusSeverity.Information);
        _ = Task.Run(async () =>
        {
            try
            {
                var requestPath = Path.Combine(model.DataDir, "download-validation", "request.json");
                if (!File.Exists(requestPath)) return;
                var request = JsonSerializer.Deserialize<DownloadValidationRequest>(File.ReadAllText(requestPath));
                if (request == null || !Regex.IsMatch(request.RunId ?? "", "^[a-zA-Z0-9][a-zA-Z0-9-]{0,79}$"))
                    throw new InvalidDataException("Invalid private validation request.");
                if (request.Operation == "sandbox-ui")
                    await SandboxDownloadValidation.StartAsync(model.DataDir, view, mainThread, request).ConfigureAwait(false);
                else
                    await model.RunDownloadValidationAsync(request, message => mainThread(() => view.SetStatus(message, LauncherStatusSeverity.Working))).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                // An unexpected entry failure is reported by type only; session details never enter test output.
                PatchHelper.Log("[DownloadValidation] entry failure: " + error.GetType().Name);
                mainThread(() => view.SetStatus("Download validation entry failed: " + error.GetType().Name, LauncherStatusSeverity.Error));
            }
        });
        return true;
    }

    internal static void Write(string path, object value)
    {
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(value, JsonOptions));
        File.Move(temporary, path, overwrite: true);
    }

    internal static string OwnedPath(string root, string relative)
    {
        var bound = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(bound, StringComparison.Ordinal)) throw new IOException("Validation path escaped its owned root.");
        return path;
    }

    internal static string SafeError(Exception error)
        => error.GetType().Name + ": " + SafeText(error.Message);

    internal static string SafeText(string text)
    {
        var message = Regex.Replace(text ?? "", @"https?://[^\s\)\]\}""']+", match =>
        {
            return Uri.TryCreate(match.Value, UriKind.Absolute, out var uri) ? uri.GetLeftPart(UriPartial.Authority) + "/<path-query-redacted>" : "<url-redacted>";
        });
        message = Regex.Replace(message, @"\b[A-Za-z0-9_-]{40,}\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b", "<token-redacted>");
        return message;
    }
}

internal partial class LauncherModel
{
    internal async Task RunDownloadValidationAsync(DownloadValidationRequest request, Action<string> status)
    {
        var validationRoot = Path.Combine(_dataDir, "download-validation");
        var run = DownloadValidationRunner.OwnedPath(validationRoot, request.RunId);
        Directory.CreateDirectory(run);
        var sandbox = Path.Combine(run, "data");
        Directory.CreateDirectory(sandbox);
        var output = DownloadValidationRunner.OwnedPath(
            Path.Combine(AndroidGodotAppBridge.GetExternalFilesDirPath(), "download-validation-evidence", "runs"), request.RunId);
        Directory.CreateDirectory(output);
        var branch = SteamGameBranch.Normalize(request.Branch);
        var started = DateTimeOffset.UtcNow;
        var attemptId = Guid.NewGuid().ToString("N");
        var outputLock = new object();
        Action<DownloadValidationHttpEvent> httpObserver = entry =>
        {
            lock (outputLock) File.AppendAllText(Path.Combine(output, "http-events.jsonl"), JsonSerializer.Serialize(new { attemptId, entry }) + "\n");
        };
        var result = new Dictionary<string, object>
        {
            ["runId"] = request.RunId,
            ["attemptId"] = attemptId,
            ["operation"] = request.Operation,
            ["branch"] = branch,
            ["startedUtc"] = started,
            ["testOwnedRoot"] = sandbox,
            ["steamCloudOperations"] = 0,
            ["credentialsExported"] = false,
            ["status"] = "running"
        };
        result["testProvider"] = request.Provider ?? "default";
        void SaveResult() { lock (outputLock) DownloadValidationRunner.Write(Path.Combine(output, "result.json"), result); }
        void Log(string message)
        {
            lock (outputLock) File.AppendAllText(Path.Combine(output, "validation.log"), DateTimeOffset.UtcNow.ToString("O") + " " + DownloadValidationRunner.SafeText(message) + "\n");
        }
        SaveResult();
        if (request.Operation == "hold") { result["status"] = "held"; SaveResult(); return; }
        AndroidJavaHttpMessageHandler.ValidationHttpChanged += httpObserver;
        using var cancel = new CancellationTokenSource();
        try
        {
            if (request.Operation == "cdn-diagnostics")
                ValidationJava.Call("configureDownloadValidationHttp", request.RunId);
            if (request.Operation == "local-fixture")
            {
                result["fixture"] = "Local verified installed Steam content; three delayed local fetch workers, no Steam session or network calls. Raw-file promotion only, not full game readiness.";
                result["networkPayloadBytes"] = 0;
                result["localResult"] = await LocalChunkValidation.RunAsync(_dataDir, branch, run, output, attemptId, request.CancelAfterBytes, cancel, status).ConfigureAwait(false);
                result["status"] = "passed";
                status("Local Android fixture passed: " + request.RunId);
                return;
            }
            if (request.Operation == "copied-update" && !File.Exists(Path.Combine(run, "source-copied")))
            {
                status("Copying the selected installed version into the test sandbox…");
                foreach (var relative in new[] { "game", "download_state", "installation_state.json" })
                {
                    var source = Path.Combine(SteamGameInstallPaths.VersionSlotDirectory(_dataDir, branch), relative);
                    var target = Path.Combine(SteamGameInstallPaths.VersionSlotDirectory(sandbox, branch), relative);
                    CopyValidationSource(source, target);
                }
                File.WriteAllText(Path.Combine(run, "source-copied"), "Copied only; original version remains read-only.");
            }
            // The normal credential reader removes a stale ciphertext file if decryption fails.
            // Copy ciphertext only inside this app's private sandbox so that failure cannot delete
            // the real credential file. No credential bytes are written to external evidence.
            File.Copy(Path.Combine(_dataDir, "steam_credentials.enc"), Path.Combine(sandbox, "steam_credentials.enc"), overwrite: true);
            using var validationModel = new LauncherModel(sandbox);
            // Deliberately bypass StartSession: it configures automatic save sync. Use the normal
            // Keystore reader and the existing on-demand session path on the isolated model instead.
            validationModel._credentialStore.Load();
            if (!validationModel._credentialStore.HasUsableCredentials()) throw new InvalidOperationException("No usable saved credentials.");
            status("Connecting the validation Steam session…");
            var connection = await validationModel.GetDepotConnectionAsync().ConfigureAwait(false);
            if (connection == null) throw new InvalidOperationException("Saved Steam session unavailable.");
            using var downloader = new DepotDownloader(connection, sandbox, branch);
            downloader.ValidationAttemptId = attemptId;
            downloader.ValidationProvider = request.Provider ?? "default";
            downloader.LogMessage += Log;
            if (request.Operation == "cdn-diagnostics")
            {
                var diagnosis = await downloader.RunCdnInvestigationAsync(_dataDir, branch, output, cancel.Token).ConfigureAwait(false);
                result["diagnosis"] = diagnosis;
                result["status"] = "completed-diagnostic";
                return;
            }
            var depotProgress = new Dictionary<uint, DepotChunkTransferProgress>();
            downloader.ValidationChunkChanged += chunk =>
            {
                lock (outputLock)
                {
                    File.AppendAllText(Path.Combine(output, "chunk-events.jsonl"), JsonSerializer.Serialize(chunk) + "\n");
                    DownloadValidationRunner.Write(Path.Combine(output, "chunk-summary.json"), chunk);
                }
            };
            downloader.ValidationRawProgressChanged += (depotId, progress) =>
            {
                lock (outputLock)
                {
                    depotProgress[depotId] = progress;
                    DownloadValidationRunner.Write(Path.Combine(output, "depot-progress.json"), depotProgress);
                    File.AppendAllText(Path.Combine(output, "raw-progress.jsonl"), JsonSerializer.Serialize(new { attemptId, utc = DateTimeOffset.UtcNow, depotId, progress }) + "\n");
                }
                if (request.CancelAfterBytes > 0 && progress.DownloadedBytes >= request.CancelAfterBytes) cancel.Cancel();
            };
            if (request.Operation == "fixture")
            {
                result["fixture"] = "Synthetic renamed/reordered layout from a captured installed Steam manifest, with one corrupted copied donor range; real CDN chunks.";
                var fixture = await downloader.RunValidationFixtureAsync(_dataDir, branch, run, progress =>
                {
                    lock (outputLock) DownloadValidationRunner.Write(Path.Combine(output, "transfer.json"), progress);
                    status($"Fixture: reused {progress.ReusedBytes} bytes, downloaded {progress.DownloadedBytes} bytes");
                    if (request.CancelAfterBytes > 0 && progress.DownloadedBytes >= request.CancelAfterBytes) cancel.Cancel();
                }, cancel.Token).ConfigureAwait(false);
                result["fixtureResult"] = fixture;
            }
            else if (request.Operation == "download" || request.Operation == "copied-update")
            {
                downloader.ProgressChanged += progress =>
                {
                    progress.ApplyTo((percent, message) =>
                    {
                        lock (outputLock) DownloadValidationRunner.Write(Path.Combine(output, "progress.json"), new { percent, message, updatedUtc = DateTimeOffset.UtcNow });
                        status(message);
                    });
                };
                var completion = await downloader.DownloadAsync(cancel.Token).ConfigureAwait(false);
                var state = BranchInstallStateStore.Current.Read(sandbox, branch);
                var identity = GameIdentityReader.ReadInstalledReadOnly(sandbox, branch);
                if (!state.IsReady || !state.MatchesReadyIdentity(identity)) throw new IOException("Completed sandbox identity is not ready.");
                result["ready"] = state.IsReady;
                result["identity"] = identity.Id;
                result["pckPreparation"] = state.PckPreparationVersion;
                result["transactionId"] = completion.TransactionId;
                result["rawManifestVerification"] = VerifyValidationFiles(sandbox, branch);
            }
            else throw new InvalidDataException("Unsupported validation operation.");
            result["status"] = "passed";
            status("Download validation passed: " + request.RunId);
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
            result["status"] = "cancelled";
            result["partialFiles"] = Directory.EnumerateFiles(run, "*.downloading", SearchOption.AllDirectories).Count();
            var statePath = BranchInstallStateStore.PathFor(sandbox, branch);
            if (File.Exists(statePath)) result["installationReady"] = BranchInstallStateStore.Current.Read(sandbox, branch).IsReady;
            status("Download validation paused: " + request.RunId);
        }
        catch (Exception error)
        {
            result["status"] = "failed";
            result["errorType"] = error.GetType().FullName;
            result["error"] = DownloadValidationRunner.SafeError(error);
            result["innerError"] = error.InnerException == null ? null : DownloadValidationRunner.SafeError(error.InnerException);
            Log("Failure: " + DownloadValidationRunner.SafeError(error));
            status("Download validation failed: " + error.GetType().Name);
        }
        finally
        {
            AndroidJavaHttpMessageHandler.ValidationHttpChanged -= httpObserver;
            result["finishedUtc"] = DateTimeOffset.UtcNow;
            result["elapsedSeconds"] = (DateTimeOffset.UtcNow - started).TotalSeconds;
            SaveResult();
        }
    }

    internal static void CopyValidationSource(string source, string destination)
    {
        if (Directory.Exists(source))
        {
            Directory.CreateDirectory(destination);
            foreach (var entry in Directory.EnumerateFileSystemEntries(source))
            {
                if ((File.GetAttributes(entry) & FileAttributes.ReparsePoint) != 0) throw new IOException("A validation source is a symbolic link.");
                CopyValidationSource(entry, Path.Combine(destination, Path.GetFileName(entry)));
            }
        }
        else if (File.Exists(source))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.Copy(source, destination, overwrite: false);
        }
    }

    internal static object VerifyValidationFiles(string sandbox, string branch)
    {
        var checkedFiles = 0;
        var preparedPcks = 0;
        long checkedBytes = 0;
        foreach (var path in Directory.EnumerateFiles(SteamGameInstallPaths.DownloadStateDirectoryPath(sandbox, branch), "*.manifest"))
        {
            using var input = File.OpenRead(path);
            var manifest = DepotManifest.Deserialize(input);
            foreach (var file in manifest.Files!)
            {
                if (file.Flags.HasFlag(EDepotFileFlag.Directory)) continue;
                if (DepotDownloader.NormalizeManifestPath(file.FileName) == "SlayTheSpire2.pck") { preparedPcks++; continue; }
                if (!DepotDownloader.VerifyFileHash(DepotDownloadStaging.Resolve(SteamGameInstallPaths.GameDirectory(sandbox, branch), file.FileName), file))
                    throw new IOException("Completed raw Steam file does not match its manifest.");
                checkedFiles++;
                checkedBytes += checked((long)file.TotalSize);
            }
        }
        return new { checkedFiles, checkedBytes, preparedPcks, preparedPckVerifiedByReadyIdentity = true };
    }
}
