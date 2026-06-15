using Godot;

namespace STS2Mobile.Launcher;

internal static class LauncherCredentialEntrySupport
{
    internal const bool AppStoresSteamPassword = false;
    internal const bool NativeCredentialHandoffPopupSupported = false;
    internal const bool NativeIntegratedCredentialPanelSupported = true;
    internal const bool NativeCredentialFieldsAutofillHintsConfigured = true;
    internal const bool SteamCredentialWebDomainConfigured = true;
    internal const bool NativeCredentialPanelInlineStatusConfigured = true;
    internal const bool NativeCredentialPanelKeyboardSafeLayoutConfigured = true;
    internal const bool NativeCredentialPanelImeInsetScrollSupported = true;
    internal const bool NativeCredentialPanelTouchTargetLayoutConfigured = true;
    internal const bool NativeCredentialPanelRequestsBothAutofillFields = true;
    internal const bool NativeCredentialPanelFocusAutofillRequestsSupported = true;
    internal const bool NativeCredentialPanelTaskLedButtonsSupported = true;
    internal const bool NativeCredentialPanelPasswordVisibilityToggleSupported = true;
    internal const bool NativeCredentialPanelPasswordFocusButtonSupported = true;
    internal const bool NativeCredentialPanelBackDismissSupported = true;
    internal const bool NativeCredentialPanelDismissRetrySupported = true;
    internal const bool NativeCredentialPanelDismissHidesKeyboardSupported = true;
    internal const bool NativeCredentialPanelSuppressesPreAuthSavePrompt = true;
    internal const bool SteamGuardOneShotCodeGuidanceSupported = true;
    internal const bool FailedLoginRetryGuidanceSupported = true;
    internal const bool ContextSpecificLoginRecoveryGuidanceSupported = true;
    internal const bool GodotFieldCredentialMetadataConfigured = true;
    internal const bool AndroidKeyboardCredentialHintsConfigured = true;
    internal const bool GodotFieldsAreNativeAndroidAutofillTargets = false;
    internal const bool PasswordManagerSuggestionsDeviceValidated = false;
    internal const int NativeCredentialHandoffResultTtlSeconds = 60;
    internal const string ProviderModel = "Integrated native Android credential panel on Android; Godot login fields remain the non-Android fallback. The launcher must not store or inject Steam passwords.";
    internal const string CurrentImplementation = "Android login uses an integrated in-app native credential panel with real username/password EditText fields, Android Autofill hints requested for both credential fields on panel open and again when each field gains focus, Steam web-domain metadata, accessible field labels, inline status/error guidance, keyboard-safe scrollable top-weighted layout with IME inset padding and focus scrolling, branded task-led full-width touch controls, next/done keyboard actions, a manual password visibility toggle that resets to hidden when fields clear, one-shot Steam Guard code guidance, context-specific failed-login and connection-recovery guidance, keyboard/focus cleanup on native panel dismiss, and clear no-password-storage copy before one-shot handoff into the existing SteamKit login flow. The old user-facing native credential popup is disabled. Native username/password fields and the managed fallback password field are cleared after submit/cancel/expiry, and pending native credential handoff values expire after 60 seconds if not consumed.";
    internal const string CapabilityBoundary = "Native Android credential fields can request Android/Samsung/Google password-manager suggestions, but provider behavior is device/provider dependent and requires ARM64 validation.";
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
        field.TooltipText = $"{label} ({purpose}); use keyboard or password-manager suggestions when available.";
        field.Secret = secret;
        field.SetMeta("credential_hint", purpose);
        field.SetMeta("credential_field", true);
        field.SetMeta("credential_storage_owner", "android_credential_provider");
    }
}
