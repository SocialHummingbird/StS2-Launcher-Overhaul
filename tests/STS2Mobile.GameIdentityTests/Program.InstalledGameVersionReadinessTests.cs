using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void InstalledVersionReadinessAcceptsCurrentPreparation()
    {
        using var fixture = CreateReadinessFixture(
            "android-pck-v2",
            ManagedReadinessEntries()
        );

        Equal(
            InstalledGameVersionReadiness.Ready,
            LauncherGameFiles.ClassifyInstalledVersion(fixture.DataDir, fixture.Branch),
            "A current, identity-bound, provenance-matched installation must be ready."
        );
    }

    private static void InstalledVersionReadinessPermitsSafeAutomaticRepair()
    {
        using var fixture = CreateReadinessFixture(
            "android-pck-v1",
            ManagedReadinessEntries(mixedRecognizedForms: true)
        );

        Equal(
            InstalledGameVersionReadiness.AutomaticRepair,
            LauncherGameFiles.ClassifyInstalledVersion(fixture.DataDir, fixture.Branch),
            "Exact v1 evidence with only recognized v1/v2 FMOD forms must permit automatic repair."
        );
    }

    private static void InstalledVersionReadinessRejectsUnknownPreparation()
    {
        foreach (var version in new[] { "android-pck-v3", "ANDROID-PCK-V1" })
        {
            using var fixture = CreateReadinessFixture(
                version,
                ManagedReadinessEntries()
            );

            Equal(
                InstalledGameVersionReadiness.RedownloadRequired,
                LauncherGameFiles.ClassifyInstalledVersion(fixture.DataDir, fixture.Branch),
                $"Unknown or non-exact preparation version '{version}' must require redownload."
            );
        }
    }

    private static void InstalledVersionReadinessRejectsMissingFiles()
    {
        using var fixture = CreateReadinessFixture(
            "android-pck-v1",
            ManagedReadinessEntries()
        );
        File.Delete(fixture.SourceAssemblyPath);

        Equal(
            InstalledGameVersionReadiness.RedownloadRequired,
            LauncherGameFiles.ClassifyInstalledVersion(fixture.DataDir, fixture.Branch),
            "A missing identity file must require redownload."
        );
    }

    private static void InstalledVersionReadinessRejectsCorruptState()
    {
        using var fixture = CreateReadinessFixture(
            "android-pck-v1",
            ManagedReadinessEntries()
        );
        File.WriteAllText(
            BranchInstallStateStore.PathFor(fixture.DataDir, fixture.Branch),
            "{",
            Encoding.UTF8
        );

        Equal(
            InstalledGameVersionReadiness.RedownloadRequired,
            LauncherGameFiles.ClassifyInstalledVersion(fixture.DataDir, fixture.Branch),
            "Corrupt installation state must require redownload."
        );
    }

    private static void InstalledVersionReadinessRejectsChangedIdentity()
    {
        using var fixture = CreateReadinessFixture(
            "android-pck-v1",
            ManagedReadinessEntries()
        );
        fixture.WriteSourceAssembly("changed-after-ready-publication");

        Equal(
            InstalledGameVersionReadiness.RedownloadRequired,
            LauncherGameFiles.ClassifyInstalledVersion(fixture.DataDir, fixture.Branch),
            "Installed files that no longer match the stored GameIdentity must require redownload."
        );
    }

    private static void InstalledVersionReadinessRejectsProvenanceMismatch()
    {
        using var fixture = CreateReadinessFixture(
            "android-pck-v1",
            ManagedReadinessEntries()
        );
        var statePath = BranchInstallStateStore.PathFor(fixture.DataDir, fixture.Branch);
        var state = File.ReadAllText(statePath, Encoding.UTF8);
        state = state.Replace(
            "\"manifestId\": 1001",
            "\"manifestId\": 9001",
            StringComparison.Ordinal
        );
        File.WriteAllText(statePath, state, Encoding.UTF8);

        Equal(
            InstalledGameVersionReadiness.RedownloadRequired,
            LauncherGameFiles.ClassifyInstalledVersion(fixture.DataDir, fixture.Branch),
            "Installation-state depots that disagree with the Steam marker must require redownload."
        );
    }

    private static void InstalledVersionReadinessRejectsBranchMismatch()
    {
        using var fixture = CreateReadinessFixture(
            "android-pck-v1",
            ManagedReadinessEntries()
        );
        var statePath = BranchInstallStateStore.PathFor(fixture.DataDir, fixture.Branch);
        var state = File.ReadAllText(statePath, Encoding.UTF8).Replace(
            $"\"branch\": \"{fixture.Branch}\"",
            "\"branch\": \"public\"",
            StringComparison.Ordinal
        );
        File.WriteAllText(statePath, state, Encoding.UTF8);

        Equal(
            InstalledGameVersionReadiness.RedownloadRequired,
            LauncherGameFiles.ClassifyInstalledVersion(fixture.DataDir, fixture.Branch),
            "Installation state for another branch must require redownload."
        );
    }

    private static void InstalledVersionReadinessRejectsCorruptPck()
    {
        using var fixture = CreateReadinessFixture(
            "android-pck-v1",
            ManagedReadinessEntries()
        );
        var pck = File.ReadAllBytes(fixture.PckPath);
        pck[0] = 0;
        File.WriteAllBytes(fixture.PckPath, pck);
        File.SetLastWriteTimeUtc(fixture.PckPath, DateTime.UtcNow.AddMinutes(-2));

        Equal(
            InstalledGameVersionReadiness.RedownloadRequired,
            LauncherGameFiles.ClassifyInstalledVersion(fixture.DataDir, fixture.Branch),
            "A structurally invalid PCK must require redownload."
        );
    }

    private static void InstalledVersionReadinessRejectsUnrecognizedFmodForm()
    {
        var entries = ManagedReadinessEntries().ToArray();
        entries[1] = (
            ManagedFmodPckForms.ProjectGodotPath,
            "FmodManager=\"*res://addons/fmod/unknown.gd\""
        );
        using var fixture = CreateReadinessFixture("android-pck-v1", entries);

        Equal(
            InstalledGameVersionReadiness.RedownloadRequired,
            LauncherGameFiles.ClassifyInstalledVersion(fixture.DataDir, fixture.Branch),
            "An unrecognized managed FMOD form must require redownload."
        );
    }

    private static void InstalledVersionReadinessRejectsMissingManagedEntry()
    {
        var entries = ManagedReadinessEntries()
            .Where(entry => entry.Path != ManagedFmodPckForms.ExtensionListPath)
            .ToArray();
        using var fixture = CreateReadinessFixture("android-pck-v1", entries);

        Equal(
            InstalledGameVersionReadiness.RedownloadRequired,
            LauncherGameFiles.ClassifyInstalledVersion(fixture.DataDir, fixture.Branch),
            "A missing managed PCK entry must require redownload."
        );
    }

    private static void InstalledVersionReadinessInspectionIsReadOnly()
    {
        using var fixture = CreateReadinessFixture(
            "android-pck-v1",
            ManagedReadinessEntries()
        );
        var statePath = BranchInstallStateStore.PathFor(fixture.DataDir, fixture.Branch);
        var cachePath = GameIdentityPckCache.PathFor(fixture.DataDir, fixture.Branch);
        if (File.Exists(cachePath))
            File.Delete(cachePath);

        var paths = new[]
        {
            fixture.PckPath,
            fixture.SourceAssemblyPath,
            fixture.BranchMarkerPath,
            statePath,
        };
        var bytesBefore = paths.ToDictionary(path => path, File.ReadAllBytes);
        var timestampsBefore = paths.ToDictionary(path => path, File.GetLastWriteTimeUtc);

        Equal(
            InstalledGameVersionReadiness.AutomaticRepair,
            LauncherGameFiles.ClassifyInstalledVersion(fixture.DataDir, fixture.Branch),
            "The read-only inspection fixture should remain safely repairable."
        );
        True(!File.Exists(cachePath), "Classification must not create a GameIdentity PCK cache.");
        foreach (var path in paths)
        {
            True(
                bytesBefore[path].SequenceEqual(File.ReadAllBytes(path)),
                $"Classification must not change {path}."
            );
            Equal(
                timestampsBefore[path],
                File.GetLastWriteTimeUtc(path),
                $"Classification must not touch the timestamp for {path}."
            );
        }
    }

    private static GameInstallFixture CreateReadinessFixture(
        string preparationVersion,
        IReadOnlyList<(string Path, string Content)> entries
    )
    {
        var fixture = GameInstallFixture.Create();
        try
        {
            WriteReadinessPck(fixture.PckPath, entries);
            WriteReadinessBranchMarker(fixture, 1001);
            var identity = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
            var depots = new[] { new BranchInstallDepot(123, 1001, "selected") };
            var transactionId = Guid.NewGuid();
            BranchInstallStateStore.Current.BeginUpdating(
                fixture.DataDir,
                fixture.Branch,
                transactionId,
                "readiness-test",
                depots
            );
            BranchInstallStateStore.Current.CommitReady(
                fixture.DataDir,
                fixture.Branch,
                transactionId,
                identity,
                depots,
                preparationVersion
            );
            return fixture;
        }
        catch
        {
            fixture.Dispose();
            throw;
        }
    }

    private static IReadOnlyList<(string Path, string Content)> ManagedReadinessEntries(
        bool mixedRecognizedForms = false
    )
    {
        var sceneEntries = ManagedFmodPckForms.GameSceneEntries
            .Select((entry, index) => mixedRecognizedForms && index % 2 == 0
                ? ManagedFmodPckForms.DisabledTextEntry(entry)
                : entry);
        return new[]
        {
            (
                ManagedFmodPckForms.ProjectBinaryPath,
                mixedRecognizedForms
                    ? ManagedFmodPckForms.DisabledProjectBinaryAutoload
                    : ManagedFmodPckForms.ProjectBinaryAutoload
            ),
            (
                ManagedFmodPckForms.ProjectGodotPath,
                ManagedFmodPckForms.ProjectGodotSetting
            ),
            (
                ManagedFmodPckForms.ExtensionListPath,
                ManagedFmodPckForms.ExtensionListEntry
            ),
            (
                ManagedFmodPckForms.GameScenePath,
                string.Join("\n", sceneEntries)
            ),
        };
    }

    private static void WriteReadinessBranchMarker(
        GameInstallFixture fixture,
        ulong manifestId
    )
    {
        File.WriteAllText(
            fixture.BranchMarkerPath,
            $"Branch: {fixture.Branch}\n"
                + $"Install slot kind: {SteamGameInstallPaths.VersionSlotKind(fixture.Branch)}\n"
                + $"Install slot directory: {SteamGameInstallPaths.VersionSlotDirectory(fixture.DataDir, fixture.Branch)}\n"
                + "Depot manifest count: 1\n"
                + $"Depot manifest: depot=123 manifest={manifestId} branch={fixture.Branch} manifestSource=selected\n",
            Encoding.UTF8
        );
        File.SetLastWriteTimeUtc(fixture.BranchMarkerPath, DateTime.UtcNow);
    }

    private static void WriteReadinessPck(
        string path,
        IReadOnlyList<(string Path, string Content)> entries
    )
    {
        var encoded = entries
            .Select(entry => (
                Path: Encoding.UTF8.GetBytes(entry.Path),
                Content: Encoding.UTF8.GetBytes(entry.Content)
            ))
            .ToArray();
        var directoryBytes = sizeof(uint) + encoded.Sum(entry =>
            sizeof(uint) + entry.Path.Length + sizeof(long) + sizeof(long) + 16 + sizeof(uint)
        );
        var nextContentOffset = 104 + directoryBytes;

        using var memory = new MemoryStream();
        using var writer = new BinaryWriter(memory, Encoding.UTF8, leaveOpen: true);
        writer.Write(0x43504447u);
        writer.Write(3u);
        writer.Write(4u);
        writer.Write(5u);
        writer.Write(0u);
        writer.Write(0u); // absolute offsets
        writer.Write(0L);
        writer.Write(104L);
        for (var index = 0; index < 16; index++)
            writer.Write(0u);

        writer.Write((uint)encoded.Length);
        foreach (var entry in encoded)
        {
            writer.Write((uint)entry.Path.Length);
            writer.Write(entry.Path);
            writer.Write((long)nextContentOffset);
            writer.Write((long)entry.Content.Length);
            writer.Write(new byte[16]);
            writer.Write(0u);
            nextContentOffset += entry.Content.Length;
        }
        foreach (var entry in encoded)
            writer.Write(entry.Content);

        File.WriteAllBytes(path, memory.ToArray());
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-2));
    }
}
