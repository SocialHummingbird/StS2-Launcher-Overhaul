function Add-SteamVersionSelectionReleaseStatusDocsChecks {
    Add-Check `
        "docs\steam-version-selection-release-note-snippet.md" `
        "describes the current polished launcher portal UX alongside version-selection limitations" `
        @(
            "cleaner status-led portal",
            "titled action sections",
            "hidden Help & Reports drawer",
            "plain-language help-report and launcher-log status copy",
            "readable bounded compact diagnostics log viewport",
            "stronger branded header",
            "single-line compact brand",
            "readable compact brand subtitle",
            "plain-language readable compact brand subtitle",
            "responsive compact status headline row with stacked narrow-screen fallback",
            "viewport-aware compact status headline reflow after rotation or keyboard viewport changes",
            "stable one-line compact status detail row with short mobile copy, tap-to-expand full status, and a visible Details/Hide cue",
            "Android compact touch-scale floor for small-device readability",
            "Android-readable shader warmup/loading",
            "Android-readable post-launch startup status card",
            "responsive touch-safe compact sticky task header in a low-profile toolbar shell with a subdued inline current-task button and two-line workflow step cells using a shared 62px touch height",
            "contextual current-task detail labels",
            "readable stacked current-task row",
            "two-line single-row compact workflow strip on narrow screens",
            "viewport-aware sticky task header reflow after rotation or keyboard viewport changes",
            "viewport-aware compact task re-anchoring after rotation or keyboard viewport changes",
            "readable step-number badges",
            "Sign in with Steam / Android login",
            "Verify Code / Submit once",
            "Start Game / Ready version",
            "structured Play/Sync title/detail action labels",
            "Save Check / Get saves first",
            "Saves for / Get Steam saves before upload / Upload can overwrite Steam",
            "Get Steam Saves / Download to Android",
            "structured locked-Push title/detail labels",
            "Upload to Steam / Overwrite cloud",
            "Confirm Upload / Overwrite cloud",
            "Steam Cloud overwrite / Confirm only after Pull/local saves are verified",
            "responsive compact action rows",
            "responsive selected-version install summary",
            "touch-safe compact version dropdown popups",
            "inline compact install-version dropdown/refresh controls with structured title/detail labels",
            "version details with structured compact version-file drawer labels",
            "responsive ready-version summary",
            "compact ready-state priority that keeps the ready summary, Save Check shortcut, and Get-saves-first cloud controls before Start Game while moving version management below the primary launch path",
            "compact ready-state cloud options stay below Start Game as an optional save-settings drawer after Get-saves-first cloud controls",
            "large compact Android sign-in CTA with readable two-line",
            "responsive compact Steam Guard code controls",
            "viewport-aware compact Steam Guard code/action row reflow after rotation or keyboard viewport changes",
            "compact Steam Guard bounded two-line helper labels",
            "primary structured compact retry recovery",
            "structured compact startup recovery actions",
            "compact user-facing support tool labels such as Safe Start / Cloud off, Check Files / Updates, Game Versions / Refresh list, Repair Files / Rebuild game, Free Space / Old versions, Help Report / Share details, Last Problem / Open details, and Copy Log / Review first",
            "short-height copy on cramped landscape screens",
            "short-height copy reflow when the landscape height class changes",
            "keyboard-reduced usable height",
            "scroll-safe compact confirmations",
            "bounded compact quick-start guide panel",
            "collapsible quick-start guidance with structured compact Quick Start / Get saves first title/detail labels and bounded guide row cards that suppress during active compact task screens",
            "dense compact drawer toggles",
            "structured compact title/detail labels",
            "native fallback recovery screens that keep verbose diagnostics collapsed until requested and split actions into responsive rows on narrow landscape screens",
            "dense compact vertical rhythm",
            "single-row compact section headers with explicit short task cues such as Steam account, Current code, Local files, and Play safely",
            "mobile-first compact panel sizing with dense compact shell padding",
            "Steam sign-in/Steam Guard/install/play-sync sections",
            "Android/Samsung/password-manager suggestion behavior"
        )

    Add-Check `
        "docs\launcher-loading-screen-staging.md" `
        "documents Android-readable shader warmup staging" `
        @(
            "Android shader warmup uses the launcher compact touch-scale floor",
            "mobile-width compact panel",
            "styled percentage progress bar",
            "Android game startup status now uses a framed mobile-width status card",
            "Successful startup cleanup now frees the whole Android startup status root container"
        )

    Add-Check `
        "docs\release-and-backport-policy.md" `
        "requires release notes to name branch/version limitations" `
        @(
            "Steam beta/version selection proof",
            "audit-steam-version-selection\.ps1",
            "audit-steam-branch-guidance-parity\.ps1",
            "beta password/private branch behavior",
            "save compatibility across branches"
        )

    Add-Check `
        "docs\steam-version-selection-release-note-snippet.md" `
        "prevents release notes from overclaiming branch/version readiness" `
        @(
            "validation-stage",
            "Known limitations",
            "Do not say yet",
            "Refresh Game Versions",
            "dropdown-first",
            "password-manager suggestion behavior",
            "SteamKit debug logs are disabled by default",
            "sts2_steamkit_debug_logs=1",
            "wrapped selector guidance",
            "managed/native selector-guidance parity",
            "audit-steam-version-selection\.ps1",
            "audit-steam-branch-guidance-parity\.ps1",
            "Password-protected beta branches",
            "Steam Cloud Push is safe",
            "last_manual_cloud_push\.txt",
            "aggregate successful post-switch Push evidence",
            "bounded two-line Files for / Play version helper labels"
        )

    Add-Check `
        "docs\current-android-status.md" `
        "keeps Android status current for version selection, credential providers, and credential-log hardening" `
        @(
            "Steam game version selection is in hardening",
            "steam-version-selection-release-readiness\.md",
            "android-steam-login-validation\.md",
            "discovery-led dropdown Steam branch selector",
            "password-manager login behavior",
            "does not store or inject Steam passwords",
            "SteamKit debug logs are disabled by default",
            "sts2_steamkit_debug_logs=1",
            "native fallback keeps verbose diagnostics collapsed until requested",
            "structured compact startup recovery actions",
            "ARM64 device validation"
        )

    Add-Check `
        "README.md" `
        "advertises version selection as published but not release-candidate signed off" `
        @(
            "implemented for validation",
            "steam-version-selection-release-readiness\.md",
            "not release-candidate signed off",
            "discovery-led dropdown selector",
            "Refresh Game Versions",
            "public-inherited",
            "public-vs-beta integrity classification",
            "steam-beta-integrity-runtime-checklist\.md",
            "mixed beta/public behavior",
            "Autofill",
            "SteamKit debug logs are disabled by default",
            "Steam beta password entry",
            "Push backup evidence"
        )
}
