function Add-SteamVersionSelectionReleaseDocsUserGuideSupportChecks {
    Add-Check `
        "docs\steam-version-selection-user-guide.md" `
        "keeps tester-facing support boundaries and version-selection setup visible" `
        @(
            "implemented for validation",
            "steam-version-selection-release-readiness\.md",
            "What is not supported yet",
            "Refresh Game Versions",
            "Steam login credential entry",
            "Android credential provider model",
            "Godot login field credential metadata configured",
            "Godot fields are native Android Autofill targets",
            "Password-manager suggestions device validated",
            "Native credential handoff result TTL seconds",
            "Android credential provider capability boundary",
            "blocked states",
            "Steam beta password entry",
            "Selected game version note",
            "Selected game version slot kind",
            "Selected game version slot directory",
            "wrapped helper text",
            "active install slot",
            "Selected Steam branch note before routing",
            "Selected branch note"
        )
}
