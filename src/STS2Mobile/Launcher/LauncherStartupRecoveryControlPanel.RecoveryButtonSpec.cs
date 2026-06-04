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

        internal static RecoveryButtonSpec For(string label, Action run)
            => new(label, run);

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
