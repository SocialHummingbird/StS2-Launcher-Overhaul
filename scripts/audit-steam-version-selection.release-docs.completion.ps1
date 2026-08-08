function Add-SteamVersionSelectionReleaseDocsCompletionChecks {

    Add-Check `
        "docs\steam-version-selection-completion-audit.md" `
        "maps goal requirements to static and runtime evidence" `
        @(
            "Completion rule",
            "steam-version-selection-release-readiness\.md",
            "Requirement audit",
            "selected-version note",
            "account-visible Steam branch dropdown",
            "public/default always available",
            "Save-transfer protections",
            "one Pull/Upload operation",
            "exact SteamID64/runtime/mod-set context",
            "destination backups",
            "destination read-back hashes",
            "no interrupted Pull marker",
            "Controlled successful Pull and Upload using the same transfer implementation",
            "Runtime evidence still required",
            "Do not mark Steam beta/version selection release-ready yet"
        )

    Add-ForbiddenCheck `
        "docs\steam-version-selection-completion-audit.md" `
        "does not describe the old manual selector model" `
        @(
            "manual Steam branch entry",
            "no arbitrary discovery"
        )

}
