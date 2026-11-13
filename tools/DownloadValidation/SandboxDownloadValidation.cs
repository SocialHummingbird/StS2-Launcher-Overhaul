using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

// Explicit test-build entry point. Uses the production controls, model and coordinator,
// with no normal controller/session/save/launch bindings and only test-owned mutable paths.
internal static class SandboxDownloadValidation
{
    private static string _output, _attempt, _transfer;
    private static readonly object Gate = new();
    private static Dictionary<string, object> _result;
    private static LauncherModel _model;
    private static LauncherDownloadCoordinator _downloads;
    private static LauncherLaunchCoordinator _launch;
    private static LauncherView _view;
    private static Action<Action> _main;
    private static string _branch;
    private static bool _observing;

    internal static async Task StartAsync(string original, LauncherView view, Action<Action> main, DownloadValidationRequest request)
    {
        var root = Path.Combine(original, "download-validation");
        var run = DownloadValidationRunner.OwnedPath(root, request.RunId);
        var sandbox = Path.Combine(run, "data");
        Directory.CreateDirectory(sandbox);
        _output = DownloadValidationRunner.OwnedPath(Path.Combine(AndroidGodotAppBridge.GetExternalFilesDirPath(), "download-validation-evidence", "runs"), request.RunId);
        Directory.CreateDirectory(_output);
        _attempt = Guid.NewGuid().ToString("N");
        _branch = SteamGameBranch.Normalize(request.Branch);
        _view = view; _main = main;
        _result = new()
        {
            ["operation"] = "sandbox-ui",
            ["scenario"] = request.Scenario,
            ["runId"] = request.RunId,
            ["attemptId"] = _attempt,
            ["status"] = "preparing",
            ["steamCloudOperations"] = 0,
            ["gameplayLaunchBindings"] = 0,
            ["normalSessionStartup"] = false,
            ["sandbox"] = sandbox,
            ["branch"] = _branch
        };
        Save();
        try
        {
            if (request.Scenario != "fresh" && !File.Exists(Path.Combine(run, "source-copied")))
            {
                if (request.Scenario != "reuse" && request.Scenario != "pck-repair") throw new IOException("Unsupported sandbox scenario.");
                if (request.SourceRunId == request.RunId || !System.Text.RegularExpressions.Regex.IsMatch(request.SourceRunId ?? "", "^[a-zA-Z0-9][a-zA-Z0-9-]{0,79}$"))
                    throw new IOException("Invalid completed sandbox source.");
                var donor = Path.Combine(DownloadValidationRunner.OwnedPath(root, request.SourceRunId), "data");
                if (!BranchInstallStateStore.Current.Read(donor, _branch).IsReady) throw new IOException("Sandbox donor is not a completed installation.");
                main(() => view.SetStatus("Copying the completed test installation…", LauncherStatusSeverity.Working));
                foreach (var relative in new[] { "game", "download_state", "installation_state.json" })
                    LauncherModel.CopyValidationSource(Path.Combine(SteamGameInstallPaths.VersionSlotDirectory(donor, _branch), relative),
                        Path.Combine(SteamGameInstallPaths.VersionSlotDirectory(sandbox, _branch), relative));
                var pck = Path.Combine(SteamGameInstallPaths.GameDirectory(sandbox, _branch), "SlayTheSpire2.pck");
                var before = await Task.Run(() => Sha256(pck)).ConfigureAwait(false);
                if (request.Scenario == "pck-repair")
                {
                    // A fixed range in the copied file only. Original source and completed donor remain read-only.
                    using var stream = new FileStream(pck, FileMode.Open, FileAccess.ReadWrite);
                    stream.Position = 4 * 1024 * 1024;
                    var value = stream.ReadByte(); stream.Position--;
                    stream.WriteByte((byte)(value ^ 255)); stream.Flush(true);
                }
                DownloadValidationRunner.Write(Path.Combine(run, "copy-evidence.json"), new
                {
                    sourceRun = request.SourceRunId,
                    originalPckSha256 = before,
                    scenario = request.Scenario,
                    corruptionOffset = request.Scenario == "pck-repair" ? 4 * 1024 * 1024 : -1
                });
                File.WriteAllText(Path.Combine(run, "source-copied"), "test-owned completed installation copied");
            }
            if (File.Exists(Path.Combine(run, "copy-evidence.json")))
                _result["copyEvidence"] = JsonDocument.Parse(File.ReadAllText(Path.Combine(run, "copy-evidence.json"))).RootElement.Clone();
            File.Copy(Path.Combine(original, "steam_credentials.enc"), Path.Combine(sandbox, "steam_credentials.enc"), true);
            File.WriteAllText(Path.Combine(sandbox, "game_branch"), _branch);
            AppPaths.ValidationDataRoot = sandbox;
            ValidationJava.Call("configureDownloadValidationHttp", request.RunId);
            AndroidJavaHttpMessageHandler.ValidationHttpChanged += entry => Append("http-events.jsonl", new { attemptId = _attempt, entry });
            _model = new LauncherModel(sandbox);
            _model.LoadValidationCredentials();
            _result["initialRouting"] = LocalPckRepairOperation.ClassifyForLauncherRouting(sandbox, _branch).ToString();
            if (!await CaptureStagingAsync("startup").ConfigureAwait(false)) return;
            main(() => Wire());
        }
        catch (Exception error) { Failure(error); }
    }

    private static void Wire()
    {
        var diagnostics = new LauncherDiagnosticsCoordinator(_model, _view);
        _launch = new LauncherLaunchCoordinator(_model, _view, diagnostics);
        _downloads = new LauncherDownloadCoordinator(_model, _view, _launch, () => { });
        var versions = new LauncherVersionCoordinator(_model, _view);
        _view.SetActionPreferences(LauncherPreferences.ReadActionPreferences(), versions.ReadGameBranchOptions());
        _view.WireValidationDownloadEvents(() => { _result["status"] = "transferring"; Event("download-tapped"); _downloads.DownloadPressed(); },
            () => { Event("pause-tapped"); _downloads.PausePressed(); });
        _model.DownloadProgressChanged += progress => _main(_model.BindCurrentDownloadCallback(() => _downloads.UpdateDownloadProgress(progress)));
        _model.DownloadLogReceived += message => { Append("validation.log.jsonl", new { attemptId = _attempt, message = DownloadValidationRunner.SafeText(message) }); _main(() => _view.AppendLog(DownloadValidationRunner.SafeText(message))); };
        _model.DownloadCancelled += branch => _main(() => { _downloads.CancelDownload(branch); Event("paused-drained"); _ = CaptureStagingAsync("paused"); });
        _model.DownloadFailed += failure => _main(() => { _downloads.FailDownload(failure); _result["status"] = "failed"; _result["failure"] = DownloadValidationRunner.SafeText(failure.Message); Event("failed"); });
        _model.DownloadCompleted += completion => _main(() =>
        {
            _downloads.CompleteDownload(completion);
            _result["status"] = "verifying"; Save();
            _ = CompleteAsync(completion);
        });
        _view.HideActions();
        if (!_downloads.TryShowDownloadRecovery(_branch)) _downloads.ShowDownloadReadyAction();
        _result["status"] = "awaiting-tap"; Event("controls-ready");
        _ = SnapshotLoopAsync();
    }

    internal static void ObserveDownloader(DepotDownloader downloader)
    {
        if (_output == null) return;
        var transferId = Guid.NewGuid().ToString("N");
        _transfer = transferId;
        lock (Gate) { _result["latestTransferId"] = transferId; Save(); }
        downloader.ValidationAttemptId = transferId;
        downloader.ConfigureValidationTrace(_output, "sandbox-ui/" + transferId);
        downloader.ValidationChunkChanged += entry =>
        {
            Append("chunk-events.jsonl", entry);
            lock (Gate) DownloadValidationRunner.Write(Path.Combine(_output, "chunk-summary.json"), entry);
        };
        downloader.ValidationRawProgressChanged += (depot, progress) =>
        {
            Append("raw-progress.jsonl", new { attemptId = _attempt, transferId, depot, progress });
            lock (Gate) DownloadValidationRunner.Write(Path.Combine(_output, "depot-progress.json"), new { attemptId = _attempt, transferId, depot, progress });
        };
    }

    private static async Task<bool> CaptureStagingAsync(string phase)
    {
        try
        {
            lock (Gate) { _result["stagingInspectionStatus"] = "running"; Save(); }
            if (_model.SelectedVersionOperationIsRunning) throw new IOException("Staging inspection requires a drained operation.");
            var snapshot = await Task.Run(() => ValidationStagingInspection.Read(_model.DataDir, _branch)).ConfigureAwait(false);
            var name = "staging-" + phase + "-" + _attempt + "-" + (_transfer ?? "none") + ".json";
            lock (Gate)
            {
                DownloadValidationRunner.Write(Path.Combine(_output, name), new { attemptId = _attempt, transferId = _transfer, phase, snapshot });
                _result["stagingInspectionStatus"] = "complete";
                _result["stagingInspectionFile"] = name;
                Save();
            }
            return true;
        }
        catch (Exception error) { Failure(error); return false; }
    }

    private static async Task CompleteAsync(BranchInstallCompletion completion)
    {
        try
        {
            var checkedFiles = await Task.Run(() => LauncherModel.VerifyValidationFiles(_model.DataDir, _branch)).ConfigureAwait(false);
            var readiness = await Task.Run(() => LauncherLaunchReadiness.EvaluateCompletedInstall(_model.DataDir, completion.GameIdentity, "sandbox validation")).ConfigureAwait(false);
            var pck = Path.Combine(SteamGameInstallPaths.GameDirectory(_model.DataDir, _branch), "SlayTheSpire2.pck");
            var pckHash = await Task.Run(() => Sha256(pck)).ConfigureAwait(false);
            var javaState = JsonDocument.Parse(ValidationJava.Call("getDownloadValidationHttpState")).RootElement.Clone();
            lock (Gate)
            {
                _result["rawFileVerification"] = checkedFiles;
                _result["finalPckSha256"] = pckHash;
                _result["ready"] = readiness.Ready; _result["readinessProblem"] = readiness.ReadinessProblem;
                _result["gameIdentity"] = completion.GameIdentity.Id;
                _result["pckPreparation"] = BranchInstallStateStore.Current.Read(_model.DataDir, _branch).PckPreparationVersion;
                _result["javaAtCompletion"] = javaState;
                _result["status"] = readiness.Ready ? "passed" : "failed"; Save();
            }
        }
        catch (Exception error) { Failure(error); }
    }

    private static async Task SnapshotLoopAsync()
    {
        if (_observing) return;
        _observing = true;
        while (true)
        {
            try { _main(() => { lock (Gate) DownloadValidationRunner.Write(Path.Combine(_output, "ui.json"), _view.ValidationUiSnapshot()); }); }
            catch (Exception error) { Failure(error); return; }
            await Task.Delay(500).ConfigureAwait(false);
        }
    }
    private static void Event(string name)
    {
        var ready = File.Exists(BranchInstallStateStore.PathFor(_model.DataDir, _branch))
            && BranchInstallStateStore.Current.Read(_model.DataDir, _branch).IsReady;
        var partialRoot = Path.Combine(SteamGameInstallPaths.DownloadStateDirectoryPath(_model.DataDir, _branch), "staging-v1");
        var partials = Directory.Exists(partialRoot) ? Directory.EnumerateFiles(partialRoot, "*.downloading", SearchOption.AllDirectories).Count() : 0;
        Append("ui-events.jsonl", new
        {
            attemptId = _attempt,
            utc = DateTimeOffset.UtcNow,
            name,
            IsReady = ready,
            running = _model.SelectedVersionOperationIsRunning,
            pending = LauncherPendingDownload.Inspect(_model.DataDir, _branch).CanResume,
            partials
        });
        if (name == "paused-drained") { _result["status"] = "paused"; _result["ready"] = ready; _result["retainedPartialFiles"] = partials; }
        Save();
    }
    private static string Sha256(string path) => GameIdentityFileHasher.Instance.Sha256(path);
    private static void Append(string name, object value) { lock (Gate) File.AppendAllText(Path.Combine(_output, name), JsonSerializer.Serialize(value) + "\n"); }
    private static void Save() { lock (Gate) DownloadValidationRunner.Write(Path.Combine(_output, "result.json"), _result); }
    private static void Failure(Exception error) { lock (Gate) { _result["status"] = "failed"; _result["failure"] = DownloadValidationRunner.SafeError(error); Save(); } }
}

internal partial class LauncherModel
{
    internal void LoadValidationCredentials()
    {
        _credentialStore.Load();
        if (!_credentialStore.HasUsableCredentials()) throw new IOException("Sandbox saved credentials unavailable.");
    }
}
