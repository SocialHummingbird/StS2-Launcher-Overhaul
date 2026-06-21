function Add-SteamVersionSelectionGovernanceChecks {
    Add-Check `
        ".github\workflows\steam-version-selection-static-audit.yml" `
        "runs the static audit in CI" `
        @(
            "Steam Version Selection Static Audit",
            "pull_request",
            "workflow_dispatch",
            "audit-steam-version-selection\.ps1",
            "audit-steam-branch-guidance-parity\.ps1"
        )

    Add-Check `
        ".github\workflows\overhaul-governance-ci.yml" `
        "requires Steam version-selection guardrail scaffolding" `
        @(
            "steam-version-selection-static-audit\.yml",
            "audit-steam-version-selection\.ps1",
            "audit-steam-branch-guidance-parity\.ps1",
            "steam-version-selection-validation\.md"
        )

    Add-Check `
        ".github\PULL_REQUEST_TEMPLATE.md" `
        "prompts reviewers to call out Steam version-selection risk" `
        @(
            "Steam version-selection static audit run",
            "Steam branch guidance parity audit run",
            "Steam version-selection risk",
            "steam_branch\.txt",
            "Pull-after-switch"
        )

    Add-Check `
        ".github\pull_request_template\pull_request_template.md" `
        "prompts reviewers to call out Steam version-selection risk" `
        @(
            "Steam version-selection static audit run",
            "Steam branch guidance parity audit run",
            "Steam version-selection risk",
            "steam_branch\.txt",
            "Pull-after-switch"
        )
}
