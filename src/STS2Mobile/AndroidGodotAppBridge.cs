using Godot;

namespace STS2Mobile;

internal static class AndroidGodotAppBridge
{
    private const string GetInstanceMethod = "getInstance";
    private const string GodotAppClass = "com.game.sts2launcher.GodotApp";
    private const string HasStoragePermissionMethod = "hasStoragePermission";
    private const string JavaClassWrapper = "JavaClassWrapper";
    private const string RequestStoragePermissionMethod = "requestStoragePermission";
    private const string WrapMethod = "wrap";

    internal static void RestartApp() => CallVoid("restartApp");

    internal static void LaunchGameOnRestart() => CallVoid("launchGameOnRestart");

    internal static void LaunchGameSafelyOnRestart()
        => CallVoid("launchGameSafelyOnRestart");

    internal static bool ShareTextFile(string path)
        => AndroidBridgeDispatcher.Run(
            () => (bool)(GetInstanceOnCurrentThread()?.Call("shareTextFile", path) ?? false)
        );

    internal static string GetLogcatTail(int lineCount)
        => AndroidBridgeDispatcher.Run(
            () => (string)GetInstanceOnCurrentThread()?.Call("getLogcatTail", lineCount)
        );

    internal static string GetExternalFilesDirPath()
        => AndroidBridgeDispatcher.Run(
            () => (string)GetInstanceOnCurrentThread()?.Call("getExternalFilesDirPath")
        );

    internal static string GetVersionName()
        => AndroidBridgeDispatcher.Run(
            () => (string)GetInstanceOnCurrentThread()?.Call("getVersionName")
        );

    internal static long GetUsableSpaceBytes(string path)
        => AndroidBridgeDispatcher.Run(
            () => (long)(GetInstanceOnCurrentThread()?.Call("getUsableSpaceBytes", path) ?? -1L)
        );

    internal static bool HasStoragePermission()
        => AndroidBridgeDispatcher.Run(
            () => (bool)(GetInstanceOnCurrentThread()?.Call(HasStoragePermissionMethod) ?? false)
        );

    internal static void RequestStoragePermission()
        => CallVoid(RequestStoragePermissionMethod);

    internal static bool TryGetInstance(out GodotObject godotApp)
    {
        godotApp = AndroidBridgeDispatcher.Run(GetInstanceOnCurrentThread);
        return godotApp != null;
    }

    private static void CallVoid(string method, params Variant[] arguments)
        => AndroidBridgeDispatcher.Run(
            () =>
            {
                GetInstanceOnCurrentThread()?.Call(method, arguments);
                return true;
            }
        );

    private static GodotObject GetInstanceOnCurrentThread()
    {
        try
        {
            return (GodotObject)GetGodotAppWrapper()
                .Call(GetInstanceMethod);
        }
        catch
        {
            return null;
        }
    }

    private static GodotObject GetGodotAppWrapper()
    {
        var javaClassWrapper = Engine.GetSingleton(JavaClassWrapper);
        return (GodotObject)javaClassWrapper.Call(
            WrapMethod,
            GodotAppClass
        );
    }
}
