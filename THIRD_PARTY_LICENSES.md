# Third-Party Licenses

This project uses the following third-party libraries and tools:

## Godot Engine

- **License**: MIT
- **Copyright**: (c) 2014-present Godot Engine contributors, (c) 2007-2014 Juan Linietsky, Ariel Manzur
- **Source**: https://github.com/godotengine/godot

## HarmonyLib (0Harmony)

- **License**: MIT
- **Copyright**: (c) Andreas Pardeike
- **Source**: https://github.com/pardeike/Harmony
- **Android build**: https://github.com/Ekyso/Harmony at `8f4bc82514c3c0a4c6651c00d9083cd939cad96f`, with the source changes and package locks in [patches/harmony](patches/harmony). The recipe records the complete patched source tree.

## MonoMod

- **License**: MIT
- **Copyright**: (c) 2015–2020 0x0ade and contributors
- **Source**: https://github.com/MonoMod/MonoMod
- **Android build**: The pinned Harmony source includes its MonoMod.Core, MonoMod.Utils and MonoMod.Iced source. MonoMod.Backports 1.1.2 and MonoMod.ILHelpers 1.1.0 are restored from locked NuGet packages.

## Mono.Cecil

- **License**: MIT
- **Copyright**: (c) Jb Evain and contributors
- **Source**: https://github.com/jbevain/cecil
- **Version**: 0.11.6

## Iced

- **License**: MIT
- **Copyright**: (c) 2018–present iced project and contributors
- **Source**: https://github.com/icedland/iced
- **Android build**: Included in the pinned MonoMod vendored source and built as MonoMod.Iced.

## SteamKit2

- **License**: LGPL v2.1
- **Copyright**: (c) SteamRE Team
- **Source**: https://github.com/SteamRE/SteamKit
- **Android build**: Restored from NuGet, then modified by [SteamKitAndroidPatch](tools/SteamKitAndroidPatch/Program.cs) during APK assembly. The patch redirects crypto calls to the launcher's Android helper. The upstream source is linked above; the local transformation is included in this repository.

## .NET Runtime (Mono)

- **License**: MIT
- **Copyright**: (c) .NET Foundation and Contributors
- **Source**: https://github.com/dotnet/runtime
- **Android build**: .NET 9.0.7 at `3c298d9f00936d651cc47d221762474e25277672`, with the constructor initialization guards and cache-maintenance export in [patches/mono](patches/mono). The pinned recipe retains the matching original BCL and records native output hashes in the launcher's embedded build manifest.

## FMOD

- **License**: Proprietary (FMOD EULA)
- **Copyright**: (c) Firelight Technologies Pty Ltd
- **Website**: https://www.fmod.com
- **Attribution**: Made using FMOD Studio by Firelight Technologies Pty Ltd.
- **Licensing**: Follow the [current FMOD licensing terms](https://www.fmod.com/licensing), including project registration and any applicable revenue and development-budget conditions. Free Indie licensing is available for qualifying projects; the licence is separate from this repository's MIT licence.
- **Build inputs**: FMOD binaries are not included in this source repository. Obtain the SDK from https://www.fmod.com/download.
- **Attribution requirements**: The [FMOD attribution guidance](https://www.fmod.com/attribution) requires an in-app credit and a readable startup logo before user interaction, unless a logo waiver applies.

## Spine Runtimes

Copyright (c) 2013-2025, Esoteric Software LLC

Integration of the Spine Runtimes into software or otherwise creating derivative works of the Spine Runtimes is permitted under the terms and conditions of Section 2 of the Spine Editor License Agreement:
http://esotericsoftware.com/spine-editor-license#s2

Otherwise, it is permitted to integrate the Spine Runtimes into software or otherwise create derivative works of the Spine Runtimes (collectively, "Products"), provided that each user of the Products must obtain their own Spine Editor license and redistribution of the Products in any form must include this license and copyright notice.

THE SPINE RUNTIMES ARE PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES, BUSINESS INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THE SPINE RUNTIMES, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

Spine runtime binaries are included in APK builds but not in this source repository. Obtain a Spine Editor license at https://esotericsoftware.com.

## AndroidX Libraries

- **License**: Apache 2.0
- **Copyright**: (c) The Android Open Source Project
- **Source**: https://github.com/androidx/androidx
