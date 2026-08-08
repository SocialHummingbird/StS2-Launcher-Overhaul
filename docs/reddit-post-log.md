# Reddit Post Log

This log tracks public Reddit posts and comment threads connected to StS2 Mobile / StS2 Launcher Overhaul. Use it for support follow-up, bug hunting, recurring-user-feedback tracking, and future announcement planning.

Do not copy private user data into this file. Keep direct quotes short and only record public thread-level bug signals, links, and follow-up status.

## Current Support Posture

- GitHub issues are the tracked support channel.
- Reddit is useful for visibility and lightweight feedback, but bugs should be redirected to GitHub with exact APK, device, branch, screenshot, and focused logs.
- Public wording must state that the launcher is unofficial, not affiliated with or endorsed by Mega Crit Games, and requires a Steam-owned copy of Slay the Spire 2. No game files, assets, or Workshop content are bundled.
- Do not describe the project as an official mobile port, an official Android release, or a sanctioned replacement for any future official mobile version.
- Current public claims should match the latest GitHub docs: `v0.2.400` publishes automatic PowerVR-to-OpenGL touch compatibility while retaining the v0.2.399 crash fix and public mod runtime evidence on top of the redesigned five-destination launcher; Workshop/mod support remains beta-quality; Quick Restart has real in-game proof; SavesMerger uses a launcher substitute rather than direct payload loading; actual Pixel/PowerVR confirmation is still required.
- Current release link for replies: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.400-powervr-touch-compat
- Steam Cloud Push must not be encouraged for modded-save testing. Users should Pull first and avoid Push unless they understand overwrite risk.

## Posts

### 2026-06-09 - Original StS2 Mobile / Overhaul Announcement

- Reddit: https://www.reddit.com/r/slaythespire/comments/1u14fbg/sts2launcheroverhaul/
- Subreddit: `r/slaythespire`
- Author: `SocialHumingbird`
- Title: `StS2-Launcher-Overhaul`
- Thread role: first broad public announcement for the fork/overhaul.
- Approximate status when reviewed: strong initial reception compared with the later mod-support post; Reddit JSON showed about `57` score and `0.88` upvote ratio during the July 3 review.
- Release linked in original post: `v0.2.178-cloudpush-icon`.
- Current replacement release to point users to: `v0.2.400-powervr-touch-compat`.

Main post claims at time of posting:

- Unofficial Android launcher/runtime adaptation for Slay the Spire 2.
- Requires a Steam account that owns StS2.
- No game files/assets bundled.
- ARM64 Android target.
- Steam login, game download, Pull from Cloud, Push hardening, and game launch/profile visibility were working in testing.
- Project was still polish/hardening rather than a finished release.

Comment-derived bug/support signals:

- Launch failure reports appeared early; one user later confirmed an update fixed their issue and said it was working well.
- Beta branch support was requested and later became a development priority.
- Mod support was requested, especially for controller-related mods.
- Missing combat/audio SFX was reported and should remain a possible regression category.
- Controller input on Android handhelds was reported as partial: launcher/menu navigation worked, but in-game actions did not.
- One user reported latest mod-support build working well but asked for UI scale adjustment.
- One user reported shader compilation crash/stall: https://www.reddit.com/r/slaythespire/comments/1u14fbg/comment/ou7jovo/
- One user on Samsung Flip 7 reported being unable to scroll the beginning menu far enough to reach the Play button.
- A user asked how to uninstall, showing install/update/uninstall guidance needs to be obvious.
- Ekyso stated the original launcher was not dead and had a v0.3.x alpha rewrite in Discord.

Follow-up implications:

- Keep README/testing docs explicit about exact release tag, APK filename, install/update/uninstall behavior, and where to file logs.
- Ask users to retest launcher scaling/scroll reachability on `v0.2.400`; treat any remaining clipping, unreachable destination, keyboard overlap, or rotation failure as a redesign regression.
- Keep controller input, shader compile stability, and audio SFX as visible known-test areas.
- Avoid claiming replacement of the original launcher or any future official mobile release; frame this as an unofficial community Android compatibility path.

### 2026-07-02 - Mod Support Announcement

- Reddit: https://www.reddit.com/r/slaythespire/comments/1ufm8sz/slay_the_spire_2_emulator_with_mod_support/
- Subreddit: `r/slaythespire`
- Author: `SocialHumingbird`
- Title: `Slay the spire 2 emulator with mod support`
- Thread role: mod-support announcement and request for mod/launcher issue reports.
- Approximate status when reviewed: lower traction than the first post; Reddit JSON showed about `2` score, `0.54` upvote ratio, and `12` comments during the July 3 review.
- Release linked in original post: `v0.2.335-mod-selector-deps-cloud-marker-debug`.
- Current replacement release to point users to: `v0.2.400-powervr-touch-compat`.

Main post claims at time of posting:

- Mod support had been added.
- Latest game version with mods was usable, but the app remained a work in progress.
- Some mods would not work on mobile.
- Users should file issues on GitHub.
- Mod selector validation covered vanilla mode, modded mode, BaseLib, Quick Restart 2, SavesMerger, disabled SavesMerger route, and Steam Cloud Push safety markers.
- SavesMerger was validated as loading, but full save-merge behavior was not signed off.

Comment-derived bug/support signals:

- User asked whether there is a Discord.
- User asked whether general stability issues had improved since the previous post.
- User asked whether the updated version runs the newest stable.
- Positive lightweight engagement appeared, but the thread did not generate many detailed bug reports.

Follow-up implications:

- Future Reddit updates should lead with current public/default and public-beta support before mod support.
- Use careful wording: "mod support is available for testing" or "early mod support", not "mod support is done".
- Always include the current release link and avoid leaving old APK links as the most prominent install route.
- State that SavesMerger loading/scanning is not the same as full save-merge signoff.
- Ask users to file GitHub issues with device, Android version, exact APK, selected branch, selected mods, Pull/Push state, screenshot, and focused logs.

### 2026-07-03 - Public-Beta Runtime-Pack / Mod Testing Update

- Reddit: https://www.reddit.com/r/slaythespire/comments/1umajqe/sts2_mobile_launcher_update_beta_branch_fixes/
- Profile source: https://www.reddit.com/user/SocialHumingbird/
- Subreddit: `r/slaythespire`
- Author: `SocialHumingbird`
- Title: `StS2 Mobile launcher update: beta branch fixes, runtime-pack support, and mod testing`
- Thread role: update post for the `v0.2.352-savemerger-compat-local` release, now superseded by `v0.2.400-powervr-touch-compat`.
- Status when first reviewed: posted minutes earlier; no comments visible yet.
- Release linked in post: `v0.2.352-savemerger-compat-local`; current replacement release is `v0.2.400-powervr-touch-compat`.

Main post claims at time of posting:

- The app works with the new beta branch.
- More mod support fixes and general improvements are included.
- Latest tester APK is `StS2Launcher-v0.2.352-savemerger-compat-local-arm64-v8a.apk`.
- Current validation covers ARM64 hardware, public/default launch, latest tested public-beta payload `v0.108.0`, matched beta PCK/runtime, public-after-beta branch switching, and runtime-pack creation/validation.
- Early Workshop/mod support is available.
- Current tested modded public-beta path selects/scans BaseLib, Quick Restart 2, and manually imported Vanilla and Modded Saves Merger.
- The strict validation recorded `playMode=modded`, `scannedRoots=3`, and `enabledMods=3`.
- Caveats are explicit: prerelease/tester software, beta-quality mod support, incomplete SavesMerger real-save validation, controller testing needed, shader compilation may stress some devices, UI scaling/scrolling still being improved, and no Steam Cloud Push during validation.

Initial follow-up plan:

- Watch for comments reporting install/update trouble, public-beta download/launch failure, mod selector confusion, SavesMerger save visibility, controller input, shader compile stalls, and UI reachability.
- Reply by asking for GitHub issues with exact APK tag, device model, Android/One UI version, selected branch, selected mods, Pull/Push state, screenshots, and focused logcat.
- If comments repeat the same issue, open or update a GitHub issue rather than leaving the report only on Reddit.

## Current Reddit Reply Snippets

Use these as starting points for future Reddit replies. Update release links before posting.

### Stability / Should I Switch?

The latest ARM64 tester APK is here:

https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.400-powervr-touch-compat

Public/default and the latest tested public-beta payload are working in current ARM64 validation. It is still an unofficial prerelease community launcher though, so I would treat it as tester-ready rather than final. It is not affiliated with or endorsed by Mega Crit Games, and it requires your own Steam copy of the game. The most useful bug reports are exact APK version, device model, Android version, selected branch, whether mods were enabled, a screenshot for UI issues, and a focused logcat for crashes.

### Mods / SavesMerger

Early mod support is available, but it is not finished. BaseLib, Quick Restart 2, and manually imported SavesMerger are selected/scanned in the latest public-beta validation without falling back to the old native failure route. The big remaining question is real save behavior: whether existing vanilla/modded saves become visible and loadable with SavesMerger enabled. Please avoid Steam Cloud Push while testing modded saves unless you deliberately want to overwrite cloud state.

### UI Too Small / Cannot Reach Button

`v0.2.400` includes the Home, Saves, Versions, Mods, and Help destination redesign. Please retest with that exact release and file a GitHub device compatibility or bug report with the APK tag, phone model, Android/One UI version, orientation, display size/font scale, active destination, and a screenshot showing any remaining clipping or unreachable control.

### Pixel / PowerVR Main-Menu Exit

Issue #34 is still open. The current evidence points to a Godot 4.5.1 OpenGL Compatibility bug on PowerVR rather than old hardware or failure to reach the game. Please include the exact APK, GPU/renderer lines, whether `NMainMenu` appeared, post-startup trace/heartbeat marker files, and a focused log that includes the dying game process if possible.

### Controller Problems

Controller support needs more device-specific evidence. Please report the controller/device model, connection mode, whether launcher navigation works, whether in-game actions work, and whether the behavior differs between vanilla and modded launch.

### Shader Compile Crash

First-run shader compilation can be heavy on some devices. The latest APK adds a bounded v7 shader warmup path and writes `last_shader_warmup_status.txt`; if it still crashes or stalls, please report exact APK, device model, Android version, how long it stayed on the compile screen, whether Android showed an app-not-responding dialog, that marker file if present, and a focused logcat around the crash/stall.

## Open Follow-Ups From Reddit

- Keep latest APK/release links current in README, release docs, and any future Reddit comments.
- Build a simpler "known issues / tester checklist" Reddit comment after each release.
- Track `v0.2.400` Samsung Flip 7 / foldable destination, rotation, clipping, and reachability regressions.
- Track whether Odin/Thor controller reports become GitHub issues.
- Track whether shader compile crash/stall reports identify a device class or build setting.
- Track whether SavesMerger reports prove real save visibility/loadability.
