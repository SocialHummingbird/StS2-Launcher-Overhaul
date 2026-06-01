using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    private static class SteamGuardCodeInbox
    {
        private const string FileName = "steam_guard_code.txt";

        private static string TryConsume()
        {
            if (!OperatingSystem.IsAndroid())
                return null;

            if (!TryReadCode(out var code))
                return null;

            if (!IsValid(code))
            {
                PatchHelper.Log("[Auth] Ignored local Steam Guard code file with invalid shape");
                return null;
            }

            PatchHelper.Log("[Auth] Consumed local Steam Guard code file");
            return code;
        }

        private static bool TryReadCode(out string code)
        {
            code = null;
            if (!LauncherExternalFileInbox.TryConsumeText(
                    FileName,
                    "[Auth] Failed to consume local Steam Guard code file",
                    out var text
                ))
                return false;

            code = text.Trim().ToUpperInvariant();
            return true;
        }

        private static bool IsValid(string code)
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
}
