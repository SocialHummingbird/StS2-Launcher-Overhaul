function Add-SteamVersionSelectionBranchEvidenceSelectorChecks {
    Add-Check `
        "docs\steam-version-selection-evidence-template.md" `
        "captures selector, branch-switch, and readiness evidence" `
        @(
            "Selector mode",
            "Branch discovery",
            "Android credential provider model",
            "Launcher stores Steam password for credential providers",
            "SteamKit debug logs opt-in status",
            "disabled by default",
            "Steam branch dropdown option metadata",
            "Static guardrails",
            "steam-version-selection-release-readiness\.md",
            "audit-steam-version-selection\.ps1",
            "audit-steam-branch-guidance-parity\.ps1",
            "blocked states",
            "Selector helper text shows the active install slot",
            "selected game version note",
            "selected game version slot kind",
            "Native pre-routing logs selected branch",
            "selected branch note",
            "branch switch marker filename",
            "Branch switch marker records",
            "parseable UTC",
            "local-backup posture",
            "previous branch",
            "Branch switch selected branch",
            "Branch switch selected version",
            "Branch switch selected version slot kind",
            "Branch switch selected version slot directory",
            "Branch switch selected branch matches current selected branch",
            "Branch switch selected branch note",
            "Branch switch local backup forced",
            "Branch switch warning acknowledged",
            "Branch switch non-public warning acknowledged",
            "Upload is eligible with transferable allowlisted local saves",
            "Upload is blocked while an interrupted Pull marker exists",
            "Steam game installation, branch-switch history, save-origin diagnostics, modded mode, and shared-storage permission do not control Upload eligibility",
            "Steam beta password",
            "save compatibility"
        )
}
