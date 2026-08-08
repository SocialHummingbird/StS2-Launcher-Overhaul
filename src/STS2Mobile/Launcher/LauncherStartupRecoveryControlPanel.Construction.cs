using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private LauncherStartupRecoveryControlPanel(Vector2 viewportSize)
    {
        var scale = LayoutScale(viewportSize);
        var compactCopy = UseCompactRecoveryCopy(viewportSize);
        Layer = CreateLayer();

        var scroll = CreateScrollContainer();
        Layer.AddChild(scroll);

        var frame = CreateFrame(viewportSize);
        scroll.AddChild(frame);

        var box = CreateContainer(viewportSize);
        frame.AddChild(box);

        box.AddChild(CreateTitle(scale));
        _detail = CreateDetail(scale, compactCopy);
        box.AddChild(_detail);

        AddRecoveryActions(box, scale, ButtonMinimumSize(viewportSize, scale), compactCopy);
    }
}
