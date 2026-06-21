function Add-SteamVersionSelectionLoginValidationDocsNativeProofCompactActionChecks {
    Add-Check `
        "docs\android-steam-login-validation.md" `
        "keeps compact ready-state, cloud, sign-in, and recovery action proof wording focused" `
        @(
            "Compact recovery/tools actions use a two-column support grid when width allows and full-width stacked tools on narrow compact viewports.",
            "Compact Play and Sync shows the ready version, Save Check guidance, and Upload-locked state in a readable touch-safe summary card that opens Save Check without unlocking Push.",
            "Compact ready state prioritizes the ready summary, Save Check shortcut, Get-saves-first cloud controls, and Start Game before version management.",
            "Compact ready state keeps save backup and cloud sync options below Start Game as optional controls.",
            "Compact Pull action renders .*Get Steam Saves / Download to Android.* as a structured title/detail label",
            "Compact locked upload toggle renders Upload Locked / Review first and Hide Upload / Keep locked as structured title/detail labels:",
            "Compact unlocked Push actions render .*Upload to Steam / Overwrite cloud.* and .*Confirm Upload / Overwrite cloud.* as structured title/detail labels after the upload overwrite drawer is explicitly opened",
            "Compact armed Push warning says Steam Cloud overwrite / Confirm only after Pull/local saves are verified",
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
