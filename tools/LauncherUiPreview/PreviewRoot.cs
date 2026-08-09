using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Launcher;

namespace LauncherUiPreview;

public partial class PreviewRoot : Control
{
    private readonly Dictionary<string, string> _arguments = new(
        StringComparer.OrdinalIgnoreCase
    );

    private LauncherView _previewView = null!;
    private Control _previewRoot = null!;

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
        ParseArguments(OS.GetCmdlineUserArgs());

        var width = ReadInt("width", 1280);
        var height = ReadInt("height", 800);
        var previewViewport = BuildFixture(new Vector2I(width, height));
        await NextFrames(20);
        RenderingServer.ForceSync();
        RenderingServer.ForceDraw(swapBuffers: false, frameStep: 0d);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        RenderingServer.ForceSync();

        if (ReadBool("interaction-test", false))
        {
            LauncherUiInteractionTest.Run(_previewView, _previewRoot);
            GD.Print("PREVIEW_INTERACTION=passed");
        }

        var output = Read("output", "");
        if (!string.IsNullOrWhiteSpace(output))
        {
            var fullPath = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            var error = previewViewport.GetTexture().GetImage().SavePng(fullPath);
            if (error != Error.Ok)
                throw new InvalidOperationException(
                    $"Failed to save preview screenshot: {error}"
                );
            GD.Print($"PREVIEW_SCREENSHOT={fullPath}");
        }

        previewViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
        previewViewport.QueueFree();
        await NextFrames(2);
        GetTree().Quit();
    }

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
        var profile = LauncherLayoutProfile.ForViewport(
            new Vector2(viewportSize.X, viewportSize.Y),
            touchOptimized
        );
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
                view.SetStatus("Sign in to continue.");
                view.HideActions();
                view.SetLoginFormVisible(visible: true, disabled: false);
                break;
            case "guard":
                view.SetStatus("Enter your Steam Guard code.");
                view.ShowCodePrompt(wasIncorrect: false);
                break;
            case "download":
                view.SetStatus("Downloading...");
                view.ShowDownloadAction("Download Game");
                view.ShowDownloadProgress("Downloading public game files...");
                view.SetDownloadProgress(42, "42% | 3.8 GB of 9.1 GB");
                break;
            case "error":
                view.SetStatus("Download required.");
                view.ShowRetry();
                break;
            case "ready":
                ApplyReadyFixture(view);
                break;
            default:
                throw new ArgumentException($"Unknown preview fixture: {fixture}");
        }
    }

    private static void ApplyReadyFixture(LauncherView view)
    {
        view.SetActionPreferences(
            new LauncherPreferences.ActionPreferences(
                gameBranch: "public",
                rendererMode: LauncherRendererMode.Auto
            )
        );
        view.SetHomeAccountState("Signed in");
        view.SetHomeGameState("Default installed");
        view.SetSaveSyncPresentation(
            "Synced",
            "9 Aug 2026, 10:30",
            "Up to date",
            "Up to date when last checked"
        );
        view.SetStatus("Ready to play.");
        view.ShowLaunchActions("Play", showUpdate: true);
    }

    private async Task NextFrames(int count)
    {
        for (var i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
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
