using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Godot;
using STS2Mobile;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed class LauncherStartupRecoveryControlPanel
{
    internal const string NodeName = "STS2MobileStartupRecovery";

    private const int CanvasLayerIndex = 128;
    private const int ContainerSeparation = 10;
    private const int DetailFontSize = 18;
    private const int TitleFontSize = 24;
    private const string ThemeFontColor = "font_color";
    private const string ThemeFontSize = "font_size";
    private const string ThemeSeparation = "separation";

    private static readonly Vector2 ButtonMinimumSize = new(420, 56);
    private static readonly Vector2 ContainerMinimumSize = new(820, 0);
    private static readonly Vector2 ContainerPosition = new(24, 72);
    private static readonly Color DetailColor = new(0.9f, 0.9f, 0.9f);
    private static readonly Color TitleColor = new(0.55f, 0.85f, 1f);

    private CanvasLayer Layer { get; }

    private readonly Label _detail;

    internal static CanvasLayer Show(Node parent)
    {
        try
        {
            if (parent.HasNode(NodeName))
                return parent.GetNode<CanvasLayer>(NodeName);

            var panel = new LauncherStartupRecoveryControlPanel(NodeName);
            parent.AddChild(panel.Layer);
            return panel.Layer;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup recovery controls failed: {ex.Message}");
            return null;
        }
    }

    private LauncherStartupRecoveryControlPanel(string nodeName)
    {
        Layer = new CanvasLayer
        {
            Name = nodeName,
            Layer = CanvasLayerIndex,
        };

        var box = new VBoxContainer
        {
            Position = ContainerPosition,
            CustomMinimumSize = ContainerMinimumSize,
        };
        box.AddThemeConstantOverride(ThemeSeparation, ContainerSeparation);
        Layer.AddChild(box);

        var title = new Label
        {
            Text = "Game is starting.",
        };
        title.AddThemeFontSizeOverride(ThemeFontSize, TitleFontSize);
        title.AddThemeColorOverride(ThemeFontColor, TitleColor);
        box.AddChild(title);

        _detail = new Label
        {
            Text = "If this screen does not change, copy the raw error log, export diagnostics, or restart with safe launch. These controls stay visible for several minutes.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _detail.AddThemeFontSizeOverride(ThemeFontSize, DetailFontSize);
        _detail.AddThemeColorOverride(ThemeFontColor, DetailColor);
        box.AddChild(_detail);

        AddButton(box, "RETURN TO LAUNCHER", AndroidGodotAppBridge.RestartApp);
        AddButton(
            box,
            "RESTART WITH SAFE LAUNCH",
            () =>
            {
                LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
                AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
            }
        );
        AddButton(box, "EXPORT STARTUP DIAGNOSTICS", ExportDiagnostics);
        AddButton(box, "COPY RAW ERROR LOG", CopyRawErrorLog);
        AddButton(box, "HIDE RECOVERY CONTROLS", () => Layer.QueueFree());
    }

    private static void AddButton(VBoxContainer box, string label, Action run)
    {
        var button = new Button
        {
            Text = label,
            CustomMinimumSize = ButtonMinimumSize,
        };
        button.Pressed += run;
        box.AddChild(button);
    }

    private void ExportDiagnostics()
    {
        try
        {
            var path = WriteDiagnostics(BuildDiagnostics());
            PatchHelper.Log($"Startup recovery diagnostics written: {path}");
            var shared = AndroidGodotAppBridge.ShareTextFile(path);
            _detail.Text = shared
                ? $"Diagnostics exported and share sheet opened.\n\nSaved at:\n{path}"
                : $"Diagnostics exported, but the share sheet did not open.\n\nSaved at:\n{path}";
        }
        catch (Exception ex)
        {
            ShowActionFailure("diagnostics export", "Diagnostics export failed", ex);
        }
    }

    private static string WriteDiagnostics(string report)
    {
        var fileName = $"sts2-startup-recovery-diagnostics-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";
        var path = TryGetExternalDiagnosticsPath(fileName)
            ?? Path.Combine(OS.GetDataDir(), fileName);

        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        File.WriteAllText(path, report);
        return path;
    }

    private static string TryGetExternalDiagnosticsPath(string fileName)
    {
        try
        {
            var externalDir = AndroidGodotAppBridge.GetExternalFilesDirPath();
            if (string.IsNullOrWhiteSpace(externalDir))
                return null;

            var diagnosticsDir = Path.Combine(externalDir, "diagnostics");
            Directory.CreateDirectory(diagnosticsDir);
            return Path.Combine(diagnosticsDir, fileName);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"Startup recovery external diagnostics path unavailable: {ex.Message}"
            );
            return null;
        }
    }

    private void CopyRawErrorLog()
    {
        try
        {
            var rawLog = BuildDiagnostics();
            DisplayServer.ClipboardSet(rawLog);
            PatchHelper.Log($"Startup recovery raw error log copied ({rawLog.Length:N0} chars)");
            _detail.Text = $"Raw error log copied to clipboard.\n\nLength: {rawLog.Length:N0} characters";
        }
        catch (Exception ex)
        {
            ShowActionFailure("raw error log copy", "Raw error log copy failed", ex);
        }
    }

    private void ShowActionFailure(string logAction, string detailTitle, Exception ex)
    {
        PatchHelper.Log($"Startup recovery {logAction} failed: {ex}");
        _detail.Text = $"{detailTitle}:\n{ex.GetBaseException().Message}";
    }

    private static string BuildDiagnostics()
    {
        var sb = new StringBuilder();
        sb.AppendLine("STS2 startup recovery diagnostics");
        sb.AppendLine($"Generated UTC: {DateTime.UtcNow:O}");
        sb.AppendLine($"Data dir: {OS.GetDataDir()}");
        sb.AppendLine();

        foreach (var file in DiagnosticFiles())
            AppendDiagnosticFile(sb, file.Label, file.Path);

        AppendLogcat(sb);

        return sb.ToString();
    }

    private static IEnumerable<(string Label, string Path)> DiagnosticFiles()
    {
        yield return ("Startup marker", LauncherLaunchMarkers.StartupMarkerPath);
        yield return ("Startup scene snapshot", LauncherStartupSceneSnapshot.Path);
        yield return ("Bootstrap trace", BootstrapTrace.TracePath);
    }

    private static void AppendDiagnosticFile(StringBuilder sb, string label, string path)
    {
        sb.AppendLine(LauncherDiagnosticAppendixText.Header(label, path));

        try
        {
            if (!File.Exists(path))
            {
                sb.AppendLine("  exists=False");
                sb.AppendLine();
                return;
            }

            var file = new FileInfo(path);
            sb.AppendLine(
                $"  exists=True bytes={file.Length} modifiedUtc={file.LastWriteTimeUtc:O}"
            );
            sb.AppendLine("  contents:");
            sb.AppendLine(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  failed={ex.Message}");
        }

        sb.AppendLine();
    }

    private static void AppendLogcat(StringBuilder sb)
    {
        sb.AppendLine(LauncherDiagnosticAppendixText.AndroidLogcatTail);
        var logcat = LauncherLogcatSnapshot.Capture(
            LauncherDiagnosticLogcatSettings.StartupRecoveryTailLines
        );
        sb.AppendLine(logcat.Content);
    }
}
