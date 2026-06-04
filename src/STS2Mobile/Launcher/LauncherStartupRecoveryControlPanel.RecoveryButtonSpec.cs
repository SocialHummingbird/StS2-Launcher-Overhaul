using System;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private readonly struct RecoveryButtonSpec
    {
        private RecoveryButtonSpec(string label, Action run)
        {
            Label = label;
            Run = run;
        }

        private string Label { get; }
        private Action Run { get; }

        internal static RecoveryButtonSpec ReturnToLauncher(Action run)
            => new("RETURN TO LAUNCHER", run);

        internal static RecoveryButtonSpec RestartWithSafeLaunch(Action run)
            => new("RESTART WITH SAFE LAUNCH", run);

        internal static RecoveryButtonSpec ExportStartupDiagnostics(Action run)
            => new("EXPORT STARTUP DIAGNOSTICS", run);

        internal static RecoveryButtonSpec CopyRawErrorLog(Action run)
            => new("COPY RAW ERROR LOG", run);

        internal static RecoveryButtonSpec HideRecoveryControls(Action run)
            => new("HIDE RECOVERY CONTROLS", run);

        internal void AddTo(VBoxContainer box)
        {
            var button = new Button
            {
                Text = Label,
                CustomMinimumSize = ButtonMinimumSize,
            };
            button.Pressed += Run;
            box.AddChild(button);
        }
    }
}
