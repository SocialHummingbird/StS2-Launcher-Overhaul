namespace STS2Mobile.Launcher;

internal static class LauncherRendererMode
{
    internal const string Auto = "auto";
    internal const string Vulkan = "vulkan";
    internal const string OpenGl = "opengl";

    internal static string Normalize(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            Vulkan => Vulkan,
            OpenGl => OpenGl,
            _ => Auto,
        };
    }

    internal static string DisplayName(string value)
        => Normalize(value) switch
        {
            Vulkan => "Vulkan",
            OpenGl => "OpenGL",
            _ => "Auto",
        };
}
