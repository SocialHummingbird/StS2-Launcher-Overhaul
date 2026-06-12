using Godot;

namespace STS2Mobile.Launcher;

internal static class LauncherAutofillSupport
{
    internal const bool AppStoresSteamPassword = false;
    internal const bool NativeAndroidAutofillOverlaySupported = true;
    internal const bool GodotFieldAutofillHintsConfigured = true;
    internal const int NativeDialogResultTtlSeconds = 60;
    internal const string ProviderModel = "Android/Samsung/password-manager Autofill only; launcher must not store or inject Steam passwords.";
    internal const string CurrentImplementation = "Godot-rendered login fields carry username/password metadata; Android can also show a native one-shot Autofill dialog with real username/password EditText controls. Filled values are held in memory only until consumed by the login flow, cancelled, or expired after 60 seconds.";
    internal const string UsernamePurpose = "username";
    internal const string PasswordPurpose = "password";

    internal static void ConfigureUsernameField(LineEdit field)
        => ConfigureField(field, "Steam Username", UsernamePurpose, secret: false);

    internal static void ConfigurePasswordField(LineEdit field)
        => ConfigureField(field, "Steam Password", PasswordPurpose, secret: true);

    private static void ConfigureField(LineEdit field, string label, string purpose, bool secret)
    {
        if (field == null)
            return;

        field.Name = label.Replace(" ", "");
        field.PlaceholderText = label;
        field.TooltipText = $"{label} ({purpose}); use Android/Samsung/password-manager Autofill when available.";
        field.Secret = secret;
        field.SetMeta("autofill_hint", purpose);
        field.SetMeta("credential_field", true);
        field.SetMeta("credential_storage_owner", "android_autofill_provider");
    }
}
