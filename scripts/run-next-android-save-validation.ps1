[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

throw @"
This legacy one-command Android validation path is disabled.

It intentionally does not build, install, launch, force-stop, clear app data, clear
logcat, or trigger a Steam operation. Stage 5 requires a byte-for-byte verified
save export before any device lifecycle or install action, followed by an unchanged
candidate APK bound to its source commit and installed-APK hash.

After those prerequisites are satisfied, collect read-only evidence with:
  .\scripts\start-android-save-validation-capture.ps1 -DeviceSerial <serial> -ApkPath <exact-candidate.apk> -SourceCommit <40-character-commit> -PackageName com.sts2launcher.overhaul.fork.dev -Stage5Row <1-10> -EvidencePhase <phase> -LogcatSince "MM-dd HH:mm:ss.fff"

See docs/android-release-validation.md for the current procedure.
"@
