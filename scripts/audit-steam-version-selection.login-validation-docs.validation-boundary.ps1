function Add-SteamVersionSelectionLoginValidationDocsValidationBoundaryChecks {
    Add-Check `
        "docs\android-steam-login-validation.md" `
        "keeps login validation boundary and remaining-risk documentation checks focused" `
        @(
            "Launcher startup fallback raw banner suppressed:",
            "Launcher portal UX device validated:",
            "Launcher portal UX validation boundary:",
            "SteamKit debug logs sanitized for credentials/tokens:",
            "portal scaling/readability/next-action clarity",
            "hidden diagnostics behavior",
            "not complete until ARM64 evidence covers"
        )
}
