using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private const string CopyRawErrorLogButton = "Copy Launcher Log (Review First)";
    private const string ExportDiagnosticsButton = "Create Startup Help Report";
    private const string HideControlsButton = "Hide Recovery Controls";
    private const string RestartSafeLaunchButton = "Restart with Safe Launch";
    private const string ReturnToLauncherButton = "Return to Launcher";

    private readonly struct RecoveryButtonSpec
    {
        private RecoveryButtonSpec(string label, string detail, Action run)
        {
            Label = label;
            Detail = detail;
            Run = run;
        }

        private string Label { get; }
        private string Detail { get; }
        private Action Run { get; }

        private Button CreateButton(float scale, Vector2 minimumSize, bool structured)
        {
            var button = new Button
            {
                Text = structured ? "" : Label,
                CustomMinimumSize = minimumSize,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            button.AddThemeFontSizeOverride(
                ThemeFontSize,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.ButtonDefaultFontSize)
            );
            LauncherButtonStyles.ApplySupportAction(button, scale);
            if (structured)
                AddCompactRecoveryButtonLabels(button, scale, Label, Detail);
            button.Pressed += Run;
            return button;
        }

        internal static Button CreateButton(
            string label,
            string detail,
            Action run,
            float scale,
            Vector2 minimumSize,
            bool compactCopy
        )
            => new RecoveryButtonSpec(label, detail, run)
                .CreateButton(scale, minimumSize, compactCopy && !string.IsNullOrWhiteSpace(detail));
    }

    private void AddRecoveryActions(VBoxContainer box, float scale, Vector2 buttonMinimumSize, bool compactCopy)
    {
        foreach (var action in RecoveryButtons(scale, buttonMinimumSize, compactCopy))
            box.AddChild(action);
    }

    private Button[] RecoveryButtons(float scale, Vector2 buttonMinimumSize, bool compactCopy)
        => new[]
        {
            RecoveryButtonSpec.CreateButton(compactCopy ? "Restart App" : ReturnToLauncherButton, "Open launcher", AndroidGodotAppBridge.RestartApp, scale, buttonMinimumSize, compactCopy),
            RecoveryButtonSpec.CreateButton(compactCopy ? "Safe Start" : RestartSafeLaunchButton, "Cloud off", RestartWithSafeLaunch, scale, buttonMinimumSize, compactCopy),
            RecoveryButtonSpec.CreateButton(compactCopy ? "Help Report" : ExportDiagnosticsButton, "Share details", ExportDiagnostics, scale, buttonMinimumSize, compactCopy),
            RecoveryButtonSpec.CreateButton(compactCopy ? "Copy Log" : CopyRawErrorLogButton, "Review first", CopyRawErrorLog, scale, buttonMinimumSize, compactCopy),
            RecoveryButtonSpec.CreateButton(compactCopy ? "Hide Help" : HideControlsButton, "Keep waiting", HideRecoveryControls, scale, buttonMinimumSize, compactCopy),
        };

    private void HideRecoveryControls()
        => Layer.QueueFree();
}
