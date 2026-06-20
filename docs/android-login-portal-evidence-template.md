# Android login and portal evidence template

Use this template for ARM64 device validation of the Android Steam login panel and launcher portal. Do not use emulator evidence for signoff.

## Build and device

```text
APK version:
APK filename:
Package name:
Device model:
Android version:
One UI / vendor skin:
ABI:
Install type: fresh / update
Clear app data before test: yes / no
Tester:
UTC date:
```

## Secret-safety confirmation

Confirm before sharing logs, screenshots, or issue comments:

```text
Steam username/email redacted: yes / no
Steam password absent: yes / no
Steam Guard code absent: yes / no
Refresh/session tokens absent: yes / no
Account IDs redacted: yes / no
Local filesystem paths reviewed: yes / no
Save/profile contents reviewed: yes / no
Only sanitized diagnostics shared publicly: yes / no
```

Do not publish screenshots while a credential suggestion row, Steam username, password, Steam Guard code, account identifier, or private save/profile data is visible.

## Fresh portal and login screen

```text
Launcher opens without crash:
Status capsule visible and readable:
Status phase chip visible and readable:
Attention/failure status uses the phase chip rather than only console text:
Compact sign-in status says Sign in with Steam to continue instead of raw credential prompt:
Compact download-needed status says Download this game version to play and the next action reads Install Game:
Compact ready status says Ready to play this version and the next action reads Start Game:
Launcher compact plain-language status copy supported:
Quick-start guide visible:
On compact phone layout, quick-start guidance starts collapsed:
Compact quick-start toggle says Quick Start / Get saves first and is touch-safe:
Compact quick-start toggle renders Quick Start / Get saves first as structured title/detail labels:
Compact expanded quick-start guide stays bounded and shows Sign in, Get files, Get saves, Play, and Upload locked rows:
Compact expanded quick-start guide renders each step inside a bounded row card:
Compact active task screens suppress the quick-start drawer so primary controls stay higher:
Quick-start guidance expands/collapses without hiding the primary task:
Titled Steam Sign-in section visible:
Native integrated Steam login panel opens automatically on Android:
Native username field visible:
Native password field visible:
No USE ANDROID AUTOFILL popup/helper dialog visible:
Panel states Steam password is never stored:
Inline status guidance visible:
Native login panel remains usable when the keyboard is open:
Native login panel can scroll if keyboard or small screen reduces available height:
Native login panel keeps Sign in and Cancel reachable with the keyboard open:
Focused managed Steam Guard/fallback input stays visible above the Android soft keyboard:
Native login controls are stacked/full-width in portrait and use responsive wide rows in landscape:
Native login panel uses short-height copy on cramped landscape screens:
Native login panel reflows short-height copy when landscape height class changes:
Native login panel reflows short-height copy when keyboard reduces landscape usable height:
Native login primary button says Sign in with Steam:
Native login action buttons render sentence-case labels instead of Android all-caps transformations:
Compact launcher sign-in shows Sign in with Steam before helper copy:
Compact launcher sign-in says Sign in with Steam / Android login:
Compact launcher sign-in uses a large primary Sign in with Steam CTA and a readable two-line password-manager safety helper:
Native login panel requests suggestions for username and password fields:
Native login panel requests suggestions again when username/password fields gain focus:
Native login Next control focuses password field:
Password visibility toggle shows/hides password without storing it:
Password visibility resets to hidden after submit/cancel/reopen:
Android Back dismisses native login panel without exiting launcher:
Back/Cancel dismissal re-enables Sign in with Steam immediately:
Back/Cancel dismissal hides the soft keyboard before returning to launcher:
Provider does not prompt to save unverified credentials before Steam authentication:
Help & Reports drawer hidden by default:
Help & Reports drawer opens only when requested:
Compact diagnostics toggle uses a touch-safe two-line detail label:
Compact diagnostics toggle renders title/detail labels as structured controls:
Compact diagnostics drawer title/help copy is user-facing and review-before-sharing safe:
Compact diagnostics is inside the scroll body rather than fixed root chrome:
Compact diagnostics log uses readable compact text and padding:
Compact diagnostics log viewport is bounded for phone screens:
Compact diagnostics log resizes after rotation or keyboard viewport changes:
Raw startup fallback failure text hidden from portal:
Native fallback verbose diagnostics collapsed until requested:
Native fallback recovery actions split into responsive rows on narrow landscape screens:
Startup recovery compact actions render Restart App / Open launcher, Safe Start / Cloud off, Help Report / Share details, Copy Log / Review first, and Hide Help / Keep waiting:
Android warmup/loading screen uses a mobile-width compact panel with readable styled progress:
Android post-launch startup status uses a framed mobile-width readable card:
Portal scale/readability acceptable:
Compact phone layout uses most of the usable screen height:
Compact phone layout avoids excessive internal panel margins:
Compact phone shell uses dense panel padding:
Compact phone layout uses dense vertical spacing between repeated launcher regions:
Compact phone uses rounded metric scaling so small labels and separators receive visible fractional scale without overflow:
Compact phone layout uses dynamic content width instead of a narrow fixed column:
Tablet/wide layout avoids a narrow fixed inner column:
Portal task flow is top anchored rather than vertically stranded:
Compact phone status appears as a readable vertical next-step card:
Compact phone status card is low-profile but still readable:
Compact status card uses an inline phase and next-action headline where width allows:
Compact status card stacks phase and next action without squeezing either label on narrow compact screens:
Compact status card reflows between inline and stacked phase/next-action headline layouts after rotation or keyboard viewport changes:
Compact status keeps normal progress text to one stable line while attention/failure guidance can expand:
Compact status uses short mobile detail copy and expands full raw status when tapped:
Launcher compact status tap-to-expand details supported:
Compact status exposes a visible Details / Hide cue in a touch-safe row:
Launcher compact touch-safe status detail button supported:
Launcher compact status detail cue supported:
Compact responsive numbered workflow step strip remains visible while scrolling:
Compact workflow step strip stays in one dense row on narrow compact viewports:
Compact workflow step strip uses unified 62px touch-height cells on narrow compact viewports:
Compact workflow step strip shows two-line visible labels, not only numbers/tooltips:
The compact workflow strip shows visible step labels such as Sign in / Account, Verify / Steam Guard, Files / Game files, and Play / Saves safe; it does not rely on hover-only tooltips:
Compact workflow step strip separates step numbers into small badges next to readable labels:
Compact workflow step number badges use readable shared compact detail-label sizing:
Launcher compact workflow step detail labels supported:
Launcher compact workflow unified touch height supported:
Tapping compact workflow step labels scrolls directly to visible matching task sections or the current safe fallback task:
Compact workflow/current-task jumps leave padded space below the sticky header instead of pinning the section flush to the top:
Compact workflow step strip uses the same larger touch height as compact task actions:
The compact workflow strip is touch-safe enough for Android while keeping two-line step labels readable:
Compact current-task bar remains reachable while scrolling:
Compact current-task bar uses app-like task title wording:
Compact current-task bar uses short Sign in / Verify / Files / Play title labels without a status prefix:
Compact current-task bar uses contextual task detail labels:
Compact current-task bar renders task/context labels as structured title/detail labels:
Compact current-task bar is touch-safe but still compact:
Compact inline current-task bar is dense while remaining touch-safe:
Compact inline current-task bar uses the shared touch-safe compact control height:
Compact current-task bar and workflow strip share a tight sticky header:
Compact current-task bar and workflow strip share one inline sticky row when width allows:
Compact stacked current-task row is low-profile on narrow compact viewports:
Compact sticky task header is grouped inside a low-profile toolbar shell:
Compact sticky task header stacks on narrow compact viewports:
Compact sticky task header keeps the narrow workflow row dense enough to leave action content visible:
Compact sticky task header reflows between inline and stacked layouts after rotation or keyboard viewport changes:
Compact active task remains re-anchored after rotation or keyboard viewport changes:
Compact two-line controls use the readable shared detail-label font:
Status card shows a clear guided next action for the current state:
Failure/blocked/crash statuses show attention/fix guidance before normal install/cloud/launch guidance:
Primary actions use clear task wording, for example sign in/start game/verify code:
Primary launch action consistently says Start Game:
Primary and secondary actions are large enough to tap comfortably:
Compact retry/failure recovery button is primary and uses Try Again / Restart task labels:
Launcher background has visible branded atmosphere without reducing readability:
Buttons use high-contrast rounded action styling:
Compact phone header uses shortened subtitle/chrome:
Compact phone brand header is a single low-profile row:
Compact phone brand subtitle remains readable at phone scale:
Compact phone brand subtitle uses plain app copy instead of pipe-separated labels:
Compact phone header leaves more first-action area visible:
Compact phone section headers avoid repeated subtitle blocks:
Compact phone section headers stay compact and leave controls visible:
Compact phone section headers keep title and readable task cue in one dense row without clipping the title:
Compact phone section headers use explicit short cues Steam account, Current code, Local files, and Play safely instead of clipped desktop subtitles:
Compact Game Install shows selected version and Download before optional version details:
Compact install primary action says Download Version / Local files only:
Compact install primary action supports Redownload Version / Rebuild local files and Retry Download / Local files only:
Compact Game Install selected-version summary is a readable touch-safe card with Cloud unchanged cue and Change / Change version shortcut:
Compact Game Install selected-version summary shortcut opens the version drawer without changing branches or cloud saves:
Compact version dropdown is large enough to read and tap when expanded:
The compact game-version dropdown is large enough to read and tap when the version drawer is expanded:
Opened compact game-version dropdown popup rows have larger spacing/padding and are touch-safe:
Opening the compact game-version dropdown shows larger touch-safe popup row spacing and horizontal padding:
Compact install-version dropdown and refresh controls share one row when width allows:
Compact version drawer controls show Local files only and Update branch list detail labels:
Compact version drawer controls render Change Version / Local files only and Refresh Versions / Update branch list as structured title/detail labels:
Compact expanded version helper says Files for / Play version with Default files or Separate files and short branch status:
Compact phone version details start collapsed:
Version details expand/collapse without changing selected version:
Compact Play and Sync ready-version summary is a readable touch-safe card with Save Check and Upload locked cues:
Compact Play and Sync ready-version summary shortcut opens Save Check without unlocking Push:
Compact Play and Sync keeps the ready summary, Save Check shortcut, Get-saves-first cloud controls, and Start Game before version management:
Compact Play and Sync keeps save backup and cloud sync options below Start Game as optional controls:
Compact download progress appears directly below the disabled DOWNLOADING primary action:
Compact download progress status stays as a stable two-line Downloading selected version label:
Compact download progress uses a taller styled percentage bar:
Compact optional drawer toggles are low-profile but still tappable:
Compact optional drawer toggles use dense touch-safe height:
Compact optional drawer toggles use a dense touch-safe height instead of the older tiny drawer rows.
Compact drawer toggles and dense workflow controls share the same touch-safe compact height:
Compact optional drawer toggles are shorter than primary action buttons:
Compact optional drawer toggles are visibly shorter than primary action buttons while still tappable:
Compact optional drawer toggles remain tappable without taking full primary-action height:
Compact Play/Sync drawer toggles show detail labels for version target, Save Check / Get saves first, backup/sync settings, and recovery tools:
Compact collapsed cloud-safety drawer reads Save Check / Get saves first so it does not look like the Get Steam Saves action:
Compact Play/Sync action buttons render title/detail labels as structured two-line controls:
Compact launch CTA says Start Game / Ready version:
Compact recovery/tools actions use a two-column support grid when width allows:
Compact recovery/tools actions stack full-width on narrow compact viewports:
Compact support tools drawer shows Fixes & Help / Repair tools when closed and Hide Fixes / Back to play when open:
Compact launcher-log copy action says Copy Log / Review first:
Compact phone cloud safety starts collapsed:
Compact cloud-safety cue appears before Pull/Push controls:
Compact expanded cloud-safety detail says Saves for and Get Steam saves before upload / Upload can overwrite Steam:
Cloud safety expands/collapses while preserving Pull/Push controls:
Compact phone cloud options start collapsed:
Cloud options expand/collapse while preserving Pull/Push controls:
Compact Save Backup and Cloud Sync options use Local safety and Steam saves detail labels and share one low-profile row when width allows:
Compact Save Backup and Cloud Sync options stack full-width on narrow compact viewports:
Pull/Push controls appear before lower-frequency cloud options:
Pull from Cloud appears before Push to Cloud:
Compact cloud labels name Pull as Android-directed and Push as Steam-directed:
Compact Pull action says Get Steam Saves / Download to Android:
Compact locked Push toggle renders Upload Locked / Review first and Hide Upload / Keep locked as structured title/detail labels:
Compact unlocked Push actions say Upload to Steam / Overwrite cloud and Confirm Upload / Overwrite cloud:
Compact armed Push warning says Steam Cloud overwrite / Confirm only after Pull/local saves are verified:
Compact Get Steam Saves and locked Steam upload share one two-button row when width allows:
Compact Get Steam Saves and locked Steam upload stack with Get Steam Saves first on narrow compact viewports:
Push to Cloud guarded by confirmation:
Armed Push state shows overwrite warning before final confirmation:
Branch, redownload, cache, and final Push confirmations use contextual confirm/cancel labels instead of generic OK/Cancel buttons:
Long compact confirmation warnings are scroll-safe and keep confirm/cancel buttons reachable:
Confirmation dialogs use the current visible viewport after rotation or keyboard-driven viewport changes:
Compact phone buttons use short labels without losing meaning:
Compact recovery/tools buttons show user-facing labels Safe Start / Cloud off, Check Files / Updates, Game Versions / Refresh list, Repair Files / Rebuild game, Free Space / Old versions, Help Report / Share details, Last Problem / Open details, and Copy Log / Review first:
Screenshot captured with no secrets visible: yes / no
Screenshot path:
```

## Native credential entry behavior

```text
Username tap opens appropriate keyboard:
Password tap opens password keyboard:
Username keyboard next action focuses password:
Next button focuses password and requests password suggestions:
Password keyboard done action attempts submit:
Empty username shows inline guidance/error:
Empty password shows inline guidance/error:
Cancel clears visible credential fields:
Android Back clears visible credential fields and returns to launcher:
Android Back or Cancel allows immediate retry without waiting:
Submit/Cancel/Back do not trigger pre-auth provider save prompt:
Submit clears visible credential fields:
Credential handoff does not log username/password:
Credential fields are not restored after panel reopen:
Manual login starts Steam authentication:
```

## Password-manager suggestions

Record each provider separately. If no provider is installed/configured, state that explicitly.

```text
Samsung Pass available:
Samsung Pass suggestion appeared on username/password field:
Samsung Pass suggestion selected successfully:
Google Password Manager available:
Google Password Manager suggestion appeared on username/password field:
Google Password Manager suggestion selected successfully:
Other provider name:
Other provider suggestion appeared:
Provider behavior notes:
```

If suggestions do not appear, keep this as a validation limitation unless manual login also fails.

## Steam Guard and failed-login recovery

```text
Wrong password produces recoverable failure:
After wrong password, integrated login panel can reopen:
Failed-login status gives clear retry guidance:
Failed-login status states Steam passwords are not stored:
Connection/session failure gives connection-specific recovery guidance:
No credential values appear in logs after wrong password:
Steam Guard required: yes / no
Steam Guard prompt visible when required:
Steam Guard field accepts alphanumeric input:
Compact Steam Guard code field and Verify button are large enough to tap comfortably:
Compact Steam Guard shows code field and Verify Code before helper copy:
Compact Steam Guard code field and Verify Code share one touch-safe action row when width allows:
Compact Steam Guard code field and Verify Code stack full-width on narrow compact viewports:
Compact Steam Guard code/action row reflows between inline and stacked layouts after rotation or keyboard viewport changes:
Compact Steam Guard submit action says Verify Code / Submit once:
Compact Steam Guard helper stays bounded to two readable lines below the code controls:
Compact Steam Guard retry keeps the rejected-code title short and moves latest-code guidance into the helper below the code controls:
Steam Guard section states code is submitted once and never stored:
Wrong Steam Guard code produces recoverable failure:
Wrong Steam Guard code asks for the latest Steam Guard code:
Correct Steam Guard code proceeds:
```

## Successful authenticated return

```text
Successful login returns to launcher:
Saved Steam session works without re-entering password:
Game version dropdown visible/readable:
Refresh game versions action visible/readable:
Selected branch helper text readable:
Version/download guidance states local game files are separate from Steam Cloud saves:
Ready-state version details repeat that Steam Cloud saves move only through Pull/Push:
Download/update action visible/readable:
Play and Sync section appears when actions are available:
Support/diagnostics controls remain secondary:
```

## Download, cloud, and launch smoke

```text
Selected game version:
Game files already present before test: yes / no
Download/update completed:
Pull from Cloud completed:
Android local save/profile evidence exists before Push:
Pull action clearly says it copies Steam Cloud saves to Android:
Push action clearly says it sends Android saves to Steam Cloud:
Push action warns it can overwrite remote saves:
Push to Cloud guarded by overwrite confirmation:
Push to Cloud attempted: yes / no
Push to Cloud completed:
Game launch completed:
Profile/save visible in game:
Return/restart behavior acceptable:
```

## Required sanitized diagnostics

Paste only sanitized lines:

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
Launcher compact workflow step number badges supported:
Launcher compact readable workflow step number badges supported:
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
Selected game branch:
Selected game version name:
Selected game version slot kind:
Selected game files ready:
Manual Pull evidence marker present:
Current important Android local save evidence present:
Baseline manual Push prerequisites satisfied:
Latest manual Push evidence outcome:
```

## Result

```text
Pass / fail / partial:
Blocking issues:
Non-blocking polish issues:
Evidence package path:
GitHub issue links:
Reddit/user report links:
Follow-up required before release signoff:
```

Release signoff is not valid unless this template proves manual entry, password-manager behavior or limitation, Steam Guard/recovery behavior, successful authenticated return, cloud actions, game launch, portal readability, and secret-safe diagnostics on ARM64 hardware.
