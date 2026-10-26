# PowerVR Input Compatibility Evidence - 2026-07-16

This report records the investigation and tests from 2026-07-16. A later reporter confirmation is linked at the end; the device results below remain specific to the named APKs.

## Reporter Evidence

GitHub issue #34 reporter testing used `v0.2.399-powervr-renderer-mod-runtime` on Pixel 10 Pro, Android 17, and PowerVR D-Series DXT-48-1536.

| Requested mode | Game starts | Touch | Reporter observation |
| --- | --- | --- | --- |
| Auto | Yes | No | Game visible but does not accept touch |
| Vulkan | Yes | No | Game visible but does not accept touch |
| OpenGL | Yes | Yes | First menu use is slow; gameplay and later menu use are normal |
| Safe Start | Yes | No | Same effective Vulkan behavior as Auto on `v0.2.399` |

In these tests, the original startup/process-teardown failure was no longer reproduced. The remaining problem was touch input on the PowerVR Vulkan path. The slower first OpenGL menu load suggested graphics compilation or cache creation, but the report did not establish its cause. The reporter evidence contained no new `NativeFallback`, Java fatal, native signal, ANR, or low-memory kill proving a new process crash.

## v0.2.400 Policy

`v0.2.400-powervr-touch-compat` captures the live Godot adapter name, adapter vendor, rendering driver, and rendering method in `graphics_device.txt` when the managed launcher starts. PowerVR, ImgTec, or Imagination evidence selects OpenGL in launcher preferences.

The Android restart boundary independently reads that evidence. On PowerVR it forces `--rendering-driver opengl3 --rendering-method gl_compatibility` for Auto, Vulkan, OpenGL, and Safe Start. The launcher keeps OpenGL selected and disables Auto/Vulkan with a compatibility tooltip so the visible controls match the effective policy. `last_renderer_attempt.txt` preserves the requested preference and records the effective mode plus whether PowerVR compatibility was active.

Non-PowerVR behavior remains unchanged:

- Auto and Safe Start remain unforced project-renderer launches.
- Vulkan remains explicit Vulkan Mobile.
- OpenGL remains explicit OpenGL Compatibility.

No upstream Slay the Spire 2 game files were modified. The change is confined to launcher GPU evidence, renderer routing, diagnostics, policy tests, and documentation.

## Connected Validation

Exact release APK:

```text
VersionName: 0.2.400-powervr-touch-compat-local
VersionCode: 400001
Package: com.sts2launcher.overhaul.fork.local
ABI: arm64-v8a
Device: Samsung SM-F966B
Android: 16 / API 36
GPU: Qualcomm Adreno 830
```

The APK installed as an update with app data preserved and passed APK content/ABI verification plus Android crypto-patch verification.

| Evidence path | Result |
| --- | --- |
| Real Adreno / Auto | Effective Auto; actual Vulkan; reached `NMainMenu`; touch opened Settings |
| Real Adreno / Vulkan | Effective Vulkan; actual Vulkan command-line renderer |
| Real Adreno / Safe Start | Effective Auto; actual Vulkan; reached `NMainMenu` |
| Real Adreno / OpenGL | Effective OpenGL; reached `NMainMenu`; touch opened character selection |
| Synthetic PowerVR marker / requested Vulkan | Effective OpenGL with PowerVR compatibility active; reached `NMainMenu`; three-second probe written; touch opened character selection |

The first integrated run exposed and fixed a parser regression where the text `PowerVR compatibility required: False` could match a naive PowerVR substring search. The native parser now honors the explicit boolean before vendor-name fallback, and the exact Adreno evidence file is a regression test.

The exact installed APK SHA-256 matched the release artifact: `623830caad7a684e3358fbb22564210a1236588e03e7161dfcf30cc5aa76cdc3`. The synthetic marker checked the native handoff and renderer command-line policy on the Adreno device. It did not reproduce the reporter's PowerVR driver. At this point, confirmation on the Pixel/PowerVR device was still needed before closing issue #34. Slow first-menu loading remained a compatibility tradeoff; this change did not redesign game content or shaders.

## Later reporter confirmation

After updating to `v0.2.401-native-modded-save-pull`, the reporter confirmed that **Auto loaded the game with working touch controls**. [Read the confirmation](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/issues/34#issuecomment-4998325130). Issue #34 was subsequently closed. This confirms that later build on the reporting device; it is not a new device test of the current APK.
