# StS2 Launcher

<p align="center">
  <img src="docs/assets/sts2-mobile-icon.svg" alt="StS2 Launcher icon" width="128" height="128">
</p>

Play your Steam copy of **Slay the Spire 2 on Android**. StS2 Launcher handles the game download, version selection, mods, and Steam Cloud saves in one app.

**[Download the latest APK](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/latest)** · **[Getting started](docs/getting-started.md)** · **[Report a problem](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/issues)**

You need a Steam account that owns the game and an ARM64 Android device. The launcher does not include game files. This is an unofficial project, unaffiliated with Mega Crit, Valve, or Steam.

## What you can do

- **Download and launch the game.** Sign in to Steam, download your copy, and play from the Home page.
- **Keep different game versions installed.** Switch between available Steam branches, including public betas, without replacing every other version.
- **Sync saves with Steam.** The app checks for saves before loading and queues uploads after saving. The Saves page also lets you sync, download, or upload manually. Conflicting changes ask you which copy to keep.
- **Choose Vanilla or Modded play.** Download subscribed Workshop mods, enable the ones you want, and see how they loaded on the last launch. Mod compatibility varies; vanilla and modded saves stay separate.
- **Try graphics and startup options.** Choose Auto, Vulkan, or OpenGL. Safe Start skips shader warmup if normal startup gets stuck.
- **Connect over LAN.** Local-network multiplayer supports discovery and manual address entry. On PC, add `--fastmp` to the game's Steam launch options. This feature is experimental.
- **Update the launcher in the app.** It checks for new releases and offers an update. You can also check from Help; Android asks you to confirm installation.
- **Report bugs with useful logs.** Help opens a GitHub issue with redacted diagnostics already filled in. Review the report, add what happened, and submit it yourself.

## Get started

1. Download the ARM64 APK from [Releases](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/latest) and install it.
2. Open the launcher and sign in to the Steam account that owns Slay the Spire 2.
3. Choose a game version and download it. For your first install, use the regular public branch.
4. Start with Vanilla mode and press Play.

Already installed? Update over your existing app. **Do not uninstall or clear app data to update** — that removes local saves. See [installation and updates](docs/getting-started.md) if Android refuses the APK.

## Before you play

Android support is still experimental. Performance, audio, graphics, and mod compatibility depend on your device and game version.

The current release is **v0.2.432**. It moves startup work off the UI thread, avoids repeated archive hashing, and limits title-screen resource-loading bursts. Automated regression checks and emulator startup checks passed; full Android gameplay remains unverified for this release. See [release notes](docs/release-notes/v0.2.432.md) for the exact validation scope.

## Help and documentation

- [Getting started and updates](docs/getting-started.md)
- [Fixing common problems](docs/android-troubleshooting.md)
- [Saves, versions, mods, and developer guides](docs/README.md)
- [Current testing status](docs/current-android-status.md)
- [Build from source](docs/development.md) · [Contribute](CONTRIBUTING.md) · [Changelog](CHANGELOG.md)

Found a bug? Use **Help → Report a bug on GitHub** in the app, or [open an issue](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/issues/new/choose). Include your device, app version, and what you did before it happened.

## License

The launcher is [MIT licensed](LICENSE). Its dependencies have [separate terms](THIRD_PARTY_LICENSES.md), including FMOD and Spine. See the [unofficial project notice](docs/unofficial-project-notice.md).
