param(
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "static-audit-utils.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.launcher-shell.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-selector.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-runtime.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.native-routing.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-availability.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.download-workflows.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.session-auth.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.automation.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.local-login.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.confirmations.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-panel.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-labels.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.section-setup.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.safe-flow-guide.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-drawer.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-reporting.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.evidence-tooling.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-docs.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.beta-integrity.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-chrome.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.status-capsule.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-workflow.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.code-section.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-section-flow.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-install.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.startup-warmup.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.startup-recovery.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-section.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-ux-support.ps1")
Initialize-StaticAudit -ScriptRoot $PSScriptRoot -Quiet:$Quiet

Add-SteamVersionSelectionHelperBoundaryChecks

Add-SteamVersionSelectionLauncherShellChecks

Add-Check `
    "src\STS2Mobile\Steam\SteamConnectionConfigurationFactory.cs" `
    "sanitizes SteamKit debug logs before writing launcher diagnostics" `
    @(
        "SanitizeSteamKitDebugMessage",
        "SteamKitDebugLogsSanitized\s*=\s*true",
        "SteamKitDebugLogsOptInEnabled",
        "STS2_STEAMKIT_DEBUG_LOGS",
        "disabled by default",
        "SensitiveJsonValueRegex",
        "SensitiveKeyValueRegex",
        "BearerTokenRegex",
        "<redacted>",
        "PatchHelper\.Log",
        "SteamKit"
    )

Add-SteamVersionSelectionBranchSelectorChecks

Add-SteamVersionSelectionBranchRuntimeChecks

Add-SteamVersionSelectionNativeRoutingChecks

Add-SteamVersionSelectionBranchAvailabilityChecks

Add-SteamVersionSelectionDownloadWorkflowChecks

Add-SteamVersionSelectionSessionAuthChecks

Add-SteamVersionSelectionAutomationChecks

Add-SteamVersionSelectionLocalLoginChecks

Add-SteamVersionSelectionConfirmationChecks

Add-SteamVersionSelectionCloudSafetyChecks

Add-SteamVersionSelectionLoginPanelChecks

Add-SteamVersionSelectionCompactLabelChecks

Add-SteamVersionSelectionSectionSetupChecks

Add-SteamVersionSelectionStatusCapsuleChecks

Add-SteamVersionSelectionSafeFlowGuideChecks

Add-SteamVersionSelectionDiagnosticsDrawerChecks

Add-SteamVersionSelectionDiagnosticsReportingChecks

Add-SteamVersionSelectionEvidenceToolingChecks

Add-SteamVersionSelectionReleaseDocsChecks

Add-SteamVersionSelectionBetaIntegrityChecks

Add-SteamVersionSelectionPortalChromeChecks

Add-SteamVersionSelectionCompactWorkflowChecks


Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
    "suppresses compact first-run safe-flow guidance during active auth states" `
    @(
        "SetFirstRunGuideVisible\(false\)",
        "SetLoginFormVisible\(bool visible, bool disabled\)[\s\S]*SetFirstRunGuideVisible\(false\)[\s\S]*HideCompactCompletedAuthSections",
        "ShowCodePrompt\(bool wasIncorrect\)[\s\S]*SetFirstRunGuideVisible\(false\)",
        "SetLoginFormVisible",
        "FirstRunGuide\.Visible = !_profile\.Compact \|\| visible"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
    "suppresses compact first-run safe-flow guidance during active download states" `
    @(
        "ShowDownloadAction\(string buttonText\)[\s\S]*SetFirstRunGuideVisible\(false\)",
        "ShowDownloadAction"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
    "restores compact setup guidance only when no active action section is shown" `
    @(
        "SetFirstRunGuideVisible\(true\)",
        "SetFirstRunGuideVisible\(false\)",
        "ShowLaunchActions",
        "ShowRetry",
        "HideActions"
    )

Add-SteamVersionSelectionCodeSectionChecks


Add-SteamVersionSelectionCompactSectionFlowChecks

Add-SteamVersionSelectionCompactInstallChecks


Add-SteamVersionSelectionStartupWarmupChecks

Add-SteamVersionSelectionStartupRecoveryChecks


Add-SteamVersionSelectionActionSectionChecks

Add-SteamVersionSelectionPortalUxSupportChecks

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.FmodAttribution.cs" `
    "keeps compact FMOD attribution low-profile without expanding the phone layout" `
    @(
        "BuildFmodAttributionSection\(float scale, bool compact\)",
        "Control\.SizeFlags\.ShrinkBegin",
        "Control\.SizeFlags\.ExpandFill",
        "if \(!compact\)",
        "CompactFmodCreditFontSize",
        "AutowrapMode"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.cs" `
    "refreshes keyboard offset anchor after viewport size changes" `
    @(
        "_panelBaseY = _panel\.Position\.Y \+ _keyboardOffset",
        "_panel\.UpdateSizeFromViewport",
        "UpdateKeyboardOffset\(\)",
        "ReanchorCompactScrollTargetAfterViewportChange\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.Keyboard.cs" `
    "updates Android keyboard offset and panel position from the visible viewport" `
    @(
        "UpdateKeyboardOffset\(\)",
        "DisplayServer\.VirtualKeyboardGetHeight\(\)",
        "_keyboardOffset = Math\.Min",
        "_panelBaseY - _keyboardOffset",
        "_keyboardOffset = 0f",
        "_panelBaseY"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.Keyboard.cs" `
    "scrolls focused managed inputs above the Android keyboard" `
    @(
        "ScrollFocusedInputAboveKeyboard",
        "GuiGetFocusOwner",
        "PrimaryScroll\.IsAncestorOf\(focusOwner\)",
        "PrimaryScroll\.EnsureControlVisible\(focusOwner\)",
        "GodotObject\.IsInstanceValid\(focusOwner\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.cs" `
    "tracks mutable keyboard offset state for viewport-aware Android keyboard avoidance" `
    @(
        "private float _panelBaseY",
        "private float _keyboardOffset",
        "private Control _keyboardFocusScrollTarget",
        "private float _keyboardFocusScrollOffset"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Components\StyledPanel.cs" `
    "uses a framed launcher shell rather than a flat unbounded panel" `
    @(
        "PanelBackground",
        "BorderColor",
        "SetBorderWidthAll",
        "PanelRadius"
    )

Add-Check `
    "src\STS2Mobile\ModEntry.StandaloneLauncher.cs" `
    "suppresses raw startup fallback banner behind the launcher portal" `
    @(
        "Startup fallback raw banner suppressed",
        "launcher diagnostics retain the startup failure detail"
    )







Add-Check `
    "android\src\com\game\sts2launcher\GodotApp.java" `
    "keeps SteamKit debug logging opt-in at the Android boundary" `
    @(
        "ENV_STEAMKIT_DEBUG_LOGS",
        "sts2_steamkit_debug_logs",
        "setSteamKitDebugLogMode",
        "Sanitized SteamKit debug logging enabled",
        "STS2_STEAMKIT_DEBUG_LOGS"
    )

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

Add-Check `
    "docs\steam-version-selection-evidence-template.md" `
    "captures branch validation evidence" `
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
        "required-evidence status",
        "Branch switch previous branch",
        "Branch switch selected branch",
        "Branch switch selected version",
        "Branch switch selected version slot kind",
        "Branch switch selected version slot directory",
        "Branch switch selected branch matches current selected branch",
        "Branch switch selected branch note",
        "Branch switch local backup forced",
        "Branch switch manual Push requires backup storage",
        "Branch switch warning acknowledged",
        "Branch switch non-public warning acknowledged",
        "Branch switch marker has required safety evidence",
        "Branch switch marker has required safety evidence for selected branch",
        "Branch-switch manual Push prerequisites satisfied",
        "Pre-Push local backup evidence count",
        "Pre-Push cloud backup evidence count",
        "Latest pre-Push local backup UTC",
        "Latest pre-Push cloud backup UTC",
        "Pre-Push local backup evidence after branch switch",
        "Pre-Push cloud backup evidence after branch switch",
        "Branch-switch pre-Push backup evidence satisfied",
        "Manual Pull evidence marker filename",
        "Manual Push evidence marker filename",
        "last_manual_cloud_push_blocked\.txt",
        "Manual Pull evidence marker filename",
        "Manual Pull evidence marker path",
        "Manual Pull evidence UTC",
        "Manual Pull evidence UTC parseable",
        "Manual Pull evidence selected branch",
        "Manual Pull evidence selected version",
        "Manual Pull evidence selected version slot kind",
        "Manual Pull evidence selected version slot directory",
        "Manual Pull completion flag recorded",
        "Manual Pull completed before Push",
        "Manual Pull evidence is after branch switch",
        "Manual Pull evidence matches selected branch",
        "Manual Pull completed after branch switch",
        "Current important Android local save evidence count",
        "Current important Android local save evidence present",
        "Baseline manual Push prerequisites satisfied",
        "Manual Push evidence marker filename",
        "Manual Push evidence marker path",
        "Manual Push evidence UTC",
        "Latest manual Push evidence outcome",
        "Latest manual Push evidence UTC",
        "Latest manual Push evidence selected branch",
        "Latest manual Push evidence selected version",
        "Latest manual Push evidence selected version slot kind",
        "Latest manual Push evidence selected version slot directory",
        "Latest manual Push evidence reason",
        "Manual Push evidence UTC parseable",
        "Manual Push evidence selected branch",
        "Manual Push evidence selected version",
        "Manual Push evidence selected version slot kind",
        "Manual Push evidence selected version slot directory",
        "Manual Push evidence recorded local backup count",
        "Manual Push evidence recorded cloud backup count",
        "Manual Push evidence recorded latest local backup UTC",
        "Manual Push evidence recorded latest cloud backup UTC",
        "Manual Push evidence recorded important local save evidence count",
        "Manual Push evidence recorded baseline prerequisites satisfied",
        "Manual Push completion flag recorded",
        "Manual Push evidence is after branch switch",
        "Manual Push evidence matches selected branch",
        "Manual Push evidence recorded pre-Push backup evidence satisfied",
        "Manual Push completed after branch switch for selected version with backup evidence",
        "Manual Push blocked evidence marker filename",
        "Manual Push blocked evidence marker path",
        "Manual Push blocked evidence UTC",
        "Manual Push blocked evidence UTC parseable",
        "Manual Push blocked evidence selected branch",
        "Manual Push blocked evidence selected version",
        "Manual Push blocked evidence selected version slot kind",
        "Manual Push blocked evidence selected version slot directory",
        "Manual Push blocked evidence matches selected branch",
        "Manual Push blocked evidence recorded prerequisites satisfied",
        "Manual Push blocked evidence recorded local backup count",
        "Manual Push blocked evidence recorded cloud backup count",
        "Manual Push blocked evidence recorded latest local backup UTC",
        "Manual Push blocked evidence recorded latest cloud backup UTC",
        "Manual Push blocked evidence recorded important local save evidence count",
        "Manual Push blocked evidence recorded baseline prerequisites satisfied",
        "Manual Push blocked evidence recorded pre-Push backup evidence satisfied",
        "Manual Push blocked evidence reason",
        "Manual Push blocked before upload evidence recorded",
        "Early branch-switch Push gate blocks",
        "local/cloud pre-Push backup counts",
        "latest local/cloud backup UTC",
        "every important Android local save",
        "every existing important Steam Cloud save",
        "selected-cache-preserved aggregate",
        "Steam beta password",
        "save compatibility",
        "Artifact hygiene",
        "Steam credentials",
        "refresh tokens",
        "shared preferences",
        "device identifiers",
        "Raw full logcat was omitted by default",
        "IncludeRawLogcat",
        "sts2_steamkit_debug_logs",
        "logcat-steam-version-focused-redacted\.txt",
        "best-effort redacted file was manually reviewed before posting",
        "Redacted focused logcat includes its best-effort/manual-review warning header",
        "ARTIFACT_HYGIENE\.txt",
        "raw logs are treated as local-only",
        "PUBLIC_SHARE_MANIFEST\.txt",
        "preferred public artifacts",
        "logcat-redaction-summary\.txt",
        "focused-line and changed-line counts",
        "launcher-diagnostics-index\.txt",
        "full launcher diagnostics report attached publicly was manually reviewed/redacted",
        "Full launcher diagnostics and startup-recovery diagnostics reports include a public-sharing warning"
    )


Add-Check `
    "docs\android-steam-login-validation.md" `
    "defines Android Steam login validation proof contract" `
    @(
        "android-login-portal-evidence-template\.md",
        "integrated in-app native Steam credential panel",
        "real Android username and password fields",
        "no user-facing native credential handoff popup",
        "does not store or inject Steam passwords",
        "real `EditText` fields",
        "Android Autofill hints",
        "Steam web-domain metadata",
        "launcher portal uses explicit status and titled state sections",
        "Diagnostics are hidden behind the Help & Reports drawer",
        "Android warmup/loading uses a mobile-width compact panel with readable styled percentage progress.",
        "Android post-launch startup status uses a framed mobile-width readable card after the launcher closes.",
        "Native fallback keeps verbose diagnostics collapsed until explicitly requested",
        "Native fallback recovery actions split into two touch-friendly rows on narrow landscape screens.",
        "Startup recovery compact actions render as structured user-facing controls: .*Restart App.*Open launcher.*Safe Start.*Cloud off.*Help Report.*Share details.*Copy Log.*Review first.*Hide Help.*Keep waiting",
        "The compact diagnostics toggle uses a touch-safe two-line detail label without exposing diagnostics by default.",
        "Compact diagnostics lives inside the scroll body rather than consuming fixed phone viewport chrome, and explicit diagnostics actions scroll to it.",
        "The compact status card stays readable while using low-profile spacing so more current task content remains visible.",
        "The compact status card stacks the phase chip and guided next action so neither is squeezed on narrow screens.",
        "Compact status details keep normal progress text to one stable line while attention/failure guidance can expand.",
        "Compact status uses short mobile detail copy and expands full raw status when tapped.",
        "Compact status exposes a visible `Details` / `Hide` cue in a touch-safe row.",
        "Compact sign-in status says `Sign in with Steam to continue` instead of exposing the raw credential prompt.",
        "Compact download-needed status says `Download this game version to play` and the next action reads `Install Game`.",
        "Compact ready status says `Ready to play this version` and the next action reads `Start Game`.",
        "Compact quick-start guidance starts collapsed behind a touch-safe two-line toggle that says `Quick Start` and `Get saves first`.",
        "Compact quick-start toggle renders Quick Start / Get saves first as structured title/detail labels",
        "Compact expanded quick-start guide renders each step inside a bounded row card.",
        "Compact sign-in, Steam Guard, and download task screens suppress the quick-start drawer so the current primary controls stay higher in the viewport.",
        "The compact brand subtitle remains readable at phone scale and uses plain app copy instead of pipe-separated command-line-style labels.",
        "The compact workflow strip stays in one dense row on narrow compact viewports instead of taking a second fixed header row.",
        "Tapping compact workflow step labels scrolls directly to the visible matching task section or the current safe fallback task.",
        "Compact recovery/tools actions use a two-column support grid when width allows and full-width stacked tools on narrow compact viewports.",
        "Compact Play and Sync shows the ready version, Save Check guidance, and Upload-locked state in a readable touch-safe summary card that opens Save Check without unlocking Push.",
        "Compact ready state prioritizes the ready summary, Save Check shortcut, Get-saves-first cloud controls, and Start Game before version management.",
        "Compact ready state keeps save backup and cloud sync options below Start Game as optional controls.",
        "Compact Pull action renders .*Get Steam Saves / Download to Android.* as a structured title/detail label",
        "Compact locked upload toggle renders Upload Locked / Review first and Hide Upload / Keep locked as structured title/detail labels:",
        "Compact unlocked Push actions render .*Upload to Steam / Overwrite cloud.* and .*Confirm Upload / Overwrite cloud.* as structured title/detail labels after the upload overwrite drawer is explicitly opened",
        "Compact armed Push warning says Steam Cloud overwrite / Confirm only after Pull/local saves are verified",
        "Compact Get Steam Saves and locked Steam upload share one two-button row when width allows and stack with Get Steam Saves first on narrow compact viewports.",
        "Compact Save Backup and Cloud Sync options use Local safety and Steam saves detail labels, share one low-profile row when width allows, and stack full-width on narrow compact viewports.",
        "Compact Android sign-in shows .*Sign in with Steam.* before password-manager helper copy",
        "Compact Android sign-in CTA renders .*Sign in with Steam / Android login.* as a structured title/detail label",
        "Compact Android sign-in uses a large primary .*Sign in with Steam.* CTA and a readable two-line password-manager safety helper",
        "Compact Steam Guard submit action renders .*Verify Code / Submit once.* as a structured title/detail label",
        "Compact Steam Guard retry keeps the rejected-code title short and moves latest-code guidance into the helper below the code controls",
        "Compact retry/failure state promotes .*Try Again.*Restart task.* primary recovery action while support tools remain secondary",
        "Compact launcher-log copy keeps the short .*Copy Log.* label but uses .*Review first.* detail text before copying diagnostics",
        "The compact current-task bar stays reachable, uses app-like task title wording, and is touch-safe without wasting vertical space",
        "The compact current-task bar uses short title labels such as .*Sign in.*, .*Verify.*, .*Files.*, and .*Play.* without a status prefix",
        "The compact current-task bar uses contextual detail labels such as .*Steam account.*, .*Steam Guard code.*, .*Download version.*, and .*Play and saves.*.",
        "The compact current-task bar renders task names and contextual details as structured title/detail labels",
        "The compact inline current-task bar uses dense height while staying touch-safe, so the persistent header does not crowd active controls.",
        "The compact current-task bar and workflow strip share a tight sticky header instead of being separated as independent chrome rows.",
        "When width allows, the compact current-task bar and workflow strip share one inline sticky row, reducing header height while keeping controls readable and tappable.",
        "On narrow compact viewports, the stacked current-task row stays low-profile while remaining touch-safe.",
        "The compact sticky task header is grouped inside a low-profile toolbar shell so the persistent task controls read as one toolbar.",
        "On narrow compact viewports, the compact sticky task header stacks into a dense current-task row plus one dense workflow row instead of a two-row workflow grid.",
        "The compact sticky task header reflows between inline and stacked task/workflow layouts after Android rotation or keyboard viewport changes.",
        "The compact active task or last compact scroll target re-anchors after Android rotation or keyboard viewport changes without stealing focus from keyboard input fields.",
        "portal clearly exposes the next action",
        "Compact section headers keep title and readable task cue in one dense row without restoring bulky repeated subtitle cards",
        "Compact section headers use explicit short task cues such as .*Steam account.*, .*Current code.*, .*Local files.*, and .*Play safely.* instead of clipped desktop subtitle sentences",
        "Password-manager suggestions",
        "Steam Guard",
        "Failed login",
        "Successful return",
        "Native integrated credential panel supported:",
        "Native credential fields Autofill hints configured:",
        "Steam credential web domain configured:",
        "Native credential panel inline status configured:",
        "Native credential panel keyboard-safe layout configured:",
        "Native credential panel IME inset scroll supported:",
        "Native credential panel touch-target layout configured:",
        "Launcher Android-readable warmup screen supported:",
        "Launcher Android-readable startup status card supported:",
        "Launcher native fallback diagnostics collapsed by default:",
        "Launcher native fallback responsive recovery rows supported:",
        "Launcher startup recovery structured compact actions supported:",
        "Native credential panel requests both Autofill fields:",
        "Native credential panel focus Autofill requests supported:",
        "Native credential panel task-led buttons supported:",
        "Native credential panel responsive action rows supported:",
        "Native credential panel orientation reflow supported:",
        "Native credential panel short-height copy supported:",
        "Native credential panel short-height reflow supported:",
        "Native credential panel IME height reflow supported:",
        "Native credential panel password visibility toggle supported:",
        "Native credential panel password-focus button supported:",
        "Native credential panel Back dismiss supported:",
        "Native credential panel dismiss retry supported:",
        "Native credential panel dismiss hides keyboard:",
        "Native credential panel suppresses pre-auth save prompt:",
        "Steam Guard one-shot code guidance supported:",
        "Failed-login retry guidance supported:",
        "Context-specific login recovery guidance supported:",
        "Native credential handoff popup supported:",
        "Password-manager suggestions device validated:",
        "Launcher portal UX model:",
        "Launcher status-led portal supported:",
        "Launcher phase-labeled status supported:",
        "Launcher structured status chip supported:",
        "Launcher guided next-action status supported:",
        "Launcher error-first guided status supported:",
        "Launcher compact plain-language status copy supported:",
        "Launcher titled state sections supported:",
        "Launcher safe first-run guidance supported:",
        "Launcher compact safe-flow guidance collapsible:",
        "Launcher compact low-profile safe-flow toggle supported:",
        "Launcher compact safe-flow toggle detail labels supported:",
        "Launcher compact structured safe-flow toggle labels supported:",
        "Launcher compact safe-flow bounded guide supported:",
        "Launcher compact active-task safe-flow suppression supported:",
        "Launcher mobile-first compact layout supported:",
        "Launcher compact dense panel padding supported:",
        "Launcher compact dense vertical rhythm supported:",
        "Launcher rounded scaled metrics supported:",
        "Launcher Android compact touch-scale floor supported:",
        "Launcher compact dynamic content width supported:",
        "Launcher tablet/wide content layout supported:",
        "Launcher top-anchored portal content supported:",
        "Launcher compact vertical status hero supported:",
        "Launcher compact low-profile status card supported:",
        "Launcher compact status headline row supported:",
        "Launcher compact stacked status headline supported:",
        "Launcher viewport-aware compact status headline reflow supported:",
        "Launcher compact stable status detail row supported:",
        "Launcher compact short status details supported:",
        "Launcher compact sticky workflow step strip supported:",
        "Launcher compact low-profile workflow step strip supported:",
        "Launcher compact low-profile two-column workflow step strip supported:",
        "Launcher compact workflow step direct navigation supported:",
        "Launcher compact narrow workflow single-row supported:",
        "Launcher compact visible workflow step labels supported:",
        "Launcher compact workflow step detail labels supported:",
        "Launcher compact workflow step number badges supported:",
        "Launcher compact workflow unified touch height supported:",
        "Launcher compact current-task jump supported:",
        "Launcher compact sticky current-task bar supported:",
        "Launcher compact low-profile current-task bar supported:",
        "Launcher compact dense inline current-task bar supported:",
        "Launcher compact current-task shared touch height supported:",
        "Launcher compact low-profile stacked current-task bar supported:",
        "Launcher compact current-task context labels supported:",
        "Launcher compact structured current-task labels supported:",
        "Launcher compact touch-safe sticky header controls supported:",
        "Launcher compact grouped sticky task header supported:",
        "Launcher compact sticky task toolbar shell supported:",
        "Launcher compact inline sticky task header supported:",
        "Launcher compact responsive sticky task header supported:",
        "Launcher viewport-aware sticky task header reflow supported:",
        "Launcher viewport-aware compact task re-anchor supported:",
        "Launcher compact dense sticky task header supported:",
        "Launcher compact task-jump navigation labels supported:",
        "Launcher compact padded scroll anchors supported:",
        "Launcher keyboard-focused input scroll supported:",
        "Launcher compact contextual confirmation labels supported:",
        "Launcher compact scroll-safe confirmation dialogs supported:",
        "Launcher viewport-aware confirmation dialogs supported:",
        "Launcher compact Steam Guard large input supported:",
        "Launcher compact Steam Guard action-first layout supported:",
        "Launcher compact Steam Guard inline action row supported:",
        "Launcher compact responsive Steam Guard action layout supported:",
        "Launcher viewport-aware compact Steam Guard action row reflow supported:",
        "Launcher compact Steam Guard submit detail label supported:",
        "Launcher compact Steam Guard retry guidance supported:",
        "Launcher compact Steam Guard bounded helper supported:",
        "Launcher compact primary retry action supported:",
        "Launcher compact structured retry action labels supported:",
        "Launcher compact primary login action first supported:",
        "Launcher compact Android login primary CTA supported:",
        "Launcher compact Android login detail label supported:",
        "Launcher compact Android login helper detail label supported:",
        "Launcher compact completed-auth section suppression supported:",
        "Launcher touch-first action targets supported:",
        "Launcher primary action wording supported:",
        "Launcher consistent Start Game CTA supported:",
        "Launcher compact launch detail label supported:",
        "Launcher branded atmospheric background supported:",
        "Launcher branded background explicit RGBA supported:",
        "Launcher high-contrast rounded actions supported:",
        "Launcher compact header chrome reduction supported:",
        "Launcher compact condensed brand header supported:",
        "Launcher compact single-line brand header supported:",
        "Launcher compact readable brand subtitle supported:",
        "The compact brand subtitle remains readable at phone scale and uses plain app copy instead of pipe-separated command-line-style labels.",
        "Launcher compact section-header subtitle suppression supported:",
        "Launcher compact low-profile section headers supported:",
        "Launcher compact single-row section headers supported:",
        "Launcher compact section-header task cues supported:",
        "Launcher compact readable section-header cues supported:",
        "Launcher compact explicit section-header cues supported:",
        "Launcher compact install primary detail label supported:",
        "Launcher compact download progress hero supported:",
        "Launcher compact download progress status label supported:",
        "Launcher compact readable download progress bar supported:",
        "Launcher compact inline install-version controls supported:",
        "Launcher compact version details collapsible:",
        "Launcher compact version drawer bounded help label supported:",
        "Launcher compact structured install-version action labels supported:",
        "Launcher compact version summary cards supported:",
        "Launcher compact selected-version summary shortcut supported:",
        "Launcher compact selected-version headline supported:",
        "Launcher compact responsive selected-version summary supported:",
        "Launcher compact ready-version summary panel supported:",
        "Launcher compact ready-version summary shortcut supported:",
        "Launcher compact ready-version headline supported:",
        "Launcher compact responsive ready-version summary supported:",
        "Launcher compact structured Play/Sync action labels supported:",
        "Launcher compact ready-state install-section suppression supported:",
        "Launcher compact touch-safe version dropdown supported:",
        "Launcher compact touch-safe dropdown popup supported:",
        "Launcher compact cloud-safety guidance collapsible:",
        "Launcher compact cloud-safety detail label supported:",
        "Launcher compact cloud options collapsible:",
        "Launcher primary cloud actions before cloud options:",
        "Launcher compact cloud option detail labels supported:",
        "Launcher safer Pull-before-Push cloud ordering supported:",
        "Launcher compact cloud direction labels supported:",
        "Launcher compact cloud primary actions row supported:",
        "Launcher compact Pull detail label supported:",
        "Launcher compact responsive action rows supported:",
        "Launcher compact dangerous Push detail labels supported:",
        "Launcher compact armed Push warning detail label supported:",
        "Launcher manual Push armed overwrite warning supported:",
        "Launcher compact cloud options row supported:",
        "Launcher version-install/cloud-save separation guidance supported:",
        "Launcher Help & Reports drawer hidden by default:",
        "Launcher Help & Reports drawer auto-opens for diagnostics actions:",
        "Compact diagnostics toggle renders .*Help & Reports.*Private until opened.*structured title/detail labels",
        "Launcher compact low-profile drawer toggles supported:",
        "Launcher compact dense drawer toggle height supported:",
        "Launcher compact support tools grid supported:",
        "Launcher compact launcher-log review label supported:",
        "Launcher plain-language help report copy supported:",
        "Launcher startup recovery structured compact actions supported:",
        "Launcher compact low-profile diagnostics toggle supported:",
        "Launcher compact diagnostics toggle detail labels supported:",
        "Launcher compact structured diagnostics toggle labels supported:",
        "Launcher startup fallback raw banner suppressed:",
        "Launcher portal UX device validated:",
        "Launcher portal UX validation boundary:",
        "SteamKit debug logs sanitized for credentials/tokens:",
        "portal scaling/readability/next-action clarity",
        "hidden diagnostics behavior",
        "not complete until ARM64 evidence covers"
    )

Add-Check `
    "docs\android-login-portal-evidence-template.md" `
    "captures ARM64 login, portal, cloud, and launch proof without secrets" `
    @(
        "Do not use emulator evidence for signoff",
        "Steam username/email redacted",
        "Steam password absent",
        "Steam Guard code absent",
        "Native integrated Steam login panel opens automatically",
        "No USE ANDROID AUTOFILL popup/helper dialog visible",
        "Inline status guidance visible",
        "Native login panel remains usable when the keyboard is open",
        "Native login panel can scroll if keyboard or small screen reduces available height",
        "Native login panel keeps Sign in and Cancel reachable with the keyboard open",
        "Native login controls are stacked/full-width in portrait and use responsive wide rows in landscape",
        "Native login primary button says Sign in with Steam",
        "Native login action buttons render sentence-case labels instead of Android all-caps transformations",
        "Compact launcher sign-in shows Sign in with Steam before helper copy",
        "Compact launcher sign-in says Sign in with Steam / Android login",
        "Compact launcher sign-in uses a large primary Sign in with Steam CTA and a readable two-line password-manager safety helper",
        "Native login panel requests suggestions for username and password fields",
        "Native login panel requests suggestions again when username/password fields gain focus",
        "Native login Next control focuses password field",
        "Back/Cancel dismissal hides the soft keyboard before returning to launcher",
        "Provider does not prompt to save unverified credentials before Steam authentication",
        "Password visibility toggle shows/hides password without storing it",
        "Password visibility resets to hidden after submit/cancel/reopen",
        "Compact sign-in status says Sign in with Steam to continue instead of raw credential prompt",
        "Compact download-needed status says Download this game version to play and the next action reads Install Game",
        "Compact ready status says Ready to play this version and the next action reads Start Game",
        "Launcher compact plain-language status copy supported",
        "Quick-start guide visible",
        "On compact phone layout, quick-start guidance starts collapsed",
        "Compact quick-start toggle says Quick Start / Get saves first and is touch-safe",
        "Compact quick-start toggle renders Quick Start / Get saves first as structured title/detail labels",
        "Compact expanded quick-start guide stays bounded and shows Sign in, Get files, Get saves, Play, and Upload locked rows",
        "Compact expanded quick-start guide renders each step inside a bounded row card",
        "Compact active task screens suppress the quick-start drawer so primary controls stay higher",
        "Quick-start guidance expands/collapses without hiding the primary task",
        "Compact phone layout uses most of the usable screen height",
        "Compact phone layout avoids excessive internal panel margins",
        "Compact phone shell uses dense panel padding",
        "Compact phone layout uses dense vertical spacing between repeated launcher regions",
        "Compact phone layout uses dynamic content width instead of a narrow fixed column",
        "Tablet/wide layout avoids a narrow fixed inner column",
        "Android warmup/loading screen uses a mobile-width compact panel with readable styled progress",
        "Android post-launch startup status uses a framed mobile-width readable card",
        "Native fallback verbose diagnostics collapsed until requested",
        "Startup recovery compact actions render Restart App / Open launcher, Safe Start / Cloud off, Help Report / Share details, Copy Log / Review first, and Hide Help / Keep waiting",
        "Native fallback recovery actions split into responsive rows on narrow landscape screens",
        "Portal task flow is top anchored rather than vertically stranded",
        "Compact phone status appears as a readable vertical next-step card",
        "Compact phone status card is low-profile but still readable",
        "Compact status card uses an inline phase and next-action headline where width allows",
        "Compact status card stacks phase and next action without squeezing either label on narrow compact screens",
        "Compact status uses short mobile detail copy and expands full raw status when tapped",
        "Compact status exposes a visible Details / Hide cue in a touch-safe row",
        "Launcher compact touch-safe status detail button supported",
        "Launcher compact status detail cue supported",
        "Compact responsive numbered workflow step strip remains visible while scrolling",
        "Compact workflow step strip stays in one dense row on narrow compact viewports",
        "Compact workflow step strip uses unified 62px touch-height cells on narrow compact viewports",
        "Compact workflow step strip shows two-line visible labels, not only numbers/tooltips",
        "Sign in / Account, Verify / Steam Guard, Files / Game files, and Play / Saves safe",
        "Compact workflow step strip separates step numbers into small badges next to readable labels",
        "Launcher compact workflow step detail labels supported",
        "Launcher compact workflow unified touch height supported",
        "Tapping compact workflow step labels scrolls directly to visible matching task sections or the current safe fallback task",
        "Compact workflow step strip uses the same larger touch height as compact task actions",
        "Compact current-task bar remains reachable while scrolling",
        "Compact current-task bar uses app-like task title wording",
        "Compact current-task bar uses contextual task detail labels",
        "Compact current-task bar renders task/context labels as structured title/detail labels",
        "Compact current-task bar is touch-safe but still compact",
        "Compact inline current-task bar is dense while remaining touch-safe",
        "Compact current-task bar and workflow strip share a tight sticky header",
        "Compact current-task bar and workflow strip share one inline sticky row when width allows",
        "Compact stacked current-task row is low-profile on narrow compact viewports",
        "Compact sticky task header is grouped inside a low-profile toolbar shell",
        "Compact sticky task header stacks on narrow compact viewports",
        "Compact sticky task header keeps the narrow workflow row dense enough to leave action content visible",
        "Compact sticky task header reflows between inline and stacked layouts after rotation or keyboard viewport changes",
        "Compact active task remains re-anchored after rotation or keyboard viewport changes",
        "Compact Game Install selected-version summary is a readable touch-safe card with Cloud unchanged cue and Change / Change version shortcut",
        "Compact Game Install selected-version summary shortcut opens the version drawer without changing branches or cloud saves",
        "Compact install-version dropdown and refresh controls share one row when width allows",
        "Compact version drawer controls render Change Version / Local files only and Refresh Versions / Update branch list as structured title/detail labels",
        "Compact expanded version helper says Files for / Play version with Default files or Separate files and short branch status",
        "Status card shows a clear guided next action for the current state",
        "Failure/blocked/crash statuses show attention/fix guidance before normal install/cloud/launch guidance",
        "Primary actions use clear task wording, for example sign in/start game/verify code",
        "Primary launch action consistently says Start Game",
        "Primary and secondary actions are large enough to tap comfortably",
        "Launcher background has visible branded atmosphere without reducing readability",
        "Buttons use high-contrast rounded action styling",
        "Compact phone header uses shortened subtitle/chrome",
        "Compact phone brand header is a single low-profile row",
        "Compact phone brand subtitle remains readable at phone scale",
        "Compact phone brand subtitle uses plain app copy instead of pipe-separated labels",
        "Compact phone header leaves more first-action area visible",
        "Compact phone section headers avoid repeated subtitle blocks",
        "Compact phone section headers stay compact and leave controls visible",
        "Compact phone section headers keep title and readable task cue in one dense row without clipping the title",
        "Compact phone section headers use explicit short cues Steam account, Current code, Local files, and Play safely instead of clipped desktop subtitles",
        "Compact Game Install shows selected version and Download before optional version details",
        "Compact version dropdown is large enough to read and tap when expanded",
        "Opened compact game-version dropdown popup rows have larger spacing/padding and are touch-safe",
        "Compact phone version details start collapsed",
        "Version details expand/collapse without changing selected version",
        "Compact download progress appears directly below the disabled DOWNLOADING primary action",
        "Compact download progress uses a taller styled percentage bar",
        "Compact optional drawer toggles are low-profile but still tappable",
        "Compact optional drawer toggles use dense touch-safe height",
        "Compact optional drawer toggles are shorter than primary action buttons",
        "Compact Play/Sync action buttons render title/detail labels as structured two-line controls",
        "Compact launch CTA says Start Game / Ready version",
        "Compact recovery/tools actions use a two-column support grid when width allows",
        "Compact recovery/tools actions stack full-width on narrow compact viewports",
        "Compact phone cloud safety starts collapsed",
        "Compact collapsed cloud-safety drawer reads Save Check / Get saves first so it does not look like the Get Steam Saves action",
        "Compact cloud-safety cue appears before Pull/Push controls",
        "Compact expanded cloud-safety detail says Saves for and Get Steam saves before upload / Upload can overwrite Steam",
        "Cloud safety expands/collapses while preserving Pull/Push controls",
        "Compact phone cloud options start collapsed",
        "Cloud options expand/collapse while preserving Pull/Push controls",
        "Compact Save Backup and Cloud Sync options use Local safety and Steam saves detail labels and share one low-profile row when width allows",
        "Compact Save Backup and Cloud Sync options stack full-width on narrow compact viewports",
        "Pull/Push controls appear before lower-frequency cloud options",
        "Pull from Cloud appears before Push to Cloud",
        "Compact cloud labels name Pull as Android-directed and Push as Steam-directed",
        "Compact Pull action says Get Steam Saves / Download to Android",
        "Compact locked Push toggle renders Upload Locked / Review first and Hide Upload / Keep locked as structured title/detail labels",
        "Compact unlocked Push actions say Upload to Steam / Overwrite cloud and Confirm Upload / Overwrite cloud",
        "Compact armed Push warning says Steam Cloud overwrite / Confirm only after Pull/local saves are verified",
        "Compact Get Steam Saves and locked Steam upload share one two-button row when width allows",
        "Compact Get Steam Saves and locked Steam upload stack with Get Steam Saves first on narrow compact viewports",
        "Armed Push state shows overwrite warning before final confirmation",
        "Branch, redownload, cache, and final Push confirmations use contextual confirm/cancel labels instead of generic OK/Cancel buttons",
        "Long compact confirmation warnings are scroll-safe and keep confirm/cancel buttons reachable",
        "Confirmation dialogs use the current visible viewport after rotation or keyboard-driven viewport changes",
        "Focused managed Steam Guard/fallback input stays visible above the Android soft keyboard",
        "Compact optional drawer toggles remain tappable without taking full primary-action height.",
        "Compact optional drawer toggles use a dense touch-safe height instead of the older tiny drawer rows.",
        "Compact drawer toggles and dense workflow controls share the same touch-safe compact height:",
        "Compact optional drawer toggles are visibly shorter than primary action buttons while still tappable.",
        "Help & Reports drawer hidden by default",
        "Compact diagnostics toggle uses a touch-safe two-line detail label",
        "Compact diagnostics toggle renders title/detail labels as structured controls",
        "Compact diagnostics is inside the scroll body rather than fixed root chrome",
        "Raw startup fallback failure text hidden from portal",
        "The compact workflow strip shows visible step labels such as Sign in / Account, Verify / Steam Guard, Files / Game files, and Play / Saves safe; it does not rely on hover-only tooltips",
        "The compact workflow strip is touch-safe enough for Android while keeping two-line step labels readable",
        "The compact game-version dropdown is large enough to read and tap when the version drawer is expanded",
        "Opening the compact game-version dropdown shows larger touch-safe popup row spacing and horizontal padding",
        "Compact Play and Sync ready-version summary is a readable touch-safe card with Save Check and Upload locked cues",
        "Compact Play and Sync ready-version summary shortcut opens Save Check without unlocking Push",
        "Compact Play and Sync keeps the ready summary, Save Check shortcut, Get-saves-first cloud controls, and Start Game before version management",
        "Compact Play and Sync keeps save backup and cloud sync options below Start Game as optional controls",
        "Username keyboard next action focuses password",
        "Next button focuses password and requests password suggestions",
        "Password keyboard done action attempts submit",
        "Password-manager suggestions",
        "Samsung Pass",
        "Google Password Manager",
        "Steam Guard prompt visible",
        "Steam Guard field accepts alphanumeric input",
        "Compact Steam Guard code field and Verify button are large enough to tap comfortably",
        "Compact Steam Guard shows code field and Verify Code before helper copy",
        "Compact Steam Guard code field and Verify Code share one touch-safe action row when width allows",
        "Compact Steam Guard code field and Verify Code stack full-width on narrow compact viewports",
        "Compact Steam Guard submit action says Verify Code / Submit once",
        "Compact Steam Guard retry keeps the rejected-code title short and moves latest-code guidance into the helper below the code controls",
        "Compact retry/failure recovery button is primary and uses Try Again / Restart task labels",
        "Compact launcher-log copy action says Copy Log / Review first",
        "Wrong password produces recoverable failure",
        "Failed-login status gives clear retry guidance",
        "Failed-login status states Steam passwords are not stored",
        "Connection/session failure gives connection-specific recovery guidance",
        "Steam Guard section states code is submitted once and never stored",
        "Wrong Steam Guard code asks for the latest Steam Guard code",
        "Successful login returns to launcher",
        "Game version dropdown visible/readable",
        "Version/download guidance states local game files are separate from Steam Cloud saves",
        "Ready-state version details repeat that Steam Cloud saves move only through Pull/Push",
        "Play and Sync section appears when actions are available",
        "Pull from Cloud completed",
        "Push to Cloud guarded by confirmation",
        "Game launch completed",
        "Native credential panel inline status configured",
        "Native credential panel keyboard-safe layout configured",
        "Native credential panel IME inset scroll supported",
        "Native credential panel touch-target layout configured",
        "Native credential panel requests both Autofill fields",
        "Native credential panel focus Autofill requests supported",
        "Native credential panel task-led buttons supported",
        "Native credential panel responsive action rows supported",
        "Native credential panel orientation reflow supported",
        "Native credential panel short-height copy supported",
        "Native credential panel short-height reflow supported",
        "Native credential panel IME height reflow supported",
        "Native credential panel password visibility toggle supported",
        "Native credential panel password-focus button supported",
        "Native credential panel Back dismiss supported",
        "Native credential panel dismiss retry supported",
        "Native credential panel dismiss hides keyboard",
        "Native credential panel suppresses pre-auth save prompt",
        "Steam Guard one-shot code guidance supported",
        "Failed-login retry guidance supported",
        "Context-specific login recovery guidance supported",
        "Launcher portal UX model",
        "Launcher phase-labeled status supported",
        "Launcher structured status chip supported",
        "Launcher guided next-action status supported",
        "Launcher error-first guided status supported",
        "Launcher safe first-run guidance supported",
        "Launcher compact safe-flow guidance collapsible",
        "Launcher compact low-profile safe-flow toggle supported",
        "Launcher compact safe-flow toggle detail labels supported",
        "Launcher compact structured safe-flow toggle labels supported",
        "Launcher compact safe-flow bounded guide supported",
        "Launcher compact active-task safe-flow suppression supported",
        "Launcher mobile-first compact layout supported",
        "Launcher compact dense panel padding supported",
        "Launcher compact dense vertical rhythm supported",
        "Launcher rounded scaled metrics supported",
        "Launcher Android compact touch-scale floor supported",
        "Launcher Android-readable warmup screen supported",
        "Launcher Android-readable startup status card supported",
        "Launcher compact dynamic content width supported",
        "Launcher tablet/wide content layout supported",
        "Launcher top-anchored portal content supported",
        "Launcher compact vertical status hero supported",
        "Launcher compact low-profile status card supported",
        "Launcher compact status headline row supported",
        "Launcher compact short status details supported",
        "Launcher compact stacked status headline supported",
        "Launcher viewport-aware compact status headline reflow supported",
        "Launcher compact stable status detail row supported",
        "Launcher compact sticky workflow step strip supported",
        "Launcher compact low-profile workflow step strip supported",
        "Launcher compact low-profile two-column workflow step strip supported",
        "Launcher compact workflow step direct navigation supported",
        "Launcher compact single-row numbered workflow step strip supported",
        "Launcher compact two-column workflow step strip supported",
        "Launcher compact narrow workflow single-row supported",
        "Launcher compact visible workflow step labels supported",
        "Launcher compact workflow step detail labels supported",
        "Launcher compact workflow step number badges supported",
        "Launcher compact workflow unified touch height supported",
        "Launcher compact current-task jump supported",
        "Launcher compact sticky current-task bar supported",
        "Launcher compact low-profile current-task bar supported",
        "Launcher compact dense inline current-task bar supported",
        "Launcher compact current-task shared touch height supported",
        "Launcher compact low-profile stacked current-task bar supported",
        "Launcher compact current-task context labels supported",
        "Launcher compact structured current-task labels supported",
        "Launcher compact current-task short title labels supported",
        "Launcher compact touch-safe sticky header controls supported",
        "Launcher compact grouped sticky task header supported",
        "Launcher compact sticky task toolbar shell supported",
        "Launcher compact inline sticky task header supported",
        "Launcher compact responsive sticky task header supported",
        "Launcher viewport-aware sticky task header reflow supported",
        "Launcher viewport-aware compact task re-anchor supported",
        "Launcher compact dense sticky task header supported",
        "Launcher compact task-jump navigation labels supported",
        "Launcher compact readable detail label font supported",
        "Launcher compact padded scroll anchors supported",
        "Launcher keyboard-focused input scroll supported",
        "Launcher compact selected-version headline supported",
        "Launcher compact selected-version summary shortcut supported",
        "Launcher compact responsive selected-version summary supported",
        "Launcher compact contextual confirmation labels supported",
        "Launcher compact scroll-safe confirmation dialogs supported",
        "Launcher viewport-aware confirmation dialogs supported",
        "Launcher compact Steam Guard large input supported",
        "Launcher compact Steam Guard action-first layout supported",
        "Launcher compact Steam Guard inline action row supported",
        "Launcher compact responsive Steam Guard action layout supported",
        "Launcher viewport-aware compact Steam Guard action row reflow supported",
        "Launcher compact Steam Guard submit detail label supported",
        "Launcher compact Steam Guard retry guidance supported",
        "Launcher compact Steam Guard bounded helper supported",
        "Launcher compact primary retry action supported",
        "Launcher compact structured retry action labels supported",
        "Launcher compact primary login action first supported",
        "Launcher compact Android login primary CTA supported",
        "Launcher compact Android login detail label supported",
        "Launcher compact Android login helper detail label supported",
        "Launcher compact completed-auth section suppression supported",
        "Launcher touch-first action targets supported",
        "Launcher primary action wording supported",
        "Launcher consistent Start Game CTA supported",
        "Launcher compact launch detail label supported",
        "Launcher branded atmospheric background supported",
        "Launcher branded background explicit RGBA supported",
        "Launcher high-contrast rounded actions supported",
        "Launcher compact header chrome reduction supported",
        "Launcher compact condensed brand header supported",
        "Launcher compact single-line brand header supported",
        "Launcher compact readable brand subtitle supported",
        "Launcher compact section-header subtitle suppression supported",
        "Launcher compact low-profile section headers supported",
        "Launcher compact single-row section headers supported",
        "Launcher compact section-header task cues supported",
        "Launcher compact explicit section-header cues supported",
        "Launcher compact install primary action first supported",
        "Launcher compact install primary detail label supported",
        "Launcher compact download progress hero supported",
        "Launcher compact download progress status label supported",
        "Launcher compact inline install-version controls supported",
        "Launcher compact version details collapsible",
        "Launcher compact structured install-version action labels supported",
        "Launcher compact ready-version summary panel supported",
        "Launcher compact ready-version summary shortcut supported",
        "Launcher compact ready-version headline supported",
        "Launcher compact responsive ready-version summary supported",
        "Launcher compact ready-state priority supported",
        "Launcher compact ready-state cloud options below launch supported",
        "Launcher compact ready-state install-section suppression supported",
        "Launcher compact touch-safe version dropdown supported",
        "Launcher compact touch-safe dropdown popup supported",
        "Launcher compact cloud-safety cue before actions supported",
        "Launcher compact cloud-safety detail label supported",
        "Launcher compact cloud direction labels supported",
        "Launcher compact cloud primary actions row supported",
        "Launcher compact Pull detail label supported",
        "Launcher compact responsive action rows supported",
        "Launcher compact dangerous Push detail labels supported",
        "Launcher compact armed Push warning detail label supported",
        "Launcher compact launcher-log review label supported",
        "Launcher compact cloud options row supported",
        "Launcher compact cloud option detail labels supported",
        "Launcher version-install/cloud-save separation guidance supported",
        "SteamKit debug logs sanitized for credentials/tokens",
        "Release signoff is not valid"
    )

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

Complete-StaticAudit `
    -FailureHeading "Steam version-selection static audit failed:" `
    -SuccessMessage "Steam version-selection static audit passed: {0} checks."
