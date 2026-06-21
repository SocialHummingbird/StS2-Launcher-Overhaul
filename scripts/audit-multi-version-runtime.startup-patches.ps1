function Add-MultiVersionRuntimeStartupPatchChecks {
    Add-Check `
        "src\STS2Mobile\Startup\StartupPatchOrchestrator.cs" `
        "orchestrates startup patch groups through the critical failure gate" `
        @(
            "internal static partial class StartupPatchOrchestrator",
            "Apply\(Harmony harmony\)",
            "ForceCriticalPatchFailureRequested\(\)",
            "ApplyGroup\(group, harmony\)",
            "criticalFailed = true",
            "Patch orchestration finished"
        )

    Add-Check `
        "src\STS2Mobile\Startup\StartupPatchOrchestrator.ForcedFailure.cs" `
        "keeps forced critical startup patch failure available for fallback validation" `
        @(
            "ForceCriticalPatchFailureVariable",
            "STS2_FORCE_CRITICAL_PATCH_FAILURE",
            "ForcedCriticalPatchFailureResult",
            "Forced critical patch failure requested for fallback validation",
            "criticalFailed: true"
        )

    Add-Check `
        "src\STS2Mobile\Startup\StartupPatchOrchestrator.Execution.cs" `
        "captures patch helper failures while timing startup patch steps" `
        @(
            "ApplyGroup",
            "ApplyStep",
            "PatchHelper\.LogEmitted \+= CaptureHelperFailure",
            "PatchHelper\.LogEmitted -= CaptureHelperFailure",
            "IsHelperPatchFailure",
            "FormatHelperFailures",
            "Starting patch step",
            "Failed patch step"
        )

    Add-Check `
        "src\STS2Mobile\Startup\StartupPatchOrchestrator.Groups.cs" `
        "keeps startup patch groups ordered by criticality" `
        @(
            "Core\(\)",
            "Gameplay\(\)",
            "Optional\(\)",
            "PlatformPatches\.Apply",
            "ModelDbInitPatch\.Apply",
            "LauncherPatches\.Apply",
            "LanMultiplayerPatcher\.Apply"
        )

    Add-Check `
        "src\STS2Mobile\Patches\LanMultiplayerPatcher.Apply.cs" `
        "orchestrates LAN multiplayer Harmony patch registration" `
        @(
            "internal static void Apply\(Harmony harmony\)",
            "JoinScreenReadyPostfix",
            "OnSubmenuOpenedPrefix",
            "JoinScreenClosedPostfix",
            "PatchHostService\(harmony, hostServiceType, patcherType\)",
            "PatchPlayerNameStrategy\(harmony, sts2Asm, patcherType\)",
            "LAN multiplayer patches applied"
        )

    Add-Check `
        "src\STS2Mobile\Patches\LanMultiplayerPatcher.Apply.HostService.cs" `
        "keeps LAN host-service patches isolated from main Apply orchestration" `
        @(
            "PatchHostService",
            "StartENetHost",
            "Disconnect",
            "StartENetHostPostfix",
            "DisconnectPostfix",
            "BindingFlags\.Public \| BindingFlags\.Instance"
        )

    Add-Check `
        "src\STS2Mobile\Patches\LanMultiplayerPatcher.Apply.PlayerName.cs" `
        "keeps LAN player-name strategy patch isolated from main Apply orchestration" `
        @(
            "PatchPlayerNameStrategy",
            "NullPlatformUtilStrategy",
            "GetPlayerName",
            "GetPlayerNamePrefix",
            "BindingFlags\.Public \| BindingFlags\.Instance"
        )

    Add-Check `
        "src\STS2Mobile\Patches\UiScalePatches.cs" `
        "keeps unsafe dropdown replacement disabled while preserving window-scale runtime patches" `
        @(
            "UI scale dropdown replacement disabled",
            "cannot safely Harmony-wrap private-field-heavy resolution dropdown methods",
            "PatchWindowChangeHandlers\(harmony, sts2Asm\)",
            "UiScaleChanged",
            "UiScalePercent"
        )

    Add-Check `
        "src\STS2Mobile\Patches\UiScalePatches.Dropdown.cs" `
        "keeps dormant UI scale dropdown Harmony registration isolated" `
        @(
            "PatchResolutionDropdown",
            "PatchResolutionDropdownItem",
            "PatchSettingsScreen",
            "RefreshEnabled",
            "PopulateDropdownItems",
            "RefreshCurrentlySelectedResolution",
            "OnDropdownItemSelected",
            "LocalizeLabels",
            "PatchHelper\.Method"
        )

    Add-Check `
        "src\STS2Mobile\Patches\UiScalePatches.Dropdown.Selection.cs" `
        "keeps dropdown scale selection prefixes isolated" `
        @(
            "RefreshEnabledPrefix",
            "PopulateScaleItemsPrefix",
            "RefreshScaleLabelPrefix",
            "ScaleItemSelectedPrefix",
            "ClearDropdownItems",
            "PackedScene\.GenEditState\.Disabled",
            "UiScalePercent = newScale",
            "SaveUiScale\(\)",
            "ApplyUiScale\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Patches\UiScalePatches.Dropdown.ItemInit.cs" `
        "keeps dropdown item initialization prefix isolated" `
        @(
            "ResolutionItemInitPrefix",
            "setResolution\.Y != 0",
            "resolution",
            "SetTextAutoSize",
            "ResolutionItemInitPrefix failed"
        )

    Add-Check `
        "src\STS2Mobile\Patches\UiScalePatches.Dropdown.SettingsLabel.cs" `
        "keeps settings label rename postfix isolated" `
        @(
            "LocalizeLabelsPostfix",
            "%GraphicsSettings",
            "WindowedResolution",
            "UI Scale",
            "LocalizeLabels postfix failed"
        )

    Add-Check `
        "src\STS2Mobile\Patches\PlatformPatches.PlatformUtil.cs" `
        "orchestrates Android PlatformUtil patch registration" `
        @(
            "ApplyPlatformUtilPatches",
            "PatchPlatformUtilStaticConstructor\(harmony\)",
            "PatchPlatformUtilPrimaryPlatformGetter\(harmony\)",
            "PatchGetPlatformUtil\(harmony\)",
            "PatchPlatformUtilMethods\(harmony\)",
            "PlatformUtil patch failed"
        )

    Add-Check `
        "src\STS2Mobile\Patches\PlatformPatches.PlatformUtil.Registration.cs" `
        "registers PlatformUtil Harmony prefixes and guards incompatible branch signatures" `
        @(
            "PatchPlatformUtilStaticConstructor",
            "PatchPlatformUtilPrimaryPlatformGetter",
            "PatchGetPlatformUtil",
            "PatchPlatformUtilMethods",
            "PatchPlatformUtilMethod",
            "Prefix\(string prefixName\)",
            "GetPlatformBranch",
            "method\.ReturnType != typeof\(string\)",
            "new HarmonyMethod\(prefix\)",
            "Patched PlatformUtil"
        )

    Add-Check `
        "src\STS2Mobile\Patches\PlatformPatches.PlatformUtil.LocaleStrategy.cs" `
        "patches NullPlatformUtilStrategy language lookup without desktop initialization" `
        @(
            "ApplyNullPlatformLanguagePatch",
            "FindGetThreeLetterLanguageCode",
            "NullPlatformUtilStrategy",
            "GetThreeLetterLanguageCode",
            "BindingFlags\.Public \| BindingFlags\.Instance",
            "Patched NullPlatformUtilStrategy\.GetThreeLetterLanguageCode",
            "Locale fix failed"
        )

    Add-Check `
        "src\STS2Mobile\Patches\PlatformPatches.PlatformUtil.Runtime.cs" `
        "replaces desktop PlatformUtil runtime state with Android-safe null strategy prefixes" `
        @(
            "PlatformUtilStaticConstructorPrefix",
            "PrimaryPlatformPrefix",
            "GetPlatformUtilPrefix",
            "GetAndroidNullPlatformStrategy",
            "NullPlatformField",
            "PlatformType\.None",
            "new NullPlatformUtilStrategy\(\)",
            "Skipped PlatformUtil desktop static initialization on Android",
            "NullPlatformUtilStrategy unavailable on Android"
        )

    Add-Check `
        "src\STS2Mobile\Patches\ModelDbInitPatch.cs" `
        "orchestrates patched two-phase ModelDb initialization" `
        @(
            "internal static partial class ModelDbInitPatch",
            "typeof\(ModelDb\)",
            "InitPrefix",
            "TryLoadModelDbInitAccess",
            "RuntimeHelpers\.GetUninitializedObject",
            "new Harmony\(HarmonyId\)",
            "RunConstructors\(types, typeObjects\)",
            "harmony\.Unpatch\(containsMethod, containsPrefix\)",
            "LogPhase2Result\(types\.Length, phase2\.SuccessCount, phase2\.Failed\)"
        )

    Add-Check `
        "src\STS2Mobile\Patches\ModelDbInitPatch.Access.cs" `
        "keeps ModelDb reflection access and fallback checks isolated" `
        @(
            "TryLoadModelDbInitAccess",
            "AllAbstractModelSubtypesProperty",
            "GetIdMethodName",
            "ContentByIdField",
            "SetItemMethod",
            "ContainsMethodName",
            "ModelDb\.Init fallback: AllAbstractModelSubtypes property missing",
            "ModelDb\.Init fallback: _contentById is null",
            "ModelDb\.Init fallback: Contains\(Type\) method missing"
        )

    Add-Check `
        "src\STS2Mobile\Patches\ModelDbInitPatch.Constructors.cs" `
        "runs ModelDb constructors while guaranteeing Contains suppression cleanup" `
        @(
            "ConstructorRunResult",
            "RunConstructors",
            "_suppressContains = true",
            "try",
            "RuntimeHelpers\.RunClassConstructor",
            "ctor\.Invoke",
            "finally",
            "_suppressContains = false",
            "LogPhase2Result",
            "WARNING: \{failed\.Count\}/\{totalCount\} types had constructor errors",
            "All \{successCount\} model types registered successfully"
        )

    Add-Check `
        "src\STS2Mobile\Patches\ModelDbInitPatch.Contains.cs" `
        "keeps temporary ModelDb Contains suppression isolated" `
        @(
            "internal static partial class ModelDbInitPatch",
            "_suppressContains = false",
            "ContainsPrefix",
            "__result = false",
            "return false",
            "return true"
        )

    Add-Check `
        "src\STS2Mobile\Patches\SettingsPatches.cs" `
        "wires mobile settings compatibility patches into startup settings initialization" `
        @(
            "internal static partial class SettingsPatches",
            "InitSettingsDataMethod",
            "PatchGetter",
            "SkipIntroLogoProperty",
            "PatchVSyncString\(harmony\)",
            "PatchLocStringGetFormattedText\(harmony\)"
        )

    Add-Check `
        "src\STS2Mobile\Patches\SettingsPatches.MobileDefaults.cs" `
        "applies first-run mobile defaults without repeatedly overwriting user settings" `
        @(
            "InitSettingsDataPostfix",
            "_mobileDefaultsChecked",
            "ApplyMobileDefaultsIfNeeded",
            "SaveManager\.Instance\.SettingsSave",
            "VSyncType\.On",
            "AspectRatioSetting\.Auto",
            "settings\.SkipIntroLogo = true",
            "MobileDefaultsMarkerPath",
            "SkipIntroLogoPrefix",
            "OperatingSystem\.IsAndroid\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Patches\SettingsPatches.VSync.cs" `
        "keeps VSync setting copy mapped to the corrected localization keys" `
        @(
            "PatchVSyncString",
            "GetVSyncStringPrefix",
            "GetVSyncText",
            "VSyncKeyFor",
            "VSyncOffKey",
            "VSyncOnKey",
            "VSyncAdaptiveKey"
        )

    Add-Check `
        "src\STS2Mobile\Patches\SettingsPatches.LocString.cs" `
        "keeps Android LocString null fallback isolated and logged once" `
        @(
            "PatchLocStringGetFormattedText",
            "GetFormattedTextFinalizer",
            "NullReferenceException",
            "OperatingSystem\.IsAndroid\(\)",
            "GetLocStringFallback",
            "ReadStringMember",
            "Interlocked\.Exchange",
            "LocString\.GetFormattedText null fallback active"
        )

    Add-Check `
        "src\STS2Mobile\Patches\LanMultiplayerPatcher.Discovery.cs" `
        "starts LAN discovery with UDP broadcast socket and Godot poll timer" `
        @(
            "private sealed partial class LanDiscovery",
            "SocketOptionName\.ReuseAddress",
            "SocketOptionName\.Broadcast",
            "Bind\(new IPEndPoint\(IPAddress\.Any, BeaconPort\)\)",
            "new Thread\(ListenLoop\)",
            "Name = ""LanDiscovery""",
            "Connect\(""timeout"", Callable\.From\(PollHosts\)\)",
            "LAN discovery started"
        )

    Add-Check `
        "src\STS2Mobile\Patches\LanMultiplayerPatcher.Discovery.Network.cs" `
        "listens for LAN beacon packets while ignoring local IPv4 addresses" `
        @(
            "ListenLoop",
            "Encoding\.UTF8\.GetString",
            "BeaconPrefix",
            "_localIps\.Contains\(ip\)",
            "NetworkInterface\.GetAllNetworkInterfaces",
            "AddressFamily\.InterNetwork",
            "LAN local IP enumeration failed"
        )

    Add-Check `
        "src\STS2Mobile\Patches\LanMultiplayerPatcher.Discovery.Poll.cs" `
        "polls discovered LAN hosts and keeps join-screen visibility state current" `
        @(
            "PollHosts",
            "TotalSeconds > 6\.0",
            "AddHostButton",
            "_loadingIndicatorField",
            "_noFriendsLabelField",
            "LAN discovery visibility update failed",
            "UpdateScreenContext"
        )

    Add-Check `
        "src\STS2Mobile\Patches\LanMultiplayerPatcher.Discovery.HostButton.cs" `
        "creates LAN join buttons that save the target IP and connect through JoinViaIp" `
        @(
            "AddHostButton",
            "_joinFriendButtonCreate\.Invoke",
            "SaveLastIp\(capturedIp\)",
            "JoinViaIp\(_screen, capturedIp, capturedPort\)",
            "Discovered LAN host"
        )

    Add-Check `
        "src\STS2Mobile\Patches\LanMultiplayerPatcher.Discovery.Stop.cs" `
        "stops LAN discovery sockets, timers, buttons, and screen state" `
        @(
            "internal void Stop\(\)",
            "_udpClient\?\.Close\(\)",
            "_listenThread\?\.Join\(500\)",
            "_pollTimer\.QueueFree\(\)",
            "_hostButtons\.Clear\(\)",
            "_cleanupDiscoveryState",
            "LAN discovery stopped"
        )

    Add-Check `
        "src\STS2Mobile\Startup\StartupPatchOrchestrator.Results.cs" `
        "keeps startup patch result shape used by runtime patch validation evidence" `
        @(
            "StartupPatchResult",
            "PatchGroupResult",
            "PatchAttempt",
            "CriticalFailed",
            "FailureMessages",
            "HasFailures",
            "FailedPatchCount"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherRuntimePatchValidationEvidence.cs" `
        "tracks runtime patch validation marker lifecycle" `
        @(
            "last_runtime_patch_validation\.json",
            "MarkerPath",
            "MarkerPresent",
            "Clear",
            "Failed to clear runtime patch validation evidence"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherRuntimePatchValidationEvidence.Write.cs" `
        "records actual runtime Harmony patch results for the selected runtime slot" `
        @(
            "StartupPatchOrchestrator\.StartupPatchResult",
            "critical_failed",
            "passed_with_noncritical_failures",
            "SteamGameBranch\.Normalize",
            "SteamGameInstallPaths\.VersionSlotKind",
            "GameRuntimeSlot\.Inspect",
            "LauncherRuntimeCacheEvidence\.RuntimeIdPrefix",
            "LauncherRuntimeCacheEvidence\.SelectedPckSha256Prefix",
            "LauncherRuntimeCacheEvidence\.SelectedSourceAssemblySha256Prefix",
            "LauncherRuntimeCacheEvidence\.ActiveSourceAssemblySha256Prefix",
            "LauncherRuntimeCacheEvidence\.RuntimePackDirectoryPrefix",
            "LauncherRuntimeCacheEvidence\.RuntimePackGameAssemblyPrefix",
            "selectedPckSha256",
            "selectedSourceAssemblySha256",
            "activeAndroidAssemblySha256",
            "runtimePackId",
            "runtimeSlotId",
            "runtimeCacheId",
            "failureMessages",
            "Failed to write runtime patch validation evidence"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherRuntimePatchValidationEvidence.RuntimeCache.cs" `
        "reads selected runtime cache values and classifies runtime pack status" `
        @(
            "RuntimeCacheValue",
            "LauncherRuntimeCacheEvidence\.MarkerPath",
            "File\.ReadLines",
            "StringComparison\.OrdinalIgnoreCase",
            "RuntimePackStatus",
            "missing runtime pack assembly",
            "runtime pack selected"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherRuntimePatchValidationEvidence.Read.cs" `
        "reads runtime patch validation marker fields for diagnostics reports" `
        @(
            "UtcParseable",
            "SelectedBranch",
            "SelectedPckSha256",
            "SelectedSourceAssemblySha256",
            "ActiveAndroidAssemblySha256",
            "RuntimePackId",
            "RuntimePackStatus",
            "FailureMessages",
            "ReadString",
            "JsonValueKind\.Number"
        )

    Add-Check `
        "src\STS2Mobile\ModEntry.cs" `
        "keeps the mobile patcher entrypoint split into focused partials" `
        @(
            "public static partial class ModEntry",
            "Bootstraps GodotSharp",
            "falls back to standalone launcher mode"
        )

    Add-Check `
        "src\STS2Mobile\ModEntry.Constants.cs" `
        "centralizes bootstrap probes, startup fallback state, and runtime path constants" `
        @(
            "ApplyComplete = 2",
            "ApplyInProgress = 1",
            "ApplyNotStarted = 0",
            "BootstrapProbeCode = 1729",
            "HarmonyConstructorProbeCode = 1730",
            "LauncherBootstrapVariable = `"STS2_LAUNCHER_BOOTSTRAP`"",
            "MinimumPckHeaderLength = 96",
            "TempVariableNames",
            "HasStartupFallbackReason"
        )

    Add-Check `
        "src\STS2Mobile\ModEntry.Exports.cs" `
        "keeps unmanaged bootstrap exports isolated from startup orchestration" `
        @(
            "InitializeGodotSharp",
            "ApplyFromGodot",
            "BootstrapProbe",
            "HarmonyConstructorProbe",
            "ShowLauncherOnly",
            "UnmanagedCallersOnly",
            "GodotSharpBootstrap\.Initialize",
            "new Harmony\(HarmonyId\)"
        )

    Add-Check `
        "src\STS2Mobile\ModEntry.Apply.cs" `
        "writes runtime patch validation evidence immediately after startup patch orchestration" `
        @(
            "ApplyInternal",
            "InstallManagedExceptionHandlers",
            "TryBeginApply",
            "ApplyStartupPatches",
            "StartupPatchOrchestrator\.Apply\(harmony\)",
            "LauncherRuntimePatchValidationEvidence\.Write\(OS\.GetDataDir\(\), patchResult\)",
            "Critical startup patches failed",
            "ScheduleStandaloneLauncher",
            "CompleteApply"
        )
}
