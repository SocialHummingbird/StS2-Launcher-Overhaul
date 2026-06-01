using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private readonly struct LocalSteamCredentials
    {
        private LocalSteamCredentials(string username, string password)
        {
            Username = username;
            Password = password;
        }

        private string Username { get; }
        private string Password { get; }

        internal static LocalSteamCredentials Create(string username, string password)
            => new(username, password);

        internal Task LoginAsync(LauncherModel model)
            => model.LoginAsync(Username, Password);
    }

    private const string LocalSteamCredentialFileName = "steam_login_credentials.txt";

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
            return DecodeLocalSteamCredentials(lines);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Ignored local Steam credential file: {ex.Message}");
            return null;
        }
    }

    private static LocalSteamCredentials DecodeLocalSteamCredentials(string[] lines)
    {
        if (lines.Length < 2)
            throw new InvalidDataException("expected two base64 lines");

        var username = Encoding.UTF8.GetString(Convert.FromBase64String(lines[0].Trim())).Trim();
        var password = Encoding.UTF8.GetString(Convert.FromBase64String(lines[1].Trim()));

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            throw new InvalidDataException("username or password was empty");

        return LocalSteamCredentials.Create(username, password);
    }
}
