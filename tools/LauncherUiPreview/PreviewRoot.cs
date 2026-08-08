using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace LauncherUiPreview;

public partial class PreviewRoot : Control
{
    private readonly Dictionary<string, string> _arguments = new(
        StringComparer.OrdinalIgnoreCase
    );

    public override async void _Ready()
    {
        try
        {
            await RunAsync();
        }
        catch (Exception ex)
        {
            GD.PushError($"Launcher UI preview failed: {ex}");
            GetTree().Quit(1);
        }
    }

    private async Task RunAsync()
    {
        System.Environment.SetEnvironmentVariable("STS2_LAUNCHER_PREVIEW", "1");
        ValidateLayoutProfiles();
        ParseArguments(OS.GetCmdlineUserArgs());

        var width = ReadInt("width", 1280);
        var height = ReadInt("height", 800);
        var previewViewport = BuildFixture(new Vector2I(width, height));
        await NextFrames(20);
        RenderingServer.ForceSync();
        RenderingServer.ForceDraw(swapBuffers: false, frameStep: 0d);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        RenderingServer.ForceSync();
        LauncherUiAccessibilityValidator.Validate(
            _previewRoot,
            touchOptimized: ReadBool("touch", true),
            viewportSize: new Vector2(width, height)
        );
        if (ScrollRecoveryFixtureIntoView())
        {
            await NextFrames(3);
            RenderingServer.ForceSync();
            RenderingServer.ForceDraw(swapBuffers: false, frameStep: 0d);
            await ToSignal(
                RenderingServer.Singleton,
                RenderingServer.SignalName.FramePostDraw
            );
        }
        LauncherCloudProgressPreviewValidator.Validate(
            _previewRoot,
            Read("fixture", "ready"),
            Read("destination", "home")
        );
        RenderingServer.ForceDraw(swapBuffers: false, frameStep: 0d);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        RenderingServer.ForceSync();

        var output = Read("output", "");
        if (!string.IsNullOrWhiteSpace(output))
        {
            var fullPath = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            var error = previewViewport.GetTexture().GetImage().SavePng(fullPath);
            if (error != Error.Ok)
                throw new InvalidOperationException($"Failed to save preview screenshot: {error}");
            GD.Print($"PREVIEW_SCREENSHOT={fullPath}");
            if (ReadBool("validate-contract", false))
            {
                LauncherUiContractValidator.Validate(_previewView, _previewRoot);
                GD.Print("PREVIEW_CONTRACT=passed");
            }
            previewViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
            previewViewport.QueueFree();
            await NextFrames(2);
            GetTree().Quit();
        }
    }

    private LauncherView _previewView = null!;
    private Control _previewRoot = null!;

    private SubViewport BuildFixture(Vector2I viewportSize)
    {
        var previewViewport = new SubViewport
        {
            Size = viewportSize,
            Disable3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
        };
        AddChild(previewViewport);

        var previewRoot = new Control
        {
            CustomMinimumSize = new Vector2(viewportSize.X, viewportSize.Y),
            Size = new Vector2(viewportSize.X, viewportSize.Y),
        };
        previewViewport.AddChild(previewRoot);
        previewRoot.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var touchOptimized = ReadBool("touch", true);
        var profile = LauncherLayoutProfile.ForViewport(new Vector2(viewportSize.X, viewportSize.Y), touchOptimized);
        var view = new LauncherView(previewRoot, profile);
        _previewView = view;
        _previewRoot = previewRoot;
        ApplyFixture(view, Read("fixture", "ready"));
        view.SelectDestination(ReadDestination(Read("destination", "home")));
        GD.Print($"PREVIEW_PROFILE={profile}");
        return previewViewport;
    }

    private static void ApplyFixture(LauncherView view, string fixture)
    {
        switch (fixture.Trim().ToLowerInvariant())
        {
            case "signed-out":
                view.SetStatus("Sign in to Steam to verify ownership and download your game files.");
                view.HideActions();
                view.SetLoginFormVisible(visible: true, disabled: false);
                break;
            case "guard":
                view.SetStatus("Steam Guard verification is required.");
                view.ShowCodePrompt(wasIncorrect: false);
                break;
            case "download":
                view.SetStatus("Downloading the selected public game version.");
                view.ShowDownloadAction("Download Game");
                view.ShowDownloadProgress("Downloading public game files...");
                view.SetDownloadProgress(42, "42% | 3.8 GB of 9.1 GB");
                break;
            case "error":
                view.SetStatus("Game files need repair before Start Game can continue.");
                view.ShowRetry();
                break;
            case "ready":
                ApplyReadyFixture(view);
                break;
            case "pull-transfer":
                ApplyReadyFixture(view);
                view.SetCloudOperationState(ActivePullState());
                view.SetPushPullDisabled(disabled: true);
                break;
            case "pull-complete":
                ApplyReadyFixture(view);
                view.SetCloudOperationState(CompletedPullState());
                view.SetPushPullDisabled(disabled: false);
                break;
            case "sync-source-choice":
                ApplyReadyFixture(view);
                view.SetAutomaticSyncBlocked(true);
                view.SetStatus("Choose the save copy to trust before launch.");
                view.ShowAutomaticSyncSourceChoice(
                    "No trusted baseline exists for this exact account, game version, and mod set. "
                        + "Use Android uploads the Android copy. Use Steam backs up Android first, then downloads the Steam copy.",
                    _ => { }
                );
                break;
            case "sync-reconciling":
                ApplyReadyFixture(view);
                view.SetAutomaticSyncBlocked(true);
                view.SetStatus(
                    "Reconciling Android and Steam saves before launch..."
                );
                break;
            case "sync-conflict":
                ApplyReadyFixture(view);
                view.SetAutomaticSyncBlocked(false);
                view.SetStatus(
                    "Launch blocked by an Android/Steam save conflict. "
                        + "Start Game will retry after you resolve which copy to keep."
                );
                break;
            case "sync-offline-pending":
                ApplyReadyFixture(view);
                view.SetAutomaticSyncBlocked(false);
                view.SetStatus(
                    "Pending automatic save sync remains on disk because Steam is offline. "
                        + "Start Game safely retries recovery."
                );
                break;
            case "recovery-empty":
                ApplyReadyFixture(view);
                view.SetSaveRecoveryCandidates(
                    Array.Empty<SaveRecoveryCandidatePresentation>()
                );
                view.SetSaveRecoveryState(
                    "No local recovery copy is currently available. Steam was not contacted.",
                    canUndo: false,
                    canApprove: false
                );
                break;
            case "recovery-unknown":
                ApplyReadyFixture(view);
                view.SetSaveRecoveryCandidates(
                    new[] { UnknownRecoveryCandidate() }
                );
                view.SetSaveRecoveryState(
                    "This copy can be restored on Android, but its unknown context must not be guessed or approved for sync yet.",
                    canUndo: false,
                    canApprove: false
                );
                break;
            case "recovery-confirm":
                ApplyReadyFixture(view);
                view.SetSaveRecoveryCandidates(
                    new[] { ValidatedRecoveryCandidate() }
                );
                view.SetSaveRecoveryState(
                    "Validated and ready for explicit local restore. Steam will not be changed.",
                    canUndo: false,
                    canApprove: false
                );
                break;
            case "recovery-restored":
                ApplyReadyFixture(view);
                view.SetSaveRecoveryCandidates(
                    new[] { ValidatedRecoveryCandidate() }
                );
                view.SetSaveRecoveryState(
                    "Restored and verified on Android. Steam was not changed. Check the save in game before approving it for sync.",
                    canUndo: true,
                    canApprove: true
                );
                break;
            default:
                throw new ArgumentException($"Unknown preview fixture: {fixture}");
        }
    }

    private static SaveRecoveryCandidatePresentation
        UnknownRecoveryCandidate()
        => new(
            "legacy-local-pre-pull-profile1-20260707",
            "07 Jul 2026 — Legacy Android backup",
            "Source: local pre-Pull progress backup | Scope: progress.save only | Account: Unknown | Save type: Unknown | Game version: Unknown | Mod set: Unknown",
            CanRestore: true
        );

    private static SaveRecoveryCandidatePresentation
        ValidatedRecoveryCandidate()
        => new(
            "before-game-76561198000000001-public-vanilla",
            "Before last game — Vanilla public",
            "Source: immutable before-game snapshot | Captured: 06 Aug 2026 18:42 UTC | Account: 76561198000000001 | Save type: Vanilla | Game version: public | Mod set: None | Hashes: verified",
            CanRestore: true
        );

    private static void ApplyReadyFixture(LauncherView view)
    {
        view.SetActionPreferences(
            new LauncherPreferences.ActionPreferences(
                localBackupEnabled: true,
                cloudSyncEnabled: true,
                gameBranch: "public",
                rendererMode: LauncherRendererMode.Auto
            )
        );
        view.SetStatus("Ready to play the selected Default game version.");
        view.ShowLaunchActions("Start Game", showUpdate: true);
        view.SetPushPullDisabled(disabled: false);
    }

    private static CloudOperationState ActivePullState()
    {
        var tracker = PreparedPullTracker();
        tracker.TransferStarted(24);
        for (var index = 0; index < 8; index++)
        {
            tracker.TransferProcessed(
                $"profile1/saves/history/download-{index + 1}.run"
            );
        }
        for (var index = 0; index < 3; index++)
        {
            tracker.TransferProcessed(
                $"profile2/saves/history/download-{index + 1}.run"
            );
        }
        tracker.TransferPathStarted("profile2/saves/progress.save");
        return tracker.State;
    }

    private static CloudOperationState CompletedPullState()
    {
        var tracker = PreparedPullTracker();
        tracker.TransferStarted(24);
        for (var index = 0; index < 24; index++)
        {
            tracker.TransferProcessed(
                $"profile{index % 3 + 1}/saves/history/download-{index + 1}.run"
            );
        }
        tracker.Finalizing("Verifying the save-context marker");
        tracker.Completed("All save files were transferred and verified");
        return tracker.State;
    }

    private static CloudOperationProgressTracker PreparedPullTracker()
    {
        var tracker = new CloudOperationProgressTracker(
            CloudOperationKind.Pull
        );
        tracker.Preparing("Connecting to Steam Cloud");
        tracker.EnumerationStarted("Checking Steam Cloud save locations");
        tracker.EnumerationCompleted(24);
        tracker.BackupStarted(24);
        for (var index = 0; index < 24; index++)
        {
            tracker.BackupProcessed(
                $"profile{index % 3 + 1}/saves/backup-{index + 1}.save",
                created: index < 3
            );
        }
        return tracker;
    }

    private async Task NextFrames(int count)
    {
        for (var i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private bool ScrollRecoveryFixtureIntoView()
    {
        if (!Read("fixture", "ready").StartsWith(
                "recovery-",
                StringComparison.OrdinalIgnoreCase
            ))
        {
            return false;
        }

        var scrolled = false;
        foreach (var scroll in Descendants<ScrollContainer>(_previewRoot))
        {
            if (!scroll.IsVisibleInTree())
                continue;

            scroll.ScrollVertical = int.MaxValue;
            scrolled = true;
        }
        return scrolled;
    }

    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T match)
                yield return match;
            foreach (var descendant in Descendants<T>(child))
                yield return descendant;
        }
    }

    private static LauncherDestination ReadDestination(string value)
        => value.Trim().ToLowerInvariant() switch
        {
            "home" => LauncherDestination.Home,
            "saves" => LauncherDestination.Saves,
            "versions" => LauncherDestination.Versions,
            "mods" => LauncherDestination.Mods,
            "help" => LauncherDestination.Help,
            _ => throw new ArgumentException($"Unknown preview destination: {value}"),
        };

    private static void ValidateLayoutProfiles()
    {
        ExpectMode(new Vector2(1080, 2400), touch: true, expected: LauncherLayoutMode.PhonePortrait);
        ExpectMode(new Vector2(2400, 1080), touch: true, expected: LauncherLayoutMode.PhoneLandscape);
        ExpectMode(new Vector2(2184, 1968), touch: true, expected: LauncherLayoutMode.Wide);
        ExpectMode(new Vector2(1280, 800), touch: false, expected: LauncherLayoutMode.Wide);
    }

    private static void ExpectMode(Vector2 viewport, bool touch, LauncherLayoutMode expected)
    {
        var actual = LauncherLayoutProfile.ForViewport(viewport, touch).Mode;
        if (actual != expected)
            throw new InvalidOperationException(
                $"Viewport {viewport} touch={touch} resolved to {actual}; expected {expected}."
            );
    }

    private void ParseArguments(string[] args)
    {
        foreach (var argument in args)
        {
            var normalized = argument.TrimStart('-');
            var separator = normalized.IndexOf('=');
            if (separator <= 0)
                continue;
            _arguments[normalized[..separator]] = normalized[(separator + 1)..];
        }
    }

    private string Read(string name, string fallback)
        => _arguments.TryGetValue(name, out var value) ? value : fallback;

    private int ReadInt(string name, int fallback)
        => int.TryParse(Read(name, ""), out var value) ? value : fallback;

    private bool ReadBool(string name, bool fallback)
        => bool.TryParse(Read(name, ""), out var value) ? value : fallback;
}
