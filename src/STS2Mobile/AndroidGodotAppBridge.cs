using Godot;

namespace STS2Mobile;

internal static class AndroidGodotAppBridge
{
    private const string GetInstanceMethod = "getInstance";
    private const string GodotAppClass = "com.game.sts2launcher.GodotApp";
    private const string JavaClassWrapper = "JavaClassWrapper";
    private const string WrapMethod = "wrap";

    internal static void RestartApp() => GetInstance()?.Call("restartApp");

    internal static void LaunchGameOnRestart()
        => GetInstance()?.Call("launchGameOnRestart");

    internal static void LaunchGameSafelyOnRestart()
        => GetInstance()?.Call("launchGameSafelyOnRestart");

    internal static bool ShareTextFile(string path)
        => (bool)(GetInstance()?.Call("shareTextFile", path) ?? false);

    internal static string GetLogcatTail(int lineCount)
        => (string)GetInstance()?.Call("getLogcatTail", lineCount);

    internal static string GetExternalFilesDirPath()
        => (string)GetInstance()?.Call("getExternalFilesDirPath");

    internal static string GetVersionName()
        => (string)GetInstance()?.Call("getVersionName");

    internal static long GetUsableSpaceBytes(string path)
        => (long)(GetInstance()?.Call("getUsableSpaceBytes", path) ?? -1L);

    internal static bool TryGetInstance(out GodotObject godotApp)
    {
        try
        {
            godotApp = (GodotObject)GetGodotAppWrapper()
                .Call(GetInstanceMethod);
            return godotApp != null;
        }
        catch
        {
            godotApp = null;
            return false;
        }
    }

    private static GodotObject GetInstance() =>
        TryGetInstance(out var godotApp) ? godotApp : null;

    private static GodotObject GetGodotAppWrapper()
    {
        var javaClassWrapper = Engine.GetSingleton(JavaClassWrapper);
        return (GodotObject)javaClassWrapper.Call(
            WrapMethod,
            GodotAppClass
        );
    }
}
