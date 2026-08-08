function Add-SteamVersionSelectionLoginValidationDocsNativeProofCompactActionChecks {
    Add-Check `
        "docs\android-steam-login-validation.md" `
        "keeps compact ready-state, cloud, sign-in, and recovery action proof wording focused" `
        @(
            "Versions and Help keep recovery/support actions in their relevant destination with full-width touch targets on narrow compact viewports.",
            "Compact Home shows the ready version, Save Check guidance, Upload-locked state, Start Game, and Safe Start without exposing Push controls.",
            "Compact Saves keeps Pull and Upload direction-explicit and preserves the explicit overwrite confirmation without making Pull a prerequisite.",
            "Compact Saves keeps backup and cloud-sync options below the guarded Pull/Push actions.",
            "Compact Pull action renders .*Get Steam Saves / Download to Android.* as a structured title/detail label",
            "Compact locked upload toggle renders Upload Locked / Review first and Hide Upload / Keep locked as structured title/detail labels:",
            "Compact unlocked Push actions render .*Upload to Steam / Overwrite cloud.* and .*Confirm Upload / Overwrite cloud.* as structured title/detail labels after the upload overwrite drawer is explicitly opened",
            "Compact armed Push warning says Steam Cloud overwrite and asks the user to confirm the selected Android local-save context",
            "Compact Get Steam Saves and locked Steam upload share one two-button row when width allows and stack with Get Steam Saves first on narrow compact viewports.",
            "Compact Save Backup and Cloud Sync options use Local safety and Steam saves detail labels, share one low-profile row when width allows, and stack full-width on narrow compact viewports.",
            "Compact Android sign-in shows .*Sign in with Steam.* before password-manager helper copy",
            "Compact Android sign-in CTA renders .*Sign in with Steam / Android login.* as a structured title/detail label",
            "Compact Android sign-in uses a large primary .*Sign in with Steam.* CTA and a readable two-line password-manager safety helper",
            "Compact Steam Guard submit action renders .*Verify Code / Submit once.* as a structured title/detail label",
            "Compact Steam Guard retry keeps the rejected-code title short and moves latest-code guidance into the helper below the code controls",
            "Compact retry/failure state promotes .*Try Again.*Restart task.* primary recovery action while support tools remain secondary",
            "Compact launcher-log copy keeps the short .*Copy Log.* label but uses .*Review first.* detail text before copying diagnostics"
        )
}
