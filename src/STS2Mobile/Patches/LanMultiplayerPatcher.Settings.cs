using System;
using Godot;

namespace STS2Mobile.Patches;

internal static partial class LanMultiplayerPatcher
{
    private static string LoadLastIp()
    {
        try
        {
            var config = new ConfigFile();
            if (config.Load(LastIpConfigPath) == Error.Ok)
                return (string)config.GetValue(LastIpSection, LastIpKey, "");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to load LAN last IP config: {ex.Message}");
        }

        return "";
    }

    private static void SaveLastIp(string ip)
    {
        try
        {
            var config = new ConfigFile();
            config.SetValue(LastIpSection, LastIpKey, ip);
            config.Save(LastIpConfigPath);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to save LAN last IP config: {ex.Message}");
        }
    }
}
