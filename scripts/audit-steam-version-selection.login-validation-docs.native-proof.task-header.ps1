function Add-SteamVersionSelectionLoginValidationDocsNativeProofTaskHeaderChecks {
    Add-Check `
        "docs\android-steam-login-validation.md" `
        "keeps destination navigation and section-header proof wording focused" `
        @(
            "Wide foldable and tablet layouts move the same five destinations into a top navigation row.",
            "Home contains the current status and primary launch or onboarding action; cloud controls remain in Saves, branch controls in Versions, mod controls in Mods, and recovery/diagnostics in Help.",
            "Selecting a destination resets that destination to a deterministic page origin; Android rotation and resize do not restore the removed task re-anchor behavior.",
            "Navigation and page content respect Android display safe-area insets, including the cover display cutout and bottom system area.",
            "Repeated destination changes and rotation settle to a complete frame without stale or partially retained launcher regions.",
            "Every visible destination control has an accessibility name, keyboard focus support where applicable, and a touch-safe target size.",
            "portal clearly exposes the next action",
            "Compact section headers keep title and readable task cue in one dense row without restoring bulky repeated subtitle cards",
            "Compact section headers use explicit short task cues such as .*Steam account.*, .*Current code.*, .*Local files.*, and .*Play safely.* instead of clipped desktop subtitle sentences"
        )
}
