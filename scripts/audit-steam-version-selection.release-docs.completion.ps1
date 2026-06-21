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
            "Manual Pull completed before Push",
            "Current important Android local save evidence count",
            "Baseline manual Push prerequisites satisfied",
            "Manual Push evidence marker filename",
            "last_manual_cloud_push_blocked\.txt",
            "Manual Push completed after branch switch for selected version with backup evidence",
            "full local pre-Push coverage",
            "full cloud pre-Push coverage",
            "marker backup counts/timestamps",
            "fail-before-upload",
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
