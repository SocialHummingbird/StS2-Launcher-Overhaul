using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    private const string SteamGuardCodeFileName = "steam_guard_code.txt";

    private static string? TryConsumeSteamGuardCode()
    {
        if (!OperatingSystem.IsAndroid())
            return null;

        var code = ConsumeSteamGuardCodeText();
        if (code == null)
            return null;

        if (!IsValidSteamGuardCode(code))
        {
            PatchHelper.Log("[Auth] Ignored local Steam Guard code file with invalid shape");
            return null;
        }

        PatchHelper.Log("[Auth] Consumed local Steam Guard code file");
        return code;
    }

    private static string? ConsumeSteamGuardCodeText()
    {
        var text = LauncherExternalFileInbox.ConsumeText(
            SteamGuardCodeFileName,
            "[Auth] Failed to consume local Steam Guard code file"
        );
        return text?.Trim().ToUpperInvariant();
    }

    private static bool IsValidSteamGuardCode(string code)
    {
        if (code == null || code.Length != 5)
            return false;

        foreach (var ch in code)
        {
            if (!char.IsLetterOrDigit(ch))
                return false;
        }

        return true;
    }
}
