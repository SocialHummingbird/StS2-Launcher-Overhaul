using Godot;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherClipboardText
{
    internal LauncherClipboardText(string text)
    {
        Text = text;
    }

    internal string Text { get; }
    internal int Length => Text.Length;

    internal void CopyToClipboard()
        => DisplayServer.ClipboardSet(Text);
}
