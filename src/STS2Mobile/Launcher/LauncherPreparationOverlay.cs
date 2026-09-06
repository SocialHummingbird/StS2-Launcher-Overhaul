using Godot;

namespace STS2Mobile.Launcher;

// Keep the bridge/frame pump alive while blocking touch, keyboard, and shortcuts.
internal sealed partial class LauncherPreparationOverlay : CanvasLayer
{
    private SceneTree _tree;
    private bool _quitOnGoBack;
    internal Label Message { get; set; }

    public override void _EnterTree()
    {
        _tree = GetTree();
        _quitOnGoBack = _tree.QuitOnGoBack;
        _tree.QuitOnGoBack = false;
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_tree))
            _tree.QuitOnGoBack = _quitOnGoBack;
    }

    public override void _Input(InputEvent input)
    {
        if (Visible)
            GetViewport().SetInputAsHandled();
    }

    internal void Release()
    {
        if (!GodotObject.IsInstanceValid(this)) return;
        SetProcessInput(false);
        Hide();
        QueueFree();
    }
}
