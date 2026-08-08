function Add-SteamVersionSelectionSupportDocsEvidenceToolingBoundaryChecks {

    Add-Check `
        "scripts\audit-steam-version-selection.evidence-tooling.ps1" `
        "keeps Steam version-selection evidence tooling audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionEvidenceToolingChecks",
            "audit-steam-version-selection.evidence-tooling.capture.ps1",
            "audit-steam-version-selection.evidence-tooling.scaffold.ps1",
            "audit-steam-version-selection.evidence-tooling.redaction.ps1",
            "audit-steam-version-selection.evidence-tooling.docs.ps1",
            "Add-SteamVersionSelectionEvidenceToolingCaptureChecks",
            "Add-SteamVersionSelectionEvidenceToolingScaffoldChecks",
            "Add-SteamVersionSelectionEvidenceToolingRedactionChecks",
            "Add-SteamVersionSelectionEvidenceToolingDocsChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.evidence-tooling.capture.ps1" `
        "keeps adb resolution and device evidence capture audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionEvidenceToolingCaptureChecks",
            "android-adb-utils.ps1",
            "capture-steam-version-selection-evidence.ps1"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.evidence-tooling.scaffold.ps1" `
        "keeps evidence artifact scaffold and guidance parity audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionEvidenceToolingScaffoldChecks",
            "new-steam-version-selection-evidence.ps1",
            "audit-steam-branch-guidance-parity.ps1"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.evidence-tooling.redaction.ps1" `
        "keeps public evidence redaction export and review audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionEvidenceToolingRedactionChecks",
            "export-public-evidence-redaction.ps1",
            "review-public-evidence-redaction.ps1"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.evidence-tooling.docs.ps1" `
        "keeps evidence tooling documentation audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionEvidenceToolingDocsChecks",
            "steam-version-selection-tooling.md"
        )
}
