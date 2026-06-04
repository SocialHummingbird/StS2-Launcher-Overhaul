using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    private const string SteamGuardCodeFileName = "steam_guard_code.txt";

    private readonly struct LocalSteamGuardCode
    {
        private LocalSteamGuardCode(string value)
        {
            Value = value;
        }

        private string Value { get; }

        internal static LocalSteamGuardCode? Consume()
        {
            if (!OperatingSystem.IsAndroid())
                return null;

            var text = LauncherExternalFileInbox.ConsumeText(
                SteamGuardCodeFileName,
                "[Auth] Failed to consume local Steam Guard code file"
            );
            if (text == null)
                return null;

            if (!TryCreate(text, out var code))
            {
                PatchHelper.Log("[Auth] Ignored local Steam Guard code file with invalid shape");
                return null;
            }

            PatchHelper.Log("[Auth] Consumed local Steam Guard code file");
            return code;
        }

        internal string Text()
            => Value;

        private static bool TryCreate(string text, out LocalSteamGuardCode code)
        {
            var normalized = text.Trim().ToUpperInvariant();
            if (!IsValid(normalized))
            {
                code = default;
                return false;
            }

            code = new LocalSteamGuardCode(normalized);
            return true;
        }

        private static bool IsValid(string code)
        {
            if (code.Length != 5)
                return false;

            foreach (var ch in code)
            {
                if (!char.IsLetterOrDigit(ch))
                    return false;
            }

            return true;
        }
    }

    private static string? TryConsumeSteamGuardCode()
    {
        var code = LocalSteamGuardCode.Consume();
        return code.HasValue ? code.Value.Text() : null;
    }
}
