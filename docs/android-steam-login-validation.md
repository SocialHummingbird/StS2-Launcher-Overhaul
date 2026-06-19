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
- The Steam Guard field requests a numeric virtual keyboard.
- Samsung Pass, Google Password Manager, and other provider suggestions are still best-effort until validated on ARM64 hardware. Local fix28 evidence validates Samsung Pass/Android Autofill field recognition only; matched saved-credential suggestion behavior is still unproven because the provider reported no matched saved Steam credential.
- Steam refresh/session credentials are still saved through the encrypted Android Keystore path after successful SteamKit authentication. This is separate from password-manager behavior.
- The Steam Guard section states that codes are submitted once and never stored, and wrong-code recovery asks for the latest Steam Guard code.
- Failed login returns to sign-in with clear retry guidance and repeats that Steam passwords are not stored.
- Connection/session recovery failures use connection-specific guidance rather than treating every failure as a wrong-password case.

## Current ARM64 evidence

Local fix28 evidence is captured at `artifacts/android/fix28-native-login-panel-cancel-20260619-112725`, with a text-only public-redacted summary at `artifacts/android/fix28-native-login-panel-cancel-20260619-112725-public-redacted`.

This evidence proves:

- Removing the saved encrypted Steam session files shows the integrated native Steam login panel on ARM64 hardware.
- The panel exposes real Android username and masked password fields, full-width sign-in/cancel controls, the password-focus control, the password visibility control, and inline text that the Steam password is never stored by StS2 Mobile.
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
   - Diagnostics are hidden behind the diagnostics console drawer until explicitly opened.
   - Raw startup fallback failure text is not visible behind or above the launcher portal.
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
   - The `NEXT: PASSWORD` control moves focus to the password field and requests password suggestions.
   - The password keyboard action submits login.
   - The native login panel remains usable while the keyboard is open.
   - The native login panel can scroll if the keyboard or a small screen reduces usable height.
   - The native login controls are stacked/full-width and easy to tap.
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
   - The Steam Guard field opens a numeric keyboard.
   - Incorrect code handling remains recoverable.
   - Correct code proceeds to successful authentication or ownership verification.

5. `Failed login`
   - Wrong password or rejected authentication returns a clear failure state.
   - The user can retry from the integrated native credential panel.
   - No credential values are logged.

6. `Successful return`
   - Successful authentication returns to the launcher.
   - Saved session state is available without asking for the password again.
   - Download/update/cloud actions can use the saved Steam session.
   - The portal clearly exposes the next action without requiring diagnostic-log reading.
   - The Play and Sync section appears when launch/retry/cloud actions are available.
   - Cloud actions label Pull as Steam Cloud to Android and Push as Android saves to Steam Cloud.
   - Push warning text names overwrite risk before upload.
   - The diagnostics console remains hidden unless explicitly opened.

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
