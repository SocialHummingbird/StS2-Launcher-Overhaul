using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal LauncherPreparationOverlay LockPreparationInteraction()
    {
        var overlay = new LauncherPreparationOverlay
        {
            Name = "LaunchPreparation",
            Layer = 2000,
            ProcessMode = Node.ProcessModeEnum.Always,
        };
        var shade = new ColorRect
        {
            Color = LauncherComponentTheme.DialogOverlay,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        shade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var center = BuildDialogCenter();
        var box = BuildDialogBox(_scale);
        overlay.Message = BuildDialogMessage("Preparing game files…", _profile);
        box.AddChild(overlay.Message);
        center.AddChild(box);
        shade.AddChild(center);
        overlay.AddChild(shade);
        _parent.GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
        _parent.AddChild(overlay);
        overlay.SetProcessInput(true);
        return overlay;
    }
}
