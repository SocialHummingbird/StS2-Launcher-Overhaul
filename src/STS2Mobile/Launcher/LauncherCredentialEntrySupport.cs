using Godot;

namespace STS2Mobile.Launcher;

internal static class LauncherCredentialEntrySupport
{
    internal static void ConfigureUsernameField(LineEdit field)
        => ConfigureField(field, "Steam Username", secret: false);

    internal static void ConfigurePasswordField(LineEdit field)
        => ConfigureField(field, "Steam Password", secret: true);

    private static void ConfigureField(LineEdit field, string label, bool secret)
    {
        if (field == null)
            return;

        field.Name = label.Replace(" ", "");
        field.PlaceholderText = label;
        field.Secret = secret;
    }
}
