# Launcher UI/UX Improvement Review and Plan

Date: 2026-07-15

## Implementation Status

Published as [`v0.2.398-launcher-ui-redesign`](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.398-launcher-ui-redesign) from `codex/android-release-bootstrap`:

- Deterministic Godot 4.5.1 Mono desktop preview using an offscreen `SubViewport`; it does not start the launcher controller or contact Steam.
- Fixture states for signed out, Steam Guard, download progress, ready, and repairable error.
- Five stable destinations: Home, Saves, Versions, Mods, and Help.
- Bottom navigation on portrait/landscape phone layouts and top navigation on constrained wide/foldable layouts.
- Existing launch, download, branch, cloud, mod, repair, and diagnostic controls retain their original events.
- Steam Cloud Upload retains its locked reveal, arm callback, explicit overwrite confirmation, and final push event.
- The former compact workflow strip, automatic viewport re-anchoring, and ready-path runtime control reparenting are no longer used.
- A 20-screenshot matrix covers 1080x2400 portrait, 2400x1080 landscape, 2184x1968 foldable, and 1280x800 desktop layouts.
- Every matrix render validates visible control accessibility names, focusability, viewport bounds, and minimum touch/desktop target heights.
- An interaction contract drives the real view controls and verifies existing events across all five destinations. Steam Cloud Push is armed only far enough to prove the explicit confirmation gate; the final Push event is asserted to remain uncalled.

Run one fixture:

```powershell
.\scripts\run-launcher-ui-preview.ps1 -Fixture ready -Destination saves -Width 1080 -Height 2400 -TouchOptimized $true
```

Run the complete matrix:

```powershell
.\scripts\test-launcher-ui-preview.ps1
```

Exact final-build ARM64 viewport validation remains incomplete. The APK installed successfully over existing app data on Samsung `SM-F966B`, but the final unlocked portrait/landscape script could not run after the device disconnected. Steam Cloud Push must not be used for layout validation.

With one ARM64 device visible in `adb devices`, run:

```powershell
.\scripts\test-launcher-ui-device.ps1
```

The device script verifies the local APK checksum, refuses to capture while the secure keyguard owns the display, installs without clearing app data, records portrait/landscape screenshots and focused Android fatal/lifecycle evidence, restores rotation settings, and never taps launcher actions or Steam Cloud Push.

Local ARM64 packaging completed with:

- Version: `0.2.398-launcher-ui-redesign-local` (`398031`)
- Package: `com.sts2launcher.overhaul.fork.local`
- APK: `artifacts/android/StS2Launcher-v0.2.398-launcher-ui-redesign-local-arm64-v8a.apk`
- SHA-256: `52d05adf3a26ee8c2135edae6ceb986a2d021b99c4b92a4c00eebc7e4fa66d97`
- APK ABI/content and Android crypto patch verification passed.

The exact APK was installed on the ARM64 device. Final cover/inner destination and game-handoff evidence is not claimed until the device is reconnected, unlocked, and those checks complete.

## Review Basis

This review uses:

- Current launcher view, section, responsive-layout, theme, and scroll code under `src/STS2Mobile/Launcher`.
- ARM64 screenshots captured on Samsung `SM-F966B`, including ready, branch, mod, login, keyboard, and scrolled launcher states.
- Current workflow and safety requirements for Steam authentication, game download, branch selection, Steam Cloud, mods, repair, diagnostics, and game launch.

An Android AVD named `sts2_test_api36` is available locally. It is Android 36, Pixel 6, x86_64, 1080x2400. It is useful for APK routing and native fallback screens, but it cannot run the authoritative Godot/.NET launcher UI. The repository intentionally routes x86_64 to `NativeFallbackActivity` because the managed Godot path is unstable there.

The ARM64 Samsung `SM-F966B` remains the final hardware proof target. Earlier redesign builds established inner-display and cover-display rendering; exact build `398031` still requires the final unlocked destination, rotation, and game-handoff pass before physical validation is complete.

## Executive Assessment

The launcher is functionally broad but presents its internal capability map instead of the user's current task. It behaves like one long diagnostics form containing onboarding, game launch, saves, versions, mods, recovery, and support at the same time.

Current strengths:

- Important operations exist and are generally guarded.
- Start Game is visually prominent.
- Steam Cloud Push has explicit safety controls.
- The native Steam login panel is more focused than the main launcher screen.
- State markers and diagnostics make support work unusually strong.

Current weaknesses:

- Too many unrelated jobs share one scroll surface.
- Status and workflow information is repeated in several places.
- Nearly every control is a full-width outlined rectangle, so hierarchy is weak.
- Wide Android and foldable screens are forced into the same compact one-column layout as phones.
- Normal, selected, disabled, safe, warning, and destructive states rely heavily on cyan, green, red, and orange borders.
- Technical launcher terminology reaches users before it is needed.
- Dynamic visibility, child reordering, deferred scrolling, and viewport re-anchoring create fragile layout behavior.

Indicative scorecard:

| Area | Current | Target |
| --- | ---: | ---: |
| Core task clarity | 2/5 | 5/5 |
| Information architecture | 1/5 | 4/5 |
| Visual hierarchy | 2/5 | 4/5 |
| Responsive behavior | 2/5 | 5/5 |
| Safety communication | 3/5 | 5/5 |
| Accessibility/readability | 2/5 | 4/5 |
| Functional coverage | 4/5 | 5/5 |

## Priority Findings

### P0: One screen contains too many products

The ready screen exposes game launch, save transfer, backup settings, branch selection, vanilla/modded mode, each mod, Workshop sync, cache clearing, repair tools, and diagnostics in one vertical flow.

Impact:

- Users must understand the whole system before performing the common Play action.
- Advanced and dangerous actions compete with routine actions.
- The page grows whenever a feature is added.
- Every state change requires visibility and ordering logic across one large tree.

Required direction:

- Split the launcher into five stable destinations: Home, Saves, Versions, Mods, and Help.
- Keep only the current task and highest-value status on Home.
- Move destructive, diagnostic, and low-frequency operations out of the normal play path.

### P0: Wide Android layouts are misclassified

`LauncherLayoutProfile.ForViewport` sets `compact = true` for every Android device. A wide foldable therefore receives a full-width single column instead of a tablet/foldable layout.

Impact:

- Rows become extremely wide.
- Large areas are empty while content remains vertically dense.
- The interface looks stretched rather than intentionally responsive.
- A wide screen still requires long scrolling.

Required direction:

- Separate input/touch sizing from layout mode.
- Choose phone portrait, phone landscape, foldable/tablet, and desktop-preview layouts from actual dimensions and aspect ratio.
- Use a navigation rail plus constrained content on wide/foldable screens.
- Use bottom navigation and one content column on phone portrait screens.

### P0: Scroll and reflow behavior is fragile

The compact UI dynamically reparents controls, changes visibility, calls `EnsureControlVisible`, then applies a second deferred scroll offset. Viewport changes can trigger another re-anchor. Existing screenshot evidence includes clipped headers and a scrolled state where almost all content disappears.

Impact:

- Rotation, keyboard display, and asynchronous state changes can move users away from the control they were using.
- Scroll position depends on deferred layout timing.
- Reordered controls make regressions difficult to reason about.

Required direction:

- Stop reparenting primary controls between groups at runtime.
- Give each destination a stable scene/tree structure.
- Preserve scroll position unless the user explicitly navigates to a different destination or a blocking step.
- Scroll to errors or focused inputs only, after one settled layout frame and with a clamped target.

### P1: Hierarchy is repeated rather than clarified

The ready state can show all of the following simultaneously:

- Brand header.
- Current-task button.
- Four-step workflow strip.
- Status capsule.
- `Play and Sync` section heading.
- Ready summary.
- Save Check button.
- Start Game button.

Impact:

- Multiple components claim to explain what happens next.
- Vertical space is spent restating state instead of enabling action.
- Users cannot tell which status is canonical.

Required direction:

- Use one app bar status and one page-level status block.
- Show onboarding progress only during onboarding.
- Remove the workflow strip from the normal ready state.
- Make Start Game the only primary action on Home.

### P1: Visual language overuses outlined rectangles

Most rows, groups, toggles, summaries, status panels, and actions use similar rectangular outlines. Green and red styles are also used for ordinary selectable modes and Workshop actions.

Impact:

- Buttons, state summaries, navigation, and warnings look interchangeable.
- Red suggests danger even when a mode is merely inactive.
- Cyan borders dominate the screen and reduce emphasis.

Required direction:

- Reserve cards for status summaries and repeated list items.
- Use unframed page sections and compact list rows for settings.
- Use segmented controls for Vanilla/Modded.
- Use switches or checkboxes for Cloud Sync, backup, and mod enablement.
- Reserve red for destructive actions and blocking errors.
- Reserve green for confirmed success, not normal navigation.
- Add familiar icons to navigation and commands through a consistent icon asset set.

### P1: Labels expose implementation concepts

Examples include `public legacy`, `runtime pairing`, `version target`, `save check`, `staged`, and multiple variations of Pull/Download/Get and Push/Upload.

Impact:

- Users must translate internal architecture into intent.
- Similar actions appear inconsistent.
- Short secondary labels do not always explain why an action is blocked.

Required direction:

- Standardize user terms:
  - `Play` or `Start Game`.
  - `Download saves` and `Upload saves`.
  - `Game version`.
  - `Mods`.
  - `Repair`.
  - `Help and diagnostics`.
- Keep branch IDs, hashes, runtime slots, and pairing evidence inside expandable technical details.
- Explain blocked actions with one sentence directly below the control.

### P1: Mod management is not a list UI

Each mod is rendered as a large full-width action slot. Mode selection and Workshop maintenance use the same visual treatment.

Required direction:

- Use one Vanilla/Modded segmented control.
- Render mods as compact rows with name, source/status, compatibility badge, and an enable switch.
- Put Sync Workshop in the page toolbar.
- Put Clear staged files in an overflow/destructive menu.
- Show incompatible or missing imports as inline row errors.

### P2: Native login is focused but still oversized

The native Steam login panel is clearer than the main launcher, but explanatory copy and side-by-side secondary buttons consume substantial space above the keyboard.

Required direction:

- Keep the password-storage reassurance, but reduce it to one concise security line.
- Use trailing icons for password visibility and field progression.
- Keep one primary Sign in action and a standard back/cancel affordance.
- Verify the focused field and submit action remain visible with Samsung, Google, and third-party keyboards.

## Target Information Architecture

### Home

First viewport content:

- App bar with account/avatar, current game version, and Help icon.
- One concise readiness status.
- Primary Start Game button.
- Vanilla/Modded segmented control when mods are available.
- Compact save status showing last download/upload and whether upload is locked.
- Active download/update progress when relevant.

Home must not show cache cleanup, raw logs, every mod, or a normal Upload Saves action.

### Saves

- Current Android and Steam Cloud save status.
- Download saves as the safe primary action.
- Upload saves as a separated dangerous action with its prerequisites and confirmation.
- Local backup and cloud sync switches.
- Last operation result and timestamp.
- Technical evidence in an expandable details area.

### Versions

- Current branch/version and readiness.
- Available versions as a selectable list or menu.
- Download/update action for the selected version.
- Storage use per installed version.
- Remove old versions under an overflow menu.
- Private/password branch limitations shown only when relevant.

### Mods

- Vanilla/Modded segmented control.
- Enabled count and compatibility summary.
- Searchable/scannable mod list with switches.
- Workshop sync command in the toolbar.
- Missing import and incompatibility states on the affected row.
- Clear staged content as a destructive overflow action.

### Help

- Repair game files.
- Safe Start.
- Last problem summary.
- Create Help Report.
- Copy launcher log.
- Advanced technical details collapsed by default.

## Responsive Model

| Layout | Navigation | Content |
| --- | --- | --- |
| Phone portrait | Bottom navigation | One constrained column, page scroll only |
| Phone landscape | Compact navigation rail | One main pane, optional detail drawer |
| Foldable/tablet | Navigation rail | Main pane plus contextual status/detail pane |
| Desktop preview | Navigation rail | Constrained 960-1200 px operational surface |

Touch targets should remain at least 48 dp. Body copy should remain readable without relying on viewport-based font scaling. Fixed-format controls must use stable heights and grid tracks so status changes do not resize surrounding layout.

## Implementation Strategy

### Phase 0: Build a deterministic UI preview harness

Create a desktop Godot preview entry point that instantiates the launcher view with a fake controller/model and explicit fixture states. It must not contact Steam, read credentials, download files, touch saves, or launch the game.

Required fixtures:

1. Signed out.
2. Steam Guard required.
3. Download required.
4. Download in progress.
5. Ready on public.
6. Ready on a non-public branch.
7. Upload locked.
8. Mods available with mixed enabled/error states.
9. Repairable error.
10. Help/diagnostics expanded.

Required screenshot matrix:

- 1080x2400 phone portrait.
- 2400x1080 phone landscape.
- 2184x1968 foldable inner display.
- 1280x800 tablet/desktop preview.
- Portrait with a simulated keyboard viewport.
- Large text/accessibility scale.

This harness is necessary because the installed x86_64 AVD cannot run the real managed launcher and repeated physical-device state setup is too slow for design iteration.

### Phase 1: Replace the shell, preserve the backend

- Keep `LauncherController`, `LauncherModel`, Steam operations, diagnostics, and safety gates.
- Introduce stable page containers and navigation state.
- Add Home, Saves, Versions, Mods, and Help destinations.
- Remove dynamic control reparenting from the ready path.
- Keep the old shell behind a temporary local feature flag until fixture parity is complete.

### Phase 2: Rebuild the core Play flow

- Implement Home and onboarding first.
- Collapse completed onboarding steps.
- Show only the current blocking action.
- Make Start Game the sole Home primary action.
- Move save upload, branch management, mods, and support to their destinations.

### Phase 3: Migrate secondary workflows

- Build Saves with existing cloud safety gates unchanged.
- Build Versions around the existing branch catalog and readiness model.
- Build Mods as list rows with segmented mode and switches.
- Build Help around existing repair and report actions.

### Phase 4: Visual system and accessibility

- Define semantic color tokens for surface, text, primary, success, warning, danger, and focus.
- Reduce border density and cyan saturation.
- Add a consistent icon set.
- Standardize page title, section title, body, detail, and label typography.
- Add visible focus states and screen-reader labels.
- Audit text wrapping and touch targets at every fixture size.

### Phase 5: Device validation and rollout

- Install on ARM64 hardware without clearing app data.
- Validate portrait, landscape, foldable open/closed, keyboard, and font/display scale changes.
- Validate signed-out, Guard, download, ready, branch, cloud safety, mods, error, and diagnostics states.
- Confirm Steam Cloud Push remains guarded; do not use real Push during layout-only validation.
- Compare screenshots against fixture baselines and inspect for blank regions, clipping, overlap, and unexpected scroll jumps.
- Remove the old shell only after controller event parity and device screenshots pass.

## Acceptance Criteria

- Start Game is visible without scrolling in the ready Home state on all target viewports.
- Home has exactly one primary action.
- Steam Cloud Upload is not presented beside Start Game on Home.
- No destination exposes more than one destructive action without an overflow or confirmation boundary.
- The ready state does not show onboarding workflow chrome.
- Wide/foldable Android uses a constrained wide layout and persistent top navigation rather than a stretched phone layout.
- Rotation, keyboard display, and asynchronous status updates do not reset or jump scroll position.
- No screenshot has clipped text, overlapping controls, blank scrolled content, or controls outside the safe viewport.
- All actionable controls meet the minimum touch target and have accessible names.
- Technical branch/runtime/hash evidence remains available, but collapsed outside Help or technical details.
- Existing launch, download, branch, cloud safety, mod, repair, and diagnostic controller events retain parity.

## Recommended First Delivery

The first implementation milestone should contain only:

1. Desktop fixture harness.
2. New responsive shell and navigation.
3. Ready-state Home page.
4. Placeholder destinations wired to the existing old sections.
5. Screenshot baselines for phone portrait, phone landscape, and foldable.

That milestone will establish the new interaction and responsive model before cloud, branch, mod, and repair complexity is migrated.
