function Add-SteamVersionSelectionReleaseDocsUserGuideArtifactHygieneChecks {
    Add-Check `
        "docs\steam-version-selection-user-guide.md" `
        "keeps public artifact hygiene and release-readiness boundaries visible" `
        @(
            "Steam credentials",
            "refresh tokens",
            "shared preferences",
            "device identifiers",
            "Release readiness"
        )
}
