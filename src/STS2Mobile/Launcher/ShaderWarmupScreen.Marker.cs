using System;
using System.IO;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static string MarkerPath =>
        Path.Combine(OS.GetUserDataDir(), LauncherStorageNames.ShaderWarmupVersion);

    internal static bool NeedsWarmup()
    {
        try
        {
            if (File.Exists(MarkerPath))
            {
                var content = File.ReadAllText(MarkerPath).Trim();
                if (content == WarmupVersion.ToString())
                {
                    PatchHelper.Log(Message.MarkerMatches(content));
                    return false;
                }
                PatchHelper.Log(Message.MarkerMismatch(content, WarmupVersion));
            }
            else
            {
                PatchHelper.Log(Message.MarkerMissing());
            }

            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log(Message.MarkerCheckFailed(ex));
            return true;
        }
    }

    private static void WriteWarmupVersion()
    {
        try
        {
            File.WriteAllText(MarkerPath, WarmupVersion.ToString());
        }
        catch (Exception ex)
        {
            PatchHelper.Log(Message.MarkerWriteFailed(ex));
        }
    }
}
