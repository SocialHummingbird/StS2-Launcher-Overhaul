# Android Steam login validation

This checklist is the proof contract for the hardened Android Steam login flow.

Use `docs/android-login-portal-evidence-template.md` for the concrete ARM64 evidence artifact. The checklist below defines what the artifact must prove.

The intended user-facing path on Android is:

1. The launcher opens an integrated in-app native Steam credential panel.
2. The panel shows real Android username and password fields.
3. The user enters credentials manually or uses Android/Samsung/Google password-manager suggestions if the provider offers them.
4. The launcher submits credentials through SteamKit.
5. Steam Guard is shown when Steam requires a code.
6. Successful authentication returns to the launcher with saved Steam session credentials available for download/cloud actions.

## Current implementation boundary

- There is no user-facing native credential handoff popup or separate helper dialog.
- Android uses an integrated native credential panel with real `EditText` fields; in release-facing shorthand, these are real EditText fields.
- The launcher portal uses explicit status and titled state sections for Steam sign-in, Steam Guard, game install, play/sync actions, and diagnostics.
- The launcher does not store or inject Steam passwords.
- The native Android username/password fields set Android Autofill hints and Steam web-domain metadata for credential providers where supported.
- The native panel explicitly requests password-manager suggestions for both username and password fields when Android exposes Autofill.
- The native panel requests password-manager suggestions again when each credential field gains focus.
- The native panel adds IME-aware scroll padding and focus scrolling so lower controls stay reachable while the keyboard is open.
- The native panel uses task-led full-width sign-in/cancel controls aligned with the launcher portal wording.
- The native panel has a manual password visibility toggle that resets to hidden when credential fields are cleared.
- The native panel provides an explicit password-focus control in addition to the keyboard Next action.
- The Android Back action dismisses the native credential panel without exiting the launcher.
- Dismissing the native credential panel through Back or Cancel re-enables Steam sign-in without waiting for the credential polling timeout.
- Dismissing the native credential panel clears focus and hides the soft keyboard before returning to the launcher.
- The native panel cancels the Android Autofill save session before handoff/dismissal so providers can suggest existing Steam credentials without prompting to save unverified credentials before Steam authentication.
- Submitted native credential handoff values are in-memory only and expire after 60 seconds if managed login does not consume them.
- The Godot `LineEdit` username/password fields remain the non-Android fallback.
- The Steam Guard field accepts alphanumeric code entry, normalizes uppercase/separators, and uses large compact controls.
- Samsung Pass, Google Password Manager, and other provider suggestions are still best-effort until validated on ARM64 hardware. Local fix28 evidence validates Samsung Pass/Android Autofill field recognition only; matched saved-credential suggestion behavior is still unproven because the provider reported no matched saved Steam credential.
- Steam refresh/session credentials are still saved through the encrypted Android Keystore path after successful SteamKit authentication. This is separate from password-manager behavior.
- The Steam Guard section states that codes are submitted once and never stored, and wrong-code recovery asks for the latest Steam Guard code. On compact layouts, rejected-code recovery keeps the title short and moves latest-code guidance into helper copy below the code controls.
- Failed login returns to sign-in with clear retry guidance and repeats that Steam passwords are not stored.
- Connection/session recovery failures use connection-specific guidance rather than treating every failure as a wrong-password case.

## Current ARM64 evidence

Local fix28 evidence is captured at `artifacts/android/fix28-native-login-panel-cancel-20260619-112725`, with a text-only public-redacted summary at `artifacts/android/fix28-native-login-panel-cancel-20260619-112725-public-redacted`.

This evidence proves:

- Removing the saved encrypted Steam session files shows the integrated native Steam login panel on ARM64 hardware.
- The panel exposes real Android username and masked password fields, portrait full-width controls, responsive wide credential/action rows in landscape, orientation/screen-size reflow for the native credential panel, the password-focus control, the password visibility control, and inline text that the Steam password is never stored by StS2 Mobile.
- Empty submit stays local and shows inline username guidance without starting Steam authentication.
- Android Back dismisses the panel without exiting the app.
- After Back dismissal, the launcher can reopen the native panel immediately.
- Cancel dismisses the panel and returns to the launcher surface.
- Samsung Pass/Android Autofill were active, recognized the Steam web domain, identified one username field and one password field, validated the expected hints, and enabled inline keyboard suggestions.
- Samsung Pass reported no matched saved Steam credential, so no account-specific credential suggestion appeared.
- The saved encrypted Steam session files were restored after the test with matching pre-test and post-restore hashes.
- No Steam Cloud Push was performed during this pass.

This evidence does not prove manual real credential entry through SteamKit, Steam Guard, failed-login recovery, successful authenticated return to the launcher, Google Password Manager behavior, or matched Samsung Pass saved-credential selection.

## Required evidence

Capture the following on ARM64 hardware:

1. `Fresh login screen`
   - Integrated native Steam login panel is present on Android.
   - Native Steam username field is present.
   - Native Steam password field is present.
   - No `USE ANDROID AUTOFILL` or native credential handoff popup is present.
   - The launcher status capsule and titled portal sections are readable at the device's current scale.
   - Android compact mode keeps a touch-oriented minimum UI scale on small devices.
   - Android warmup/loading uses a mobile-width compact panel with readable styled percentage progress.
   - Android post-launch startup status uses a framed mobile-width readable card after the launcher closes.
   - The compact launcher shell uses dense panel padding so phone-width content is not squeezed by excess internal margins.
   - Compact vertical spacing between repeated launcher regions is dense enough that first-action controls remain visible.
   - Diagnostics are hidden behind the Help & Reports drawer until explicitly opened.
   - Raw startup fallback failure text is not visible behind or above the launcher portal.
   - Native fallback keeps verbose diagnostics collapsed until explicitly requested; fallback remains a failure/recovery route, not launch success.
   - Native fallback recovery actions split into two touch-friendly rows on narrow landscape screens.
   - Startup recovery compact actions render as structured user-facing controls: `Restart App` / `Open launcher`, `Safe Start` / `Cloud off`, `Help Report` / `Share details`, `Copy Log` / `Review first`, and `Hide Help` / `Keep waiting`.
   - Native credential panel uses short-height copy on cramped landscape screens so credential fields and primary actions stay higher.
   - Native credential panel reflows short-height copy when the landscape height class changes without changing the wide-layout class.
   - Native credential panel reflows short-height copy when the keyboard reduces landscape usable height.
   - Managed Steam Guard/fallback text input scrolls into view and remains visible above the Android soft keyboard.
   - The compact status card stays readable while using low-profile spacing so more current task content remains visible.
   - The compact status card keeps the phase chip and guided next action inline where width allows.
   - The compact status card stacks the phase chip and guided next action so neither is squeezed on narrow screens.
   - The compact status card reflows between inline and stacked phase/next-action headline layouts after Android rotation or keyboard viewport changes.
   - Compact status details keep normal progress text to one stable line while attention/failure guidance can expand.
   - Compact status uses short mobile detail copy and expands full raw status when tapped.
   - Compact status exposes a visible Details / Hide cue in a touch-safe row.
   - Compact sign-in status says Sign in with Steam to continue instead of exposing the raw credential prompt.
   - Compact download-needed status says Download this game version to play and the next action reads Install Game.
   - Compact ready status says Ready to play this version and the next action reads Start Game.
   - Compact quick-start guidance starts collapsed behind a touch-safe two-line toggle that says Quick Start and Get saves first.
   - Compact quick-start toggle renders Quick Start / Get saves first as structured title/detail labels, not cramped raw newline text.
   - Compact expanded quick-start guide stays bounded and shows `Sign in`, `Get files`, `Get saves`, `Play`, and `Upload locked` row cards while keeping upload locked until saves are deliberately checked and unlocked.
   - Compact expanded quick-start guide renders each step inside a bounded row card.
   - Compact sign-in, Steam Guard, and download task screens suppress the quick-start drawer so the current primary controls stay higher in the viewport.
   - The compact brand header is a single low-profile row that leaves more of the current task/status area visible on small screens.
   - The compact brand subtitle remains readable at phone scale and uses plain app copy instead of pipe-separated command-line-style labels.
   - Compact section headers keep title and readable task cue in one dense row without restoring bulky repeated subtitle cards.
   - Compact section headers use explicit short task cues such as `Steam account`, `Current code`, `Local files`, and `Play safely` instead of clipped desktop subtitle sentences.
   - Compact section headers stay compact enough that the first controls remain visible.
   - Compact Android sign-in shows `Sign in with Steam` before password-manager helper copy.
   - Compact Android sign-in CTA renders `Sign in with Steam / Android login` as a structured title/detail label.
   - Compact Android sign-in uses a large primary `Sign in with Steam` CTA and a readable two-line password-manager safety helper.
   - The compact workflow strip shows short visible step labels such as `Sign in`, `Verify`, `Files`, and `Play`; it does not rely on hover-only tooltips.
   - The compact workflow strip stays in one dense row on narrow compact viewports instead of taking a second fixed header row.
   - The compact workflow strip separates the step number into a small badge so labels stay readable on phone-width rows.
   - Tapping compact workflow step labels scrolls directly to the visible matching task section or the current safe fallback task.
   - The compact workflow strip is touch-safe enough for Android while keeping step labels readable.
   - The compact current-task bar stays reachable, uses app-like task title wording, and is touch-safe without wasting vertical space.
   - The compact current-task bar uses short title labels such as `Sign in`, `Verify`, `Files`, and `Play` without a status prefix.
   - The compact current-task bar uses contextual detail labels such as `Steam account`, `Steam Guard code`, `Download version`, and `Play and saves`.
   - The compact current-task bar renders task names and contextual details as structured title/detail labels, not cramped raw newline text.
   - The compact inline current-task bar uses dense height while staying touch-safe, so the persistent header does not crowd active controls.
   - The compact inline current-task bar uses the same touch-safe compact control height as the workflow and drawer controls.
   - The compact current-task bar and workflow strip share a tight sticky header instead of being separated as independent chrome rows.
   - When width allows, the compact current-task bar and workflow strip share one inline sticky row, reducing header height while keeping controls readable and tappable.
   - On narrow compact viewports, the stacked current-task row stays low-profile while remaining touch-safe.
   - The compact sticky task header is grouped inside a low-profile toolbar shell so the persistent task controls read as one toolbar.
   - On narrow compact viewports, the compact sticky task header stacks into a dense current-task row plus one dense workflow row instead of a two-row workflow grid.
   - The compact sticky task header reflows between inline and stacked task/workflow layouts after Android rotation or keyboard viewport changes.
   - The compact active task or last compact scroll target re-anchors after Android rotation or keyboard viewport changes without stealing focus from keyboard input fields.
   - Compact two-line controls use a readable shared detail-label font for secondary action context.
   - Compact Game Install shows the selected version as a readable touch-safe summary card with `Cloud unchanged`, `Default files` or `Separate files`, and a `Change` / `Change version` cue that opens the version drawer before `Download Version / Local files only`.
   - The compact install primary action renders `Download Version / Local files only`, `Redownload Version / Rebuild local files`, `Retry Download / Local files only`, and `Downloading... / Steam files` as structured title/detail labels.
   - The compact game-version dropdown is large enough to read and tap when the version drawer is expanded.
   - Opening the compact game-version dropdown shows larger touch-safe popup row spacing and horizontal padding.
   - The compact install-version dropdown and refresh controls share one row when width allows and stack full-width on narrow compact viewports.
   - Compact version drawer controls show short detail labels such as `Local files only` and `Update branch list`.
   - Compact version drawer controls render `Change Version` / `Local files only` and `Refresh Versions` / `Update branch list` as structured title/detail labels, not cramped raw newline text.
   - Compact expanded version helper copy is bounded to two lines and uses `Files for` or `Play version` with `Default files` / `Separate files` plus a short selected-branch status instead of implementation slot names.
   - Compact diagnostics toggle renders `Help & Reports` / `Private until opened` as structured title/detail labels when closed and `Hide Help` / `Back to launcher` when open.
   - Opening compact diagnostics keeps the title as `Help & Reports` and states problem details/help reports should be reviewed before sharing.
   - Diagnostics show `Native integrated credential panel supported: true`.
   - Diagnostics show `Native credential fields Autofill hints configured: true`.
   - Diagnostics show `Steam credential web domain configured: true`.
   - Diagnostics show `Native credential panel inline status configured: true`.
   - Diagnostics show `Native credential panel keyboard-safe layout configured: true`.
   - Diagnostics show `Native credential panel IME inset scroll supported: true`.
   - Diagnostics show `Native credential panel touch-target layout configured: true`.
   - Diagnostics show `Native credential handoff popup supported: false`.

2. `Manual credential entry`
   - Tapping username opens an appropriate keyboard.
   - Tapping password opens a password-appropriate keyboard.
   - The username keyboard action moves focus to password.
   - The `Next` control moves focus to the password field and requests password suggestions.
   - The password keyboard action submits login.
   - The native login panel remains usable while the keyboard is open.
   - The native login panel can scroll if the keyboard or a small screen reduces usable height.
   - The native login controls are stacked/full-width and easy to tap.
   - Native login action buttons render sentence-case labels such as `Next`, `Show password`, `Cancel`, and `Sign in with Steam` instead of Android all-caps transformations.
   - The panel shows inline guidance for empty username/password before handoff.
   - Manual username/password entry starts Steam authentication.
   - Username and password fields clear after submit/cancel/expiry.
   - Android Back dismisses the native credential panel and returns to the launcher without leaving the app.
   - Back/Cancel dismissal lets the user reopen the native credential panel immediately.
   - Submitting or cancelling does not trigger a provider prompt to save unverified credentials before Steam authentication.
   - The panel states that the Steam password is never stored by StS2 Mobile.

3. `Password-manager suggestions`
   - Test Samsung Pass if available.
   - Test Google Password Manager if available.
   - Record whether suggestions appear on the visible username/password fields.
   - If suggestions do not appear, keep this as a blocker/limitation, not a failed login regression.

4. `Steam Guard`
   - If Steam Guard is required, the launcher shows the Steam Guard prompt.
   - The Steam Guard field accepts alphanumeric input and is large enough to tap comfortably on compact layouts.
   - Compact Steam Guard shows the code field and `Verify Code` before explanatory helper copy.
   - Compact Steam Guard keeps the code field and `Verify Code` on one touch-safe row when width allows and stacks them full-width on narrow compact viewports.
   - Compact Steam Guard code/action controls reflow between inline and stacked layouts after Android rotation or keyboard viewport changes.
   - Compact Steam Guard submit action renders `Verify Code / Submit once` as a structured title/detail label.
   - Compact Steam Guard helper copy is bounded to two lines below the code controls and keeps current-code/no-storage or newest-code recovery guidance readable.
   - Compact Steam Guard retry keeps the rejected-code title short and moves latest-code guidance into the helper below the code controls.
   - Incorrect code handling remains recoverable.
   - Correct code proceeds to successful authentication or ownership verification.

5. `Failed login`
   - Wrong password or rejected authentication returns a clear failure state.
   - The user can retry from the integrated native credential panel.
   - Compact retry/failure state promotes `Try Again` / `Restart task` as the primary recovery action while support tools remain secondary.
   - No credential values are logged.

6. `Successful return`
   - Successful authentication returns to the launcher.
   - Saved session state is available without asking for the password again.
   - Download/update/cloud actions can use the saved Steam session.
   - The portal clearly exposes the next action without requiring diagnostic-log reading.
   - The Play and Sync section appears when launch/retry/cloud actions are available.
   - Cloud actions label Pull as Steam Cloud to Android and Push as Android saves to Steam Cloud.
   - Compact cloud buttons name Pull as Android-directed and Push as Steam-directed.
   - Compact Get Steam Saves and locked Steam upload share one two-button row when width allows and stack with Get Steam Saves first on narrow compact viewports.
   - Compact locked upload toggle renders Upload Locked / Review first and Hide Upload / Keep locked as structured title/detail labels:
   - Compact expanded cloud-safety detail says Saves for and Get Steam saves before upload / Upload can overwrite Steam.
   - Push warning text names overwrite risk before upload.
   - Compact armed Push warning renders `Steam Cloud overwrite / Confirm only after Pull/local saves are verified` as a stable two-line label.
   - Branch, redownload, cache, and final Push confirmations use contextual confirm/cancel labels instead of generic OK/Cancel buttons.
   - Long compact confirmation warnings are scroll-safe and keep confirm/cancel buttons reachable on narrow compact viewports.
   - Confirmation dialogs use the current visible viewport after rotation or keyboard-driven viewport changes.
   - Compact optional drawer toggles remain tappable without taking full primary-action height.
   - Compact optional drawer toggles use a dense touch-safe height instead of the older tiny drawer rows.
   - Compact drawer toggles and dense workflow controls share the same touch-safe compact height.
   - Compact optional drawer toggles are visibly shorter than primary action buttons while still tappable.
   - Compact Play/Sync drawer toggles include short detail labels for version targeting, Save Check / Get saves first, backup/sync settings, and recovery tools.
   - Compact collapsed cloud-safety drawer reads Save Check / Get saves first so it does not look like the Get Steam Saves action.
   - Compact Play/Sync action buttons render title/detail labels as structured two-line controls, not cramped raw newline text.
   - Compact launch CTA renders `Start Game / Ready version` as a structured title/detail primary action.
   - Compact Pull action renders `Get Steam Saves / Download to Android` as a structured title/detail label before upload remains locked.
   - Compact Save Backup and Cloud Sync options use Local safety and Steam saves detail labels, share one low-profile row when width allows, and stack full-width on narrow compact viewports.
   - Compact download progress appears directly below the disabled `DOWNLOADING...` primary action.
   - Compact download progress status stays as a stable two-line `Downloading selected version` label with a short current Steam/depot detail.
   - Compact download progress uses a taller styled percentage bar instead of the generic thin progress bar.
   - Compact Play and Sync shows the ready version, Save Check guidance, and Upload-locked state in a readable touch-safe summary card that opens Save Check without unlocking Push.
   - Compact ready state prioritizes the ready summary, Save Check shortcut, Get-saves-first cloud controls, and Start Game before version management.
   - Compact ready state keeps save backup and cloud sync options below Start Game as optional controls.
   - Compact unlocked Push actions render `Upload to Steam / Overwrite cloud` and `Confirm Upload / Overwrite cloud` as structured title/detail labels after the upload overwrite drawer is explicitly opened.
   - Compact armed Push warning says Steam Cloud overwrite / Confirm only after Pull/local saves are verified before final upload confirmation.
   - Compact recovery/tools actions use a two-column support grid when width allows and full-width stacked tools on narrow compact viewports.
   - Compact support tools drawer reads `Fixes & Help` / `Repair tools` when closed and `Hide Fixes` / `Back to play` when open.
   - Compact recovery/tools buttons use user-facing labels: `Safe Start / Cloud off`, `Check Files / Updates`, `Game Versions / Refresh list`, `Repair Files / Rebuild game`, `Free Space / Old versions`, `Help Report / Share details`, `Last Problem / Open details`, and `Copy Log / Review first`.
   - Compact launcher-log copy keeps the short `Copy Log` label but uses `Review first` detail text before copying diagnostics.
   - The Help & Reports drawer remains hidden unless explicitly opened.
   - The compact diagnostics toggle uses a touch-safe two-line detail label without exposing diagnostics by default.
   - Compact diagnostics lives inside the scroll body rather than consuming fixed phone viewport chrome, and explicit diagnostics actions scroll to it.
   - Compact diagnostics log text and padding are increased for Android readability, while the log viewport is bounded so it does not take over the phone screen.
   - Compact diagnostics log height refreshes after Android rotation or keyboard viewport changes instead of staying at the startup size.

## Diagnostics to preserve

Paste only non-secret diagnostics:

```text
Android credential provider model:
Native integrated credential panel supported:
Native credential fields Autofill hints configured:
Steam credential web domain configured:
Native credential panel inline status configured:
Native credential panel keyboard-safe layout configured:
Native credential panel IME inset scroll supported:
Native credential panel touch-target layout configured:
Native credential panel requests both Autofill fields:
Native credential panel focus Autofill requests supported:
Native credential panel task-led buttons supported:
Native credential panel responsive action rows supported:
Native credential panel orientation reflow supported:
Native credential panel short-height copy supported:
Native credential panel short-height reflow supported:
Native credential panel IME height reflow supported:
Native credential panel password visibility toggle supported:
Native credential panel password-focus button supported:
Native credential panel Back dismiss supported:
Native credential panel dismiss retry supported:
Native credential panel dismiss hides keyboard:
Native credential panel suppresses pre-auth save prompt:
Steam Guard one-shot code guidance supported:
Failed-login retry guidance supported:
Context-specific login recovery guidance supported:
Godot login field credential metadata configured:
Android keyboard credential hints configured:
Godot fields are native Android Autofill targets:
Password-manager suggestions device validated:
Native credential handoff popup supported:
Launcher stores Steam password for credential providers:
Native credential handoff result TTL seconds:
Android credential provider implementation note:
Android credential provider capability boundary:
Launcher portal UX model:
Launcher status-led portal supported:
Launcher phase-labeled status supported:
Launcher structured status chip supported:
Launcher guided next-action status supported:
Launcher error-first guided status supported:
Launcher compact plain-language status copy supported:
Launcher titled state sections supported:
Launcher safe first-run guidance supported:
Launcher compact safe-flow guidance collapsible:
Launcher compact low-profile safe-flow toggle supported:
Launcher compact safe-flow toggle detail labels supported:
Launcher compact structured safe-flow toggle labels supported:
Launcher compact safe-flow bounded guide supported:
Launcher compact active-task safe-flow suppression supported:
Launcher mobile-first compact layout supported:
Launcher compact dense panel padding supported:
Launcher compact dense vertical rhythm supported:
Launcher rounded scaled metrics supported:
Launcher Android compact touch-scale floor supported:
Launcher Android-readable warmup screen supported:
Launcher Android-readable startup status card supported:
Launcher compact dynamic content width supported:
Launcher tablet/wide content layout supported:
Launcher top-anchored portal content supported:
Launcher compact vertical status hero supported:
Launcher compact low-profile status card supported:
Launcher compact status headline row supported:
Launcher compact stacked status headline supported:
Launcher viewport-aware compact status headline reflow supported:
Launcher compact stable status detail row supported:
Launcher compact short status details supported:
Launcher compact sticky workflow step strip supported:
Launcher compact low-profile workflow step strip supported:
Launcher compact low-profile two-column workflow step strip supported:
Launcher compact workflow step direct navigation supported:
Launcher compact two-column workflow step strip supported:
Launcher compact single-row numbered workflow step strip supported:
Launcher compact narrow workflow single-row supported:
Launcher compact visible workflow step labels supported:
Launcher compact workflow step detail labels supported:
Launcher compact workflow step number badges supported:
Launcher compact readable workflow step number badges supported:
Launcher compact workflow unified touch height supported:
Launcher compact current-task jump supported:
Launcher compact sticky current-task bar supported:
Launcher compact low-profile current-task bar supported:
Launcher compact dense inline current-task bar supported:
Launcher compact current-task shared touch height supported:
Launcher compact low-profile stacked current-task bar supported:
Launcher compact current-task context labels supported:
Launcher compact structured current-task labels supported:
Launcher compact current-task short title labels supported:
Launcher compact touch-safe sticky header controls supported:
Launcher compact grouped sticky task header supported:
Launcher compact sticky task toolbar shell supported:
Launcher compact inline sticky task header supported:
Launcher compact responsive sticky task header supported:
Launcher viewport-aware sticky task header reflow supported:
Launcher viewport-aware compact task re-anchor supported:
Launcher compact dense sticky task header supported:
Launcher compact task-jump navigation labels supported:
Launcher compact readable detail label font supported:
Launcher compact padded scroll anchors supported:
Launcher keyboard-focused input scroll supported:
Launcher compact contextual confirmation labels supported:
Launcher compact scroll-safe confirmation dialogs supported:
Launcher viewport-aware confirmation dialogs supported:
Launcher compact Steam Guard large input supported:
Launcher compact Steam Guard action-first layout supported:
Launcher compact Steam Guard inline action row supported:
Launcher compact responsive Steam Guard action layout supported:
Launcher viewport-aware compact Steam Guard action row reflow supported:
Launcher compact Steam Guard submit detail label supported:
Launcher compact Steam Guard retry guidance supported:
Launcher compact Steam Guard bounded helper supported:
Launcher compact primary retry action supported:
Launcher compact structured retry action labels supported:
Launcher compact primary login action first supported:
Launcher compact Android login primary CTA supported:
Launcher compact Android login detail label supported:
Launcher compact Android login helper detail label supported:
Launcher compact completed-auth section suppression supported:
Launcher touch-first action targets supported:
Launcher primary action wording supported:
Launcher consistent Start Game CTA supported:
Launcher compact launch detail label supported:
Launcher branded atmospheric background supported:
Launcher branded background explicit RGBA supported:
Launcher high-contrast rounded actions supported:
Launcher compact header chrome reduction supported:
Launcher compact condensed brand header supported:
Launcher compact single-line brand header supported:
Launcher compact readable brand subtitle supported:
Launcher compact section-header subtitle suppression supported:
Launcher compact low-profile section headers supported:
Launcher compact single-row section headers supported:
Launcher compact section-header task cues supported:
Launcher compact readable section-header cues supported:
Launcher compact explicit section-header cues supported:
Launcher compact install primary action first supported:
Launcher compact install primary detail label supported:
Launcher compact download progress hero supported:
Launcher compact download progress status label supported:
Launcher compact readable download progress bar supported:
Launcher compact inline install-version controls supported:
Launcher compact version details collapsible:
Launcher compact version drawer detail labels supported:
Launcher compact version drawer bounded help label supported:
Launcher compact structured install-version action labels supported:
Launcher compact version summary cards supported:
Launcher compact selected-version summary shortcut supported:
Launcher compact selected-version headline supported:
Launcher compact responsive selected-version summary supported:
Launcher compact ready-version summary panel supported:
Launcher compact ready-version summary shortcut supported:
Launcher compact ready-version headline supported:
Launcher compact responsive ready-version summary supported:
Launcher compact ready-state priority supported:
Launcher compact ready-state cloud options below launch supported:
Launcher compact Play/Sync drawer detail labels supported:
Launcher compact structured Play/Sync action labels supported:
Launcher compact ready-state install-section suppression supported:
Launcher compact touch-safe version dropdown supported:
Launcher compact touch-safe dropdown popup supported:
Launcher compact cloud-safety guidance collapsible:
Launcher compact cloud-safety cue before actions supported:
Launcher compact cloud-safety detail label supported:
Launcher compact cloud options collapsible:
Launcher primary cloud actions before cloud options:
Launcher compact cloud option detail labels supported:
Launcher safer Pull-before-Push cloud ordering supported:
Launcher compact cloud direction labels supported:
Launcher compact cloud primary actions row supported:
Launcher compact Pull detail label supported:
Launcher compact responsive action rows supported:
Launcher manual Push armed overwrite warning supported:
Launcher compact button labels supported:
Launcher compact cloud options row supported:
Launcher compact low-profile drawer toggles supported:
Launcher compact dense drawer toggle height supported:
Launcher compact touch-safe drawer toggle sizing supported:
Launcher compact dangerous Push detail labels supported:
Launcher compact armed Push warning detail label supported:
Launcher compact support tools grid supported:
Launcher compact support tool detail labels supported:
Launcher compact launcher-log review label supported:
Launcher version-install/cloud-save separation guidance supported:
Launcher Help & Reports drawer hidden by default:
Launcher Help & Reports drawer auto-opens for diagnostics actions:
Launcher plain-language help report copy supported:
Launcher compact low-profile diagnostics toggle supported:
Launcher compact diagnostics toggle detail labels supported:
Launcher compact structured diagnostics toggle labels supported:
Launcher compact diagnostics scroll-hosted supported:
Launcher compact readable diagnostics log supported:
Launcher compact bounded diagnostics log viewport supported:
Launcher viewport-aware diagnostics log resize supported:
Launcher startup fallback raw banner suppressed:
Launcher native fallback recovery actions styled:
Launcher native fallback diagnostics collapsed by default:
Launcher native fallback responsive recovery rows supported:
Launcher startup recovery structured compact actions supported:
Launcher portal UX device validated:
Launcher portal UX implementation note:
Launcher portal UX validation boundary:
SteamKit debug logs opt-in enabled:
SteamKit debug logs sanitized for credentials/tokens:
```

## Do not publish

- Steam username if it identifies the account.
- Steam password.
- Steam Guard code.
- Refresh tokens.
- Shared preferences.
- Full unsanitized logcat.
- Screenshots showing account-identifying data.

## Completion rule

This login hardening work is not complete until ARM64 evidence covers the integrated native credential panel, manual entry, password-manager suggestion behavior, Steam Guard, failed-login recovery, successful return to launcher, portal scaling/readability/next-action clarity, hidden diagnostics behavior, and secret-safe diagnostics.
