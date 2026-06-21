function Add-SteamVersionSelectionLoginValidationDocsNativeProofTaskHeaderChecks {
    Add-Check `
        "docs\android-steam-login-validation.md" `
        "keeps compact current-task and section-header proof wording focused" `
        @(
            "The compact current-task bar stays reachable, uses app-like task title wording, and is touch-safe without wasting vertical space",
            "The compact current-task bar uses short title labels such as .*Sign in.*, .*Verify.*, .*Files.*, and .*Play.* without a status prefix",
            "The compact current-task bar uses contextual detail labels such as .*Steam account.*, .*Steam Guard code.*, .*Download version.*, and .*Play and saves.*.",
            "The compact current-task bar renders task names and contextual details as structured title/detail labels",
            "The compact inline current-task bar uses dense height while staying touch-safe, so the persistent header does not crowd active controls.",
            "The compact current-task bar and workflow strip share a tight sticky header instead of being separated as independent chrome rows.",
            "When width allows, the compact current-task bar and workflow strip share one inline sticky row, reducing header height while keeping controls readable and tappable.",
            "On narrow compact viewports, the stacked current-task row stays low-profile while remaining touch-safe.",
            "The compact sticky task header is grouped inside a low-profile toolbar shell so the persistent task controls read as one toolbar.",
            "On narrow compact viewports, the compact sticky task header stacks into a dense current-task row plus one dense workflow row instead of a two-row workflow grid.",
            "The compact sticky task header reflows between inline and stacked task/workflow layouts after Android rotation or keyboard viewport changes.",
            "The compact active task or last compact scroll target re-anchors after Android rotation or keyboard viewport changes without stealing focus from keyboard input fields.",
            "portal clearly exposes the next action",
            "Compact section headers keep title and readable task cue in one dense row without restoring bulky repeated subtitle cards",
            "Compact section headers use explicit short task cues such as .*Steam account.*, .*Current code.*, .*Local files.*, and .*Play safely.* instead of clipped desktop subtitle sentences"
        )
}
