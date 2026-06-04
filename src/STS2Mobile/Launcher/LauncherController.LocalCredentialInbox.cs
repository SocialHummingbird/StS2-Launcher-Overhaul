using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const string LocalSteamCredentialFileName = "steam_login_credentials.txt";

    private static (string username, string password)? ConsumeLocalSteamCredentials()
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

    private static (string username, string password) DecodeLocalSteamCredentials(
        string[] lines
    )
    {
        if (lines.Length < 2)
            throw new InvalidDataException("expected two base64 lines");

        var username = DecodeBase64Line(lines[0]).Trim();
        var password = DecodeBase64Line(lines[1]);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            throw new InvalidDataException("username or password was empty");

        return (username, password);
    }

    private static string DecodeBase64Line(string line)
        => Encoding.UTF8.GetString(Convert.FromBase64String(line.Trim()));
}
