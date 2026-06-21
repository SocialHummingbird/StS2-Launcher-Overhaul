function Add-SteamVersionSelectionLoginPortalEvidenceDocsAuthStatusChecks {
    Add-Check `
        "docs\android-login-portal-evidence-template.md" `
        "captures secret-safe login, credential, and status evidence" `
        @(
            "Do not use emulator evidence for signoff",
            "Steam username/email redacted",
            "Steam password absent",
            "Steam Guard code absent",
            "Native integrated Steam login panel opens automatically",
            "No USE ANDROID AUTOFILL popup/helper dialog visible",
            "Inline status guidance visible",
            "Native login panel remains usable when the keyboard is open",
            "Native login panel can scroll if keyboard or small screen reduces available height",
            "Native login panel keeps Sign in and Cancel reachable with the keyboard open",
            "Native login controls are stacked/full-width in portrait and use responsive wide rows in landscape",
            "Native login primary button says Sign in with Steam",
            "Native login action buttons render sentence-case labels instead of Android all-caps transformations",
            "Compact launcher sign-in shows Sign in with Steam before helper copy",
            "Compact launcher sign-in says Sign in with Steam / Android login",
            "Compact launcher sign-in uses a large primary Sign in with Steam CTA and a readable two-line password-manager safety helper",
            "Native login panel requests suggestions for username and password fields",
            "Native login panel requests suggestions again when username/password fields gain focus",
            "Native login Next control focuses password field",
            "Back/Cancel dismissal hides the soft keyboard before returning to launcher",
            "Provider does not prompt to save unverified credentials before Steam authentication",
            "Password visibility toggle shows/hides password without storing it",
            "Password visibility resets to hidden after submit/cancel/reopen",
            "Compact sign-in status says Sign in with Steam to continue instead of raw credential prompt",
            "Compact download-needed status says Download this game version to play and the next action reads Install Game",
            "Compact ready status says Ready to play this version and the next action reads Start Game",
            "Launcher compact plain-language status copy supported"
        )
}
