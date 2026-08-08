using Godot;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherViewPrimaryBody
{
    internal LauncherViewPrimaryBody(ScrollContainer primaryScroll, VBoxContainer body)
    {
        PrimaryScroll = primaryScroll;
        Body = body;
    }

    internal ScrollContainer PrimaryScroll { get; }
    internal VBoxContainer Body { get; }
}
