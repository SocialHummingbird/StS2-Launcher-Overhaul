using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    internal static void WriteMainMenuPreparation(
        string classification,
        params string[] details
    )
    {
        var evidence = MainMenuPreparation(Godot.OS.GetDataDir());
        try
        {
            evidence.WriteAllText(
                CreateTimestampedText(
                    "StS2 Launcher main-menu preparation",
                    "UTC",
                    sb =>
                    {
                        sb.AppendLine(
                            $"Classification: {CleanPostStartupValue(classification)}"
                        );
                        if (details == null)
                            return;

                        foreach (var detail in details)
                            sb.AppendLine(CleanPostStartupValue(detail));
                    }
                ).Build()
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Main-menu preparation evidence failed: {ex.Message}");
        }
    }
}
