using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private const string FmodExtension = "res://addons/fmod/fmod.gdextension";
    private const string FmodProjectAutoload =
        "FmodManager=\"*res://addons/fmod/FmodManager.gd\"";
    private const string FmodBinaryAutoload = "autoload/FmodManager";
    private const string DisabledFmodBinaryAutoload = "disabled/FmodManager";
    private const string AudioProxyResource =
        "[ext_resource type=\"Script\" uid=\"uid://c6blhu0io0iwp\" path=\"res://src/gdscript/audio_manager_proxy.gd\" id=\"3_xfu11\"]";
    private const string FmodBankLoader =
        "[node name=\"FmodBankLoader\" type=\"FmodBankLoader\" parent=\".\"]";
    private const string AudioProxyScript = "script = ExtResource(\"3_xfu11\")";
    private const string FmodListener =
        "[node name=\"FmodListener2D\" type=\"FmodListener2D\" parent=\"AudioManager\"]";
    private const string DesktopBanks =
        "bank_paths = [\"res://banks/desktop/Master.strings.bank\", \"res://banks/desktop/Master.bank\", \"res://banks/desktop/sfx.bank\", \"res://banks/desktop/temp_sfx.bank\", \"res://banks/desktop/ambience.bank\"]";
    private const string UserBanks =
        "bank_paths = [\"user://fmod_banks/Master.strings.bank\", \"user://fmod_banks/Master.bank\", \"user://fmod_banks/sfx.bank\", \"user://fmod_banks/temp_sfx.bank\", \"user://fmod_banks/ambience.bank\"]";
    private const string ExternalBanks =
        "bank_paths = [\"/sdcard/sts2b/Master.strings.bank\", \"/sdcard/sts2b/Master.bank\", \"/sdcard/sts2b/sfx.bank\", \"/sdcard/sts2b/temp_sfx.bank\", \"/sdcard/sts2b/ambience.bank\"]";

    private static readonly string[] FmodSceneEntries =
    {
        AudioProxyResource,
        FmodBankLoader,
        DesktopBanks,
        AudioProxyScript,
        FmodListener,
    };

    private static void Issue37Arm64RestoresFmodRuntimePieces()
    {
        AssertFmodArchitecturePolicyIsWired();

        var extensionOutput = PatchPckEntryForCharacterization(
            ".godot/extension_list.cfg",
            new string(' ', FmodExtension.Length)
        );
        Equal(
            FmodExtension,
            extensionOutput,
            "The managed PCK patch must restore the FMOD extension registration."
        );
        Equal(
            FmodExtension,
            PatchPckEntryForCharacterization(
                ".godot/extension_list.cfg",
                extensionOutput,
                expectedChange: false
            ),
            "Repatching an enabled FMOD extension must be idempotent."
        );

        var disabledScene = FmodSceneEntries
            .Select(DisabledSceneEntry)
            .Concat(new[]
            {
                PadToDesktopBankLength(UserBanks),
                PadToDesktopBankLength(ExternalBanks),
            });
        var sceneBytes = Encoding.UTF8.GetBytes(string.Join("\n", disabledScene));

        ApplyPrivateRuleSet(
            sceneBytes,
            "ApplyReplacementPatches",
            "GameSceneSettingRestorations"
        );
        ApplyPrivateRuleSet(
            sceneBytes,
            "ApplyReplacementPatches",
            "Arm64FmodBankPathReplacements"
        );
        var restoredScene = Encoding.UTF8.GetString(sceneBytes);

        Contains(restoredScene, AudioProxyResource, "ARM64 must restore the audio proxy resource.");
        Contains(restoredScene, AudioProxyScript, "ARM64 must restore the audio proxy script binding.");
        Contains(restoredScene, FmodBankLoader, "ARM64 must restore the FMOD bank loader.");
        Contains(restoredScene, DesktopBanks, "ARM64 must restore desktop bank paths.");
        Contains(restoredScene, FmodListener, "ARM64 must restore the FMOD listener.");
        Equal(
            3,
            CountOccurrences(restoredScene, DesktopBanks),
            "ARM64 must restore commented, user-directory, and external-directory bank paths."
        );

        ApplyPrivateRuleSet(
            sceneBytes,
            "ApplyReplacementPatches",
            "GameSceneSettingRestorations",
            expectedChange: false
        );
        ApplyPrivateRuleSet(
            sceneBytes,
            "ApplyReplacementPatches",
            "Arm64FmodBankPathReplacements",
            expectedChange: false
        );
        Equal(
            restoredScene,
            Encoding.UTF8.GetString(sceneBytes),
            "Repatching the restored ARM64 scene must not change it."
        );

        var bankPayload = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        var bankPatch = PatchPckEntryForCharacterization(
            "banks/desktop/Master.bank",
            bankPayload
        );
        True(!bankPatch.Patched, "FMOD bank payload entries must not be patched.");
        True(
            bankPayload.SequenceEqual(bankPatch.Content),
            "FMOD bank payload bytes must remain unchanged."
        );
    }

    private static void Issue37Arm64EnablesAndRepairsFmodManagers()
    {
        AssertFmodArchitecturePolicyIsWired();

        var disabledProjectGodot = Encoding.UTF8.GetBytes(
            DisabledSceneEntry(FmodProjectAutoload)
        );
        ApplyPrivateRuleSet(
            disabledProjectGodot,
            "ApplyReplacementPatches",
            "Arm64FmodProjectGodotRestorations"
        );
        Equal(
            FmodProjectAutoload,
            Encoding.UTF8.GetString(disabledProjectGodot),
            "ARM64 must repair a project.godot FmodManager autoload disabled by an earlier preparer."
        );
        ApplyPrivateRuleSet(
            disabledProjectGodot,
            "ApplyReplacementPatches",
            "Arm64FmodProjectGodotRestorations",
            expectedChange: false
        );

        var disabledProjectBinary = Encoding.UTF8.GetBytes(
            DisabledFmodBinaryAutoload
        );
        ApplyPrivateRuleSet(
            disabledProjectBinary,
            "ApplyReplacementPatches",
            "Arm64FmodProjectBinaryRestorations"
        );
        Equal(
            FmodBinaryAutoload,
            Encoding.UTF8.GetString(disabledProjectBinary),
            "ARM64 must repair disabled/FmodManager in project.binary."
        );
        ApplyPrivateRuleSet(
            disabledProjectBinary,
            "ApplyReplacementPatches",
            "Arm64FmodProjectBinaryRestorations",
            expectedChange: false
        );

        var freshProjectGodot = Encoding.UTF8.GetBytes(FmodProjectAutoload);
        ApplyPrivateRuleSet(
            freshProjectGodot,
            "ApplyReplacementPatches",
            "Arm64FmodProjectGodotRestorations",
            expectedChange: false
        );
        Equal(
            FmodProjectAutoload,
            Encoding.UTF8.GetString(freshProjectGodot),
            "ARM64 must leave a fresh enabled project.godot autoload unchanged."
        );

        var freshProjectBinary = Encoding.UTF8.GetBytes(FmodBinaryAutoload);
        ApplyPrivateRuleSet(
            freshProjectBinary,
            "ApplyReplacementPatches",
            "Arm64FmodProjectBinaryRestorations",
            expectedChange: false
        );
        Equal(
            FmodBinaryAutoload,
            Encoding.UTF8.GetString(freshProjectBinary),
            "ARM64 must leave a fresh enabled project.binary autoload unchanged."
        );
    }

    private static void Issue37X86DisablesFmodSceneUsage()
    {
        AssertFmodArchitecturePolicyIsWired();

        var sceneBytes = Encoding.UTF8.GetBytes(string.Join("\n", FmodSceneEntries));
        ApplyPrivateRuleSet(
            sceneBytes,
            "ApplyProjectSettingComments",
            "GameSceneSettingsToPatch"
        );
        var disabledScene = Encoding.UTF8.GetString(sceneBytes);

        foreach (var entry in FmodSceneEntries)
        {
            Contains(
                disabledScene,
                DisabledSceneEntry(entry),
                $"The x86/non-ARM64 policy must disable this FMOD scene entry: {entry}"
            );
        }

        ApplyPrivateRuleSet(
            sceneBytes,
            "ApplyProjectSettingComments",
            "GameSceneSettingsToPatch",
            expectedChange: false
        );
        Equal(
            disabledScene,
            Encoding.UTF8.GetString(sceneBytes),
            "Repatching an already prepared x86 scene must not change it."
        );

        var projectGodot = Encoding.UTF8.GetBytes(FmodProjectAutoload);
        ApplyPrivateRuleSet(
            projectGodot,
            "ApplyProjectSettingComments",
            "X86FmodProjectGodotSettingsToComment"
        );
        Equal(
            DisabledSceneEntry(FmodProjectAutoload),
            Encoding.UTF8.GetString(projectGodot),
            "x86 must disable the project.godot FMOD manager autoload."
        );

        var projectBinary = Encoding.UTF8.GetBytes(FmodBinaryAutoload);
        ApplyPrivateRuleSet(
            projectBinary,
            "ApplyReplacementPatches",
            "X86FmodProjectBinaryReplacements"
        );
        Equal(
            DisabledFmodBinaryAutoload,
            Encoding.UTF8.GetString(projectBinary),
            "x86 must disable the project.binary FMOD manager autoload."
        );

        ApplyPrivateRuleSet(
            projectGodot,
            "ApplyProjectSettingComments",
            "X86FmodProjectGodotSettingsToComment",
            expectedChange: false
        );
        ApplyPrivateRuleSet(
            projectBinary,
            "ApplyReplacementPatches",
            "X86FmodProjectBinaryReplacements",
            expectedChange: false
        );
    }

    private static void Issue37PckPreparationPrecedesGameIdentity()
    {
        Equal(
            "android-pck-v2",
            DepotDownloader.AndroidPckPreparationVersion,
            "Issue #37 must advance the managed PCK preparation version."
        );

        using var assembly = AssemblyDefinition.ReadAssembly(typeof(DepotDownloader).Assembly.Location);
        var downloader = assembly.MainModule.GetType(typeof(DepotDownloader).FullName);
        var downloadCore = downloader.Methods.Single(method => method.Name == "DownloadCoreAsync");
        var stateMachineAttribute = downloadCore.CustomAttributes.Single(attribute =>
            attribute.AttributeType.FullName == typeof(AsyncStateMachineAttribute).FullName
        );
        var stateMachine = ((TypeReference)stateMachineAttribute.ConstructorArguments[0].Value).Resolve();
        var moveNext = stateMachine.Methods.Single(method => method.Name == "MoveNext");

        var pckPreparationCall = CallIndex(
            moveNext,
            typeof(DepotDownloader).FullName!,
            "PatchGamePck"
        );
        var installCompletionCall = CallIndex(
            moveNext,
            typeof(BranchInstallUpdate).FullName!,
            nameof(BranchInstallUpdate.CompleteInstalledFiles)
        );

        True(pckPreparationCall >= 0, "DownloadCoreAsync must call the managed PCK preparer.");
        True(
            installCompletionCall > pckPreparationCall,
            "DownloadCoreAsync must prepare the managed PCK before completing installed files."
        );

        var branchInstallUpdate = assembly.MainModule.GetType(typeof(BranchInstallUpdate).FullName);
        var completeInstalledFiles = branchInstallUpdate.Methods.Single(method =>
            method.Name == nameof(BranchInstallUpdate.CompleteInstalledFiles)
        );
        var identityCall = CallIndex(
            completeInstalledFiles,
            typeof(GameIdentityReader).FullName!,
            nameof(GameIdentityReader.ReadInstalled)
        );
        var invalidationCall = CallIndex(
            completeInstalledFiles,
            typeof(BranchInstallDerivedArtifacts).FullName!,
            nameof(BranchInstallDerivedArtifacts.InvalidatePreviousIdentity)
        );
        True(
            identityCall >= 0,
            "CompleteInstalledFiles must be the point where GameIdentity is calculated."
        );
        True(
            invalidationCall > identityCall,
            "The existing Issue #38 lifecycle must invalidate derived artifacts after calculating the repaired identity."
        );

        using var fixture = GameInstallFixture.Create();
        BranchInstallCompletion oldCompletion;
        var depots = new[] { new BranchInstallDepot(123, 1001, "selected") };
        using (var oldUpdate = BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            depots
        ))
        {
            oldCompletion = oldUpdate.CompleteInstalledFiles("android-pck-v1");
        }
        True(
            !BranchInstallStateStore.Current.TryReadReady(
                fixture.DataDir,
                fixture.Branch,
                oldCompletion.GameIdentity,
                out _,
                out var staleProblem
            ),
            "An installed PCK prepared by v1 must require managed repair."
        );
        Contains(
            staleProblem,
            DepotDownloader.AndroidPckPreparationVersion,
            "The repair requirement must identify the current PCK preparation version."
        );

        BranchInstallCompletion repairedCompletion;
        using (var repair = BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            depots
        ))
        {
            repairedCompletion = repair.CompleteInstalledFiles(
                DepotDownloader.AndroidPckPreparationVersion
            );
        }
        True(
            BranchInstallStateStore.Current.TryReadReady(
                fixture.DataDir,
                fixture.Branch,
                repairedCompletion.GameIdentity,
                out var repairedState,
                out var repairedProblem
            ),
            $"The repaired v2 installation must become ready. Problem={repairedProblem}"
        );
        Equal(
            DepotDownloader.AndroidPckPreparationVersion,
            repairedState.PckPreparationVersion,
            "The repaired installation must publish the current preparation version."
        );
    }

    private static void Issue37RepairChangesIdentityAndRebuildsRuntimePack()
    {
        using var fixture = CreateRuntimePackFixture();
        var brokenIdentity = PublishRuntimePackFixtureReady(fixture);
        var cache = new TransactionalTestCachePreparer();
        var brokenCandidate = GenerateRuntimePackCandidate(fixture, brokenIdentity);
        True(brokenCandidate.Succeeded, brokenCandidate.Problem);
        var brokenLaunch = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            brokenIdentity,
            brokenCandidate.Candidate,
            cache
        );
        True(brokenLaunch.Succeeded, brokenLaunch.Problem);

        var savePath = Path.Combine(fixture.DataDir, "saves", "issue-37.save");
        var saveBytes = Enumerable.Range(0, 513)
            .Select(index => unchecked((byte)(index * 29)))
            .ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
        File.WriteAllBytes(savePath, saveBytes);

        using var publicInstall = AddSiblingInstall(
            fixture.DataDir,
            "public",
            2001,
            0x31,
            "public-generation-one"
        );
        var publicIdentity = GameIdentityReader.ReadInstalled(
            publicInstall.DataDir,
            publicInstall.Branch
        );
        PublishInitialReady(publicInstall, publicIdentity, 2001);
        publicInstall.WriteMatchingRuntimePack();
        var publicPck = File.ReadAllBytes(publicInstall.PckPath);
        var publicSource = File.ReadAllBytes(publicInstall.SourceAssemblyPath);
        var publicState = File.ReadAllBytes(
            BranchInstallStateStore.PathFor(fixture.DataDir, "public")
        );
        var publicPack = DirectoryFingerprint(
            GameRuntimeSlot.RuntimePackDirectoryPath(fixture.DataDir, "public")
        );

        BranchInstallCompletion repairCompletion;
        using (var repair = BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            LifecycleDepots(1001),
            "preparing-android-pck"
        ))
        {
            repair.RequireUpdatingBeforeInstalledMutation();
            fixture.WritePck(0x37);
            repairCompletion = repair.CompleteInstalledFiles(
                DepotDownloader.AndroidPckPreparationVersion
            );
        }

        var repairedIdentity = repairCompletion.GameIdentity;
        NotEqual(
            brokenIdentity,
            repairedIdentity,
            "Repairing stale PCK bytes must change the authoritative GameIdentity."
        );
        Equal(
            brokenIdentity.InstallGeneration,
            repairedIdentity.InstallGeneration,
            "A PCK-only repair must not need a fabricated depot generation change."
        );
        NotEqual(
            brokenIdentity.PckSha256,
            repairedIdentity.PckSha256,
            "The repaired PCK bytes must produce a new authoritative PCK hash."
        );
        Equal(
            brokenIdentity.SourceAssemblySha256,
            repairedIdentity.SourceAssemblySha256,
            "PCK repair must not alter the source managed assembly."
        );
        True(
            !Directory.Exists(
                GameRuntimeSlot.RuntimePackDirectoryPath(
                    fixture.DataDir,
                    fixture.Branch
                )
            ),
            "Publishing the repaired identity must invalidate the old runtime pack."
        );

        var repairedCandidate = GenerateRuntimePackCandidate(
            fixture,
            repairedIdentity
        );
        True(repairedCandidate.Succeeded, repairedCandidate.Problem);
        var repairedLaunch = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            repairedIdentity,
            repairedCandidate.Candidate,
            cache
        );
        True(repairedLaunch.Succeeded, repairedLaunch.Problem);
        NotEqual(
            brokenLaunch.RuntimeSlot.RuntimePack.PackId,
            repairedLaunch.RuntimeSlot.RuntimePack.PackId,
            "The repaired identity must produce and promote a different runtime pack."
        );
        AssertPromotedManifestContract(
            GameRuntimeSlot.RuntimePackDirectoryPath(
                fixture.DataDir,
                fixture.Branch
            ),
            repairedIdentity
        );

        True(
            saveBytes.SequenceEqual(File.ReadAllBytes(savePath)),
            "PCK repair and runtime-pack rebuild must preserve saves byte-for-byte."
        );
        True(
            publicPck.SequenceEqual(File.ReadAllBytes(publicInstall.PckPath)),
            "Repairing public-beta must preserve public PCK bytes."
        );
        True(
            publicSource.SequenceEqual(File.ReadAllBytes(publicInstall.SourceAssemblyPath)),
            "Repairing public-beta must preserve the public source assembly."
        );
        True(
            publicState.SequenceEqual(
                File.ReadAllBytes(
                    BranchInstallStateStore.PathFor(fixture.DataDir, "public")
                )
            ),
            "Repairing public-beta must preserve public installation state."
        );
        Equal(
            publicPack,
            DirectoryFingerprint(
                GameRuntimeSlot.RuntimePackDirectoryPath(fixture.DataDir, "public")
            ),
            "Repairing public-beta must preserve the public runtime pack."
        );
    }

    private static void AssertFmodArchitecturePolicyIsWired()
    {
        using var assembly = AssemblyDefinition.ReadAssembly(typeof(DepotDownloader).Assembly.Location);
        var downloader = assembly.MainModule.GetType(typeof(DepotDownloader).FullName);
        var policyMethods = AllMethods(downloader).Where(method =>
            method.HasBody
            && method.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference
                && reference.DeclaringType.FullName == "System.Runtime.InteropServices.RuntimeInformation"
                && reference.Name == "get_ProcessArchitecture"
            )
        ).ToArray();
        var referencedFields = policyMethods
            .SelectMany(method => method.Body.Instructions)
            .Select(instruction => instruction.Operand as FieldReference)
            .Where(reference => reference != null)
            .Select(reference => reference!.Name)
            .ToHashSet(StringComparer.Ordinal);

        True(
            referencedFields.Contains("GameSceneSettingRestorations")
                && referencedFields.Contains("Arm64FmodBankPathReplacements"),
            "The ARM64 branch must use the FMOD scene and bank restoration rule sets."
        );
        True(
            referencedFields.Contains("GameSceneSettingsToPatch"),
            "The non-ARM64 branch must use the FMOD scene disabling rule set."
        );
        True(
            referencedFields.Contains("Arm64FmodProjectGodotRestorations")
                && referencedFields.Contains("Arm64FmodProjectBinaryRestorations"),
            "The ARM64 branch must restore both forms of FmodManager."
        );
        True(
            referencedFields.Contains("X86FmodProjectGodotSettingsToComment")
                && referencedFields.Contains("X86FmodProjectBinaryReplacements"),
            "The non-ARM64 branch must disable both forms of FmodManager."
        );
    }

    private static IEnumerable<MethodDefinition> AllMethods(TypeDefinition type)
    {
        foreach (var method in type.Methods)
            yield return method;
        foreach (var nested in type.NestedTypes)
        {
            foreach (var method in AllMethods(nested))
                yield return method;
        }
    }

    private static int CallIndex(
        MethodDefinition method,
        string declaringType,
        string methodName
    )
    {
        for (var index = 0; index < method.Body.Instructions.Count; index++)
        {
            if (method.Body.Instructions[index].Operand is MethodReference reference
                && reference.DeclaringType.FullName == declaringType
                && reference.Name == methodName)
            {
                return index;
            }
        }
        return -1;
    }

    private static string PatchPckEntryForCharacterization(
        string path,
        string input,
        bool expectedChange = true
    )
    {
        var result = PatchPckEntryForCharacterization(
            path,
            Encoding.UTF8.GetBytes(input)
        );
        Equal(
            expectedChange,
            result.Patched,
            $"Unexpected managed PCK entry patch result for {path}."
        );
        return Encoding.UTF8.GetString(result.Content);
    }

    private static (bool Patched, byte[] Content) PatchPckEntryForCharacterization(
        string path,
        byte[] input
    )
    {
        var tempPath = Path.Combine(
            Path.GetTempPath(),
            $"sts2-issue37-pck-{Guid.NewGuid():N}.bin"
        );
        try
        {
            File.WriteAllBytes(tempPath, input);
            using var stream = new FileStream(tempPath, FileMode.Open, FileAccess.ReadWrite);
            var patched = (bool)InvokePrivateStatic(
                "PatchPckEntry",
                path,
                stream,
                0L,
                stream.Length
            )!;
            stream.Position = 0;
            var output = new byte[stream.Length];
            stream.ReadExactly(output);
            return (patched, output);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static void ApplyPrivateRuleSet(
        byte[] content,
        string methodName,
        string fieldName,
        bool expectedChange = true
    )
    {
        var field = typeof(DepotDownloader).GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Static
        ) ?? throw new InvalidOperationException($"Missing PCK rule field {fieldName}.");
        var changed = (bool)InvokePrivateStatic(methodName, content, field.GetValue(null)!)!;
        Equal(
            expectedChange,
            changed,
            $"Unexpected patch result from PCK rule set {fieldName}."
        );
    }

    private static object? InvokePrivateStatic(string methodName, params object[] arguments)
    {
        var method = typeof(DepotDownloader).GetMethods(
                BindingFlags.NonPublic | BindingFlags.Static
            )
            .Single(candidate =>
                candidate.Name == methodName
                && candidate.GetParameters().Length == arguments.Length
            );
        try
        {
            return method.Invoke(null, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static string DisabledSceneEntry(string entry)
        => ";" + entry.Substring(1);

    private static string PadToDesktopBankLength(string bankPaths)
        => bankPaths.PadRight(DesktopBanks.Length);

    private static int CountOccurrences(string value, string expected)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(expected, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += expected.Length;
        }
        return count;
    }
}
