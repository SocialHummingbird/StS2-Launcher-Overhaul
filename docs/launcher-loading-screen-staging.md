# Launcher loading screen staging

## Stage 1 inventory

The current launch/loading path is split across two screens:

- Android native splash: `android/res/values/themes.xml` defines `GodotAppSplashTheme`, and `android/src/com/game/sts2launcher/GodotApp.java` installs the AndroidX splash screen before Godot starts.
- Godot post-launch warmup/status: `ShaderWarmupScreen` displays shader warmup progress, and `LauncherStartupStatus` displays game-startup phase text after the launcher closes.

No main launcher loading screen currently uses a large bitmap. The fixed-asset hangover was the native splash default icon plus fixed-size/fixed-position startup status layout.

## Stage 2 adaptive scaling patch

Implemented in this stage:

- Native splash uses the scalable launcher vector icon instead of Android's default system app icon.
- Shader warmup panel scales from the short viewport edge, not only the long edge, so short/wide Samsung-style landscape screens do not inflate text and controls beyond the available height.
- Shader warmup panel width is clamped with safe side margins and keeps a bounded aspect-friendly layout.
- Warmup status/detail labels use word wrapping.
- Game startup status is anchored top-wide with viewport-derived safe margins instead of a fixed `(24, 24)` position.

## Remaining stages

Stage 3 should add richer launch progress copy:

- current phase such as `Checking files`, `Preparing Steam session`, `Starting game`, or `Recovering startup`;
- last meaningful launcher log line;
- a visible diagnostics shortcut if startup stalls.

Stage 4 should replace the transitional loading surface with a fully responsive branded layout:

- scalable icon/logo treatment using the orange/cyan launcher identity;
- real Godot controls for all text and actions;
- responsive composition for phones, foldables, tablets, notches, and navigation bars;
- no text baked into images.

Stage 5 should validate on a device matrix:

- short/wide Samsung-style landscape screen;
- normal phone landscape;
- foldable inner display;
- tablet landscape;
- high-DPI small-height viewport.
