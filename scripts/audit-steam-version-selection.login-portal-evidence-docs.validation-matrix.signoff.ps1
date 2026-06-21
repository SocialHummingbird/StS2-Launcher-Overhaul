function Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixSignoffChecks {
    Add-Check `
        "docs\android-login-portal-evidence-template.md" `
        "captures login portal evidence sanitization and release signoff boundaries" `
        @(
            "SteamKit debug logs sanitized for credentials/tokens",
            "Release signoff is not valid"
        )
}
