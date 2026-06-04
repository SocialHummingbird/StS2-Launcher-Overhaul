using System;
using System.IO;
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

        internal Task LoginAsync(LauncherModel model)
            => model.LoginAsync(Username, Password);

        internal static LocalSteamCredentials FromBase64Lines(string[] lines)
        {
            if (lines.Length < 2)
                throw new InvalidDataException("expected two base64 lines");

            var username = DecodeBase64Line(lines[0]).Trim();
            var password = DecodeBase64Line(lines[1]);

            return FromDecoded(username, password);
        }

        private static LocalSteamCredentials FromDecoded(
            string username,
            string password
        )
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                throw new InvalidDataException("username or password was empty");

            return new(username, password);
        }

        private static string DecodeBase64Line(string line)
            => Encoding.UTF8.GetString(Convert.FromBase64String(line.Trim()));
    }

    private static LocalSteamCredentials? ConsumeLocalSteamCredentials()
    {
        var lines = LauncherExternalFileInbox.ConsumeLines(
            LocalSteamCredentialFileName,
            "[Launcher] Ignored local Steam credential file"
        );
        if (lines == null)
            return null;

        try
        {
            return LocalSteamCredentials.FromBase64Lines(lines);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Ignored local Steam credential file: {ex.Message}");
            return null;
        }
    }
}
