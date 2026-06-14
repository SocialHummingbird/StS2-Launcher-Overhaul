# Reddit announcement prep - 2026-06-09

## Current public claim

StS2 Mobile now works on the validated ARM64 Android path: install, Steam login, game download, Pull from Cloud, local hardening Push to Cloud, Pull-after-Push, local save handoff, and game launch/profile visibility have been validated. The newest public release asset is published, structurally verified, and installed/launched on ARM64 hardware, but full newest-public-release Pull/Push/game-launch smoke is still pending before release-candidate signoff.

## Public release to link

- Repository: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul
- Release: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.187-beta-art-fallback
- APK: `StS2Launcher-v0.2.187-beta-art-fallback-arm64-v8a.apk`
- Package: `com.sts2launcher.overhaul.fork.dev`
- Version: `0.2.187-beta-art-fallback` / `versionCode=218700`
- SHA-256: `f2ef1c3ef2149d4901fc1051058d44cfdb2e45afb1c7a9ef5693d4714d31dffe`
- Target: ARM64 Android phones only for real validation.

## Posting constraints checked

- Direct `r/slaythespire` rules access was attempted on 2026-06-09, but Reddit returned network-security/403 blocks from this environment. Treat subreddit-specific approval as unverified until checked from a logged-in browser or by modmail.
- Reddit's platform rules require community-rule compliance, authentic participation, no spam/disruptive behavior, no deception/impersonation, and respect for privacy. Source: https://redditinc.com/policies/reddit-rules
- Reddit Help says promotional content is not inherently spam, but some communities disallow promotional content and others use a 10% self-promotion convention. It also lists repeated same/similar posts, unrelated link-farmed posts, disguised redirects, harmful links, and promotional bots as spam examples. Source: https://support.reddithelp.com/hc/en-us/articles/28012014962580-How-do-I-keep-spam-out-of-my-community
- The announcement should disclose affiliation, avoid APK-only promotion, avoid reposting, and avoid asking users to post private data.
- If using a Slay the Spire 2-specific community, repeat the same mod-rule check before posting because subreddit rules can change.

## Must-say caveats

- Unofficial community project; Mega Crit is not involved.
- Requires a valid Steam account that owns Slay the Spire 2.
- No game assets are bundled or redistributed.
- ARM64 Android hardware is the support target; x86_64 emulator behavior is diagnostic-only.
- Pull from Cloud before Push to Cloud.
- Push to Cloud can overwrite Steam Cloud state. Only use it after verifying Android local saves are the intended state.
- Do not share credentials, guard codes, refresh tokens, private save data, or unsanitized logs in public.
- This is working, but still in polish/hardening rather than release-candidate signoff.

## Suggested Reddit title

StS2 Mobile now runs Slay the Spire 2 on ARM64 Android - working, but still in polish/hardening

## Suggested Reddit body

I have been working on an unofficial Android launcher for Slay the Spire 2: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul

The current ARM64 Android path now works in testing: the app installs, authenticates with Steam, downloads the game files from Steam, pulls Steam Cloud saves into Android local storage, launches the game, and shows the pulled profile in-game. Push to Cloud has also been validated on the local hardening build and the fix is included in the latest public APK. Recent public builds also include Samsung-layout reachability fixes, Steam login crash-boundary hardening, adaptive native splash/loading-screen scaling, and a responsive launcher shell with active download progress and the diagnostics console collapsed by default.

This is not a finished release-candidate yet. It is in the polish/hardening phase, and I am looking for technically comfortable testers who understand the risks.

Important caveats:

- You need a Steam account that owns Slay the Spire 2.
- No game assets are included in the repo or APK.
- Current real target is ARM64 Android phones, not x86_64 emulators.
- Pull from Cloud before using Push to Cloud.
- Push to Cloud can overwrite Steam Cloud saves, so only use it after verifying the Android local saves are the state you want in Steam Cloud.
- Do not post credentials, guard codes, tokens, private save contents, or full unsanitized logs publicly.

Latest public APK:
https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.187-beta-art-fallback

If you try it, the most useful feedback is device model, Android version, whether install/login/download/Pull/game launch worked, and any scrubbed crash log snippets. Please keep reports sanitized.

## Recommended posting approach

1. Prefer a text post linking to the GitHub repository and release, not a direct APK-only post.
2. Put the unofficial/no-assets/Steam-ownership caveats near the top.
3. Make the testing request explicit and avoid marketing language.
4. If posting to a subreddit with unclear self-promotion rules, send modmail first with the title/body above.
