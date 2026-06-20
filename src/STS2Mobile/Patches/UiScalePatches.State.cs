using System;
using System.IO;
using Godot;

namespace STS2Mobile.Patches;

internal static partial class UiScalePatches
{
    internal static void EnsureUiScaleLoaded()
    {
        if (_uiScaleLoaded)
            return;
        _uiScaleLoaded = true;
        UiScalePercent = LoadUiScalePercent(UiScalePercent);
    }

    private static void SaveUiScale()
    {
        SaveUiScalePercent(UiScalePercent);
    }

    private static void ApplyScaledContentSize(Window window)
    {
        float scale = UiScalePercent / 100f;

        // Expand mode fills any screen ratio including near-square foldable displays.
        window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
        window.ContentScaleSize = new Vector2I(
            (int)Math.Round(BaseWidth / scale),
            (int)Math.Round(BaseHeight / scale)
        );
    }

    private static int LoadUiScalePercent(int fallback)
    {
        try
        {
            var path = ProjectSettings.GlobalizePath(ConfigPath);
            if (!File.Exists(path))
                return fallback;

            if (
                int.TryParse(File.ReadAllText(path).Trim(), out int value)
                && value >= MinPercent
                && value <= MaxPercent
            )
                return value;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"UI scale settings load failed: {ex.GetType().Name}: {ex.Message}");
        }

        return fallback;
    }

    private static void SaveUiScalePercent(int percent)
    {
        try
        {
            var path = ProjectSettings.GlobalizePath(ConfigPath);
            File.WriteAllText(path, percent.ToString());
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"UI scale settings save failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void ApplyUiScale()
    {
        EnsureUiScaleLoaded();
        try
        {
            var window = ((SceneTree)Engine.GetMainLoop()).Root;
            ApplyScaledContentSize(window);
            PatchHelper.Log($"UI Scale: {UiScalePercent}% -> ContentScaleSize {window.ContentScaleSize}");
            UiScaleChanged?.Invoke();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to apply UI scale: {ex.Message}");
        }
    }
}
