# Android startup responsiveness harness

This isolated APK compiles the production `AndroidStartupPreparation.java` directly.
It checks real touch input while preparation is blocked, main-Looper route delivery,
pause/resume deferral, stale completion after destruction, and failure-result/duplicate
handling. It does not load Godot, bypass the x86 guard, or access launcher data.

From the repository root, use JDK 17, Android SDK 35, and Gradle 8.14.3 on `PATH`,
as described in [development prerequisites](../../docs/development.md#prerequisites).
This Java-only harness does not need the game or staged .NET runtime inputs.

```powershell
gradle -p tools/AndroidStartupHarness assembleDebug
adb -s emulator-5554 install -r tools/AndroidStartupHarness/build/outputs/apk/debug/AndroidStartupHarness-debug.apk
adb -s emulator-5554 shell am instrument -w -r com.sts2launcher.startup.harness/com.game.sts2launcher.StartupHarnessInstrumentation
```

Require four `PASS` lines, `PASS: 4 Android main-Looper startup tests`, and
`INSTRUMENTATION_CODE: -1`. Pair this with the production x86 APK and
`scripts/test-android-emulator-startup.ps1`. Neither test proves ARM64 title-screen
or gameplay stability.
