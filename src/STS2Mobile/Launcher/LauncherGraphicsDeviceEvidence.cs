using System;
using System.IO;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class LauncherGraphicsDeviceEvidence
{
    internal static bool CaptureAndApplyCompatibility(string dataDir)
    {
        if (!OperatingSystem.IsAndroid())
            return false;

        try
        {
            var adapterName = Sanitize(RenderingServer.GetVideoAdapterName());
            var adapterVendor = Sanitize(RenderingServer.GetVideoAdapterVendor());
            var renderingDriver = Sanitize(RenderingServer.GetCurrentRenderingDriverName());
            var renderingMethod = Sanitize(RenderingServer.GetCurrentRenderingMethod());
            var powerVr = IsPowerVr(adapterName, adapterVendor);
            var text =
                "StS2 Android graphics device\n"
                + $"UTC: {DateTime.UtcNow:O}\n"
                + $"Adapter name: {adapterName}\n"
                + $"Adapter vendor: {adapterVendor}\n"
                + $"Rendering driver: {renderingDriver}\n"
                + $"Rendering method: {renderingMethod}\n"
                + $"PowerVR compatibility required: {powerVr}\n";

            Directory.CreateDirectory(dataDir);
            File.WriteAllText(Path.Combine(dataDir, LauncherStorageNames.GraphicsDevice), text);
            PatchHelper.Log(
                $"Android graphics device recorded: adapter={adapterName} vendor={adapterVendor} "
                    + $"driver={renderingDriver} method={renderingMethod} powerVr={powerVr}"
            );

            if (powerVr && LauncherPreferences.ReadRendererMode() != LauncherRendererMode.OpenGl)
            {
                LauncherPreferences.SaveRendererMode(LauncherRendererMode.OpenGl);
                PatchHelper.Log(
                    "PowerVR compatibility selected OpenGL because reporter evidence shows Vulkan touch input is unreliable."
                );
            }

            return powerVr;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Android graphics device evidence unavailable: {ex.Message}");
            return false;
        }
    }

    internal static bool IsPowerVr(string adapterName, string adapterVendor)
    {
        var evidence = $"{adapterName}\n{adapterVendor}";
        return evidence.Contains("PowerVR", StringComparison.OrdinalIgnoreCase)
            || evidence.Contains("ImgTec", StringComparison.OrdinalIgnoreCase)
            || evidence.Contains("Imagination Technologies", StringComparison.OrdinalIgnoreCase);
    }

    private static string Sanitize(string value)
        => string.IsNullOrWhiteSpace(value)
            ? "<unknown>"
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
}
