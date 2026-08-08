using STS2Mobile;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherSharedTextFile
{
    private LauncherSharedTextFile(string path, bool shared)
    {
        Path = path;
        Shared = shared;
    }

    internal string Path { get; }
    internal bool Shared { get; }

    internal static LauncherSharedTextFile Share(string path)
        => new(path, AndroidGodotAppBridge.ShareTextFile(path));

    internal string AndroidShareSheetLogMessage()
        => Shared
            ? "Android share sheet opened."
            : "Could not open Android share sheet.";
}
