# Local Steam login emulator testing

For emulator-only Steam login testing, create an untracked credential file at:

```text
tmp\steam-login.local.json
```

Example schema:

```json
{
  "username": "your_steam_username",
  "password": "your_steam_password",
  "shared_secret": "optional Steam mobile authenticator shared secret"
}
```

Use `guard_code` instead of `shared_secret` if you only want to submit one current short-lived code manually:

```json
{
  "username": "your_steam_username",
  "password": "your_steam_password",
  "guard_code": "ABCDE"
}
```

Run:

```powershell
.\scripts\login-emulator.ps1 -Launch
```

Or pass a current short-lived Steam Guard code without writing it to the credentials file:

```powershell
.\scripts\login-emulator.ps1 -Launch -GuardCode ABCDE
```

To avoid putting the code in shell history or echoing it to the terminal, prompt for it interactively:

```powershell
.\scripts\login-emulator.ps1 -Launch -PromptForGuardCode
```

Do not commit `tmp/steam-login.local.json`.

This file is intended for local automation only. Scripts should consume it without printing credential values to the terminal or logs.

Current status: this harness can reduce repeated manual typing and can prove whether the login flow reaches Steam Guard, successful authentication, ownership verification, or a known crash signature. It does not by itself prove the ARM64 phone path, and Steam startup authentication failures remain an open issue until fresh logs show a successful startup/login/ownership sequence.

To rebuild the local emulator APK with the tracked SteamKit Android crypto patcher:

```powershell
.\scripts\build-android-local.ps1 -VersionName 0.2.15-local-loop-test -VersionCode 215014
```

The build script publishes `STS2Mobile`, stages `STS2Mobile.dll` and `SteamKit2.dll`, patches SteamKit Android crypto calls, and packages the APK with Gradle.

To check a captured logcat file for the repeated native crypto crash regression:

```powershell
.\scripts\check-login-crash-log.ps1 -LogcatPath tmp\login-local-loop-boundary-logcat.txt
```

The checker fails on fatal native crypto signatures and passes when the login reaches Steam Guard, successful authentication, or ownership verification.
It should not be used to mark startup Steam authentication fully fixed unless the relevant run reaches successful authentication or ownership verification.

For post-Steam Guard verification, require successful authentication or ownership verification:

```powershell
.\scripts\check-login-crash-log.ps1 -LogcatPath tmp\login-boundary-post-2fa-logcat.txt -RequirePostSteamGuard
```

To install the latest local APK, submit stored credentials, capture logcat/screenshot, and run the crash checker:

```powershell
.\scripts\test-login-boundary.ps1
```

This verifies the repeated SteamKit native crypto crash does not reappear before the Steam Guard boundary.

To verify the full post-2FA login path, pass a current Steam Guard code or include `guard_code`/`shared_secret` in the local credentials file:

```powershell
.\scripts\test-login-boundary.ps1 -GuardCode ABCDE
```

Or prompt for the code interactively without echoing it:

```powershell
.\scripts\test-login-boundary.ps1 -PromptForGuardCode
```

Or enter the code directly in the emulator UI without passing it through the script and let the harness wait for a terminal post-2FA signal:

```powershell
.\scripts\test-login-boundary.ps1 -WaitForPostGuardResult
```

This waits for successful authentication, ownership verification, or a crash signature before capturing evidence. To increase the wait:

```powershell
.\scripts\test-login-boundary.ps1 -WaitForPostGuardResult -PostGuardResultTimeoutSeconds 300
```

Prompted Steam Guard entry is handed to the app through a one-shot file in the app-specific external files directory. The launcher consumes and deletes `steam_guard_code.txt` while waiting for SteamKit's 2FA callback. This avoids unreliable Android text-field injection into Godot `LineEdit` controls.

You can also press Enter manually once the code has been submitted:

```powershell
.\scripts\test-login-boundary.ps1 -WaitForManualGuardSubmit
```

If the emulator is already sitting at the `Enter Steam Guard code` screen, submit only the code and capture the decisive post-2FA evidence:

```powershell
.\scripts\submit-steam-guard-and-capture.ps1
```

For unattended manual timing, use a fixed wait:

```powershell
.\scripts\test-login-boundary.ps1 -WaitForManualGuardCode -ManualGuardWaitSeconds 180
```

When Steam Guard material is available, prompted, or entered manually, `test-login-boundary.ps1` automatically requires successful authentication or ownership verification. Reaching only the Steam Guard prompt is not enough for the post-2FA gate.

The default post-2FA artifacts are:

```text
tmp\login-boundary-post-2fa-logcat.txt
tmp\login-boundary-post-2fa.png
```
