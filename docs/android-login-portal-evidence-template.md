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
Safe first-run flow guidance visible:
On compact phone layout, safe-flow guidance starts collapsed:
Safe-flow guidance expands/collapses without hiding the primary task:
Titled Steam Sign-in section visible:
Native integrated Steam login panel opens automatically on Android:
Native username field visible:
Native password field visible:
No USE ANDROID AUTOFILL popup/helper dialog visible:
Panel states Steam password is never stored:
Inline status guidance visible:
Native login panel remains usable when the keyboard is open:
Native login panel can scroll if keyboard or small screen reduces available height:
Native login panel keeps Sign In and Cancel reachable with the keyboard open:
Native login controls are stacked/full-width and easy to tap:
Native login primary button says SIGN IN WITH STEAM:
Native login panel requests suggestions for username and password fields:
Native login panel requests suggestions again when username/password fields gain focus:
Native login NEXT: PASSWORD control focuses password field:
Password visibility toggle shows/hides password without storing it:
Password visibility resets to hidden after submit/cancel/reopen:
Android Back dismisses native login panel without exiting launcher:
Back/Cancel dismissal re-enables SIGN IN WITH STEAM immediately:
Back/Cancel dismissal hides the soft keyboard before returning to launcher:
Provider does not prompt to save unverified credentials before Steam authentication:
Diagnostics console hidden by default:
Diagnostics console opens only when requested:
Raw startup fallback failure text hidden from portal:
Portal scale/readability acceptable:
Compact phone layout uses most of the usable screen height:
Compact phone layout avoids excessive internal panel margins:
Compact phone layout uses dynamic content width instead of a narrow fixed column:
Tablet/wide layout avoids a narrow fixed inner column:
Portal task flow is top anchored rather than vertically stranded:
Compact phone status appears as a readable vertical next-step card:
Status card shows a clear guided next action for the current state:
Failure/blocked/crash statuses show attention/fix guidance before normal install/cloud/launch guidance:
Primary actions use clear task wording, for example sign in/start game/verify code:
Primary launch action consistently says START GAME:
Primary and secondary actions are large enough to tap comfortably:
Launcher background has visible branded atmosphere without reducing readability:
Buttons use high-contrast rounded action styling:
Compact phone header uses shortened subtitle/chrome:
Compact phone section headers avoid repeated subtitle blocks:
Compact phone version details start collapsed:
Version details expand/collapse without changing selected version:
Compact phone cloud safety starts collapsed:
Cloud safety expands/collapses while preserving Pull/Push controls:
Compact phone cloud options start collapsed:
Cloud options expand/collapse while preserving Pull/Push controls:
Pull/Push controls appear before lower-frequency cloud options:
Pull from Cloud appears before Push to Cloud:
Push to Cloud guarded by confirmation:
Armed Push state shows overwrite warning before final confirmation:
Compact phone buttons use short labels without losing meaning:
Screenshot captured with no secrets visible: yes / no
Screenshot path:
```

## Native credential entry behavior

```text
Username tap opens appropriate keyboard:
Password tap opens password keyboard:
Username keyboard next action focuses password:
NEXT: PASSWORD button focuses password and requests password suggestions:
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
Steam Guard field opens numeric keyboard:
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
Launcher titled state sections supported:
Launcher safe first-run guidance supported:
Launcher compact safe-flow guidance collapsible:
Launcher mobile-first compact layout supported:
Launcher compact dynamic content width supported:
Launcher tablet/wide content layout supported:
Launcher top-anchored portal content supported:
Launcher compact vertical status hero supported:
Launcher touch-first action targets supported:
Launcher primary action wording supported:
Launcher consistent START GAME CTA supported:
Launcher branded atmospheric background supported:
Launcher branded background explicit RGBA supported:
Launcher high-contrast rounded actions supported:
Launcher compact header chrome reduction supported:
Launcher compact section-header subtitle suppression supported:
Launcher compact version details collapsible:
Launcher compact cloud-safety guidance collapsible:
Launcher compact cloud options collapsible:
Launcher primary cloud actions before cloud options:
Launcher safer Pull-before-Push cloud ordering supported:
Launcher manual Push armed overwrite warning supported:
Launcher compact button labels supported:
Launcher version-install/cloud-save separation guidance supported:
Launcher diagnostics console hidden by default:
Launcher startup fallback raw banner suppressed:
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
