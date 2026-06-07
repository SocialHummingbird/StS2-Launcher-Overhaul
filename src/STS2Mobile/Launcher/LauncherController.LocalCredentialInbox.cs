using System;
using System.Text;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const string LocalSteamCredentialFileName = "steam_login_credentials.txt";

    private readonly struct LocalSteamCredentials
    {
        private LocalSteamCredentials(string username, string password)
        {
            Username = username;
            Password = password;
        }

        private string Username { get; }
        private string Password { get; }

        internal static LocalSteamCredentials? Consume()
        {
            var lines = LauncherExternalFileInbox.ConsumeLines(
                LocalSteamCredentialFileName,
                "[Launcher] Ignored local Steam credential file"
            );
            if (lines == null)
                return null;

            if (!TryDecode(lines, out var credentials))
                return null;

            return credentials;
        }

        private static bool TryDecode(
            string[] lines,
            out LocalSteamCredentials credentials
        )
        {
            credentials = default;
            if (lines.Length < 2)
                return Invalid("expected two base64 lines");

            if (
                !TryDecodeBase64Line(lines[0], out var username)
                || !TryDecodeBase64Line(lines[1], out var password)
            )
                return Invalid("expected base64-encoded username and password");

            username = username.Trim();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                return Invalid("username or password was empty");

            credentials = new LocalSteamCredentials(username, password);
            return true;
        }

        private static bool TryDecodeBase64Line(string line, out string value)
        {
            value = null;
            try
            {
                value = Encoding.UTF8.GetString(Convert.FromBase64String(line.Trim()));
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool Invalid(string reason)
        {
            PatchHelper.Log($"[Launcher] Ignored local Steam credential file: {reason}");
            return false;
        }

        internal Task LoginAsync(
            LauncherModel model,
            Action<int> startTimeout
        )
            => model.LoginWithTimeoutAsync(Username, Password, startTimeout);
    }

    private static LocalSteamCredentials? ConsumeLocalSteamCredentials()
        => LocalSteamCredentials.Consume();
}
