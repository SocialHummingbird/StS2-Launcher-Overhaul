using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void DiagnosticCacheSnapshotSurvivesReplacement()
    {
        using var fixture = GameInstallFixture.Create();
        var path = LauncherRuntimeCacheEvidence.MarkerPath(fixture.DataDir);
        File.WriteAllText(path, "Package: original\nActive branch: public\n");
        var captured = LauncherRuntimeCacheEvidence.ReadSnapshot(fixture.DataDir);
        Equal("original", captured.Package, "The report begins with the original marker.");
        File.WriteAllText(path, "Package: replacement\nActive branch: beta\n");
        Equal("public", captured.ActiveBranch, "One report cannot mix fields from two marker generations.");
        Equal("replacement", LauncherRuntimeCacheEvidence.ReadSnapshot(fixture.DataDir).Package, "The next report must read fresh evidence.");
    }

    private static void DiagnosticJsonSnapshotSurvivesReplacement()
    {
        using var fixture = GameInstallFixture.Create();
        var path = LauncherRuntimePatchValidationEvidence.MarkerPath(fixture.DataDir);
        File.WriteAllText(path, "{\"status\":\"passed\",\"selectedBranch\":\"public\"}");
        var captured = LauncherRuntimePatchValidationEvidence.ReadSnapshot(fixture.DataDir);
        Equal("passed", captured.Status, "The report begins with the original JSON marker.");
        File.WriteAllText(path, "{\"status\":\"failed\",\"selectedBranch\":\"beta\"}");
        Equal("public", captured.SelectedBranch, "JSON fields in one report must come from the same read.");
        Equal("failed", LauncherRuntimePatchValidationEvidence.ReadSnapshot(fixture.DataDir).Status, "Subsequent reports must observe replacement JSON.");
    }

    private static void DiagnosticSnapshotPreservesFormatting()
    {
        using var fixture = GameInstallFixture.Create();
        var path = LauncherRuntimePatchValidationEvidence.MarkerPath(fixture.DataDir);
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            status = false, utc = "2026-10-02T12:00:00Z", selectedVersion = (string?)null,
            totalPatchCount = 123, runtimePackId = new { unsupported = true },
            failureMessages = new object[] { "", "  ", 42 }.Concat(Enumerable.Range(0, 12).Select(i => (object)$"failure-{i}")).ToArray()
        }));
        var captured = LauncherRuntimePatchValidationEvidence.ReadSnapshot(fixture.DataDir);
        Equal("false", captured.Status, "Booleans keep lowercase diagnostic formatting.");
        True(captured.UtcParseable, "The existing UTC parsing policy must be preserved.");
        Equal("123", captured.TotalPatchCount, "Numeric fields keep their JSON representation.");
        Equal("<unsupported>", captured.SelectedVersion, "Null JSON values remain unsupported.");
        Equal("<unsupported>", captured.RuntimePackId, "Object values remain unsupported.");
        Equal("<missing>", captured.SelectedBranch, "Missing properties retain their sentinel.");
        Equal(string.Join(" | ", Enumerable.Range(0, 10).Select(i => $"failure-{i}")), captured.FailureMessages, "Only the first ten nonempty string failures are reported.");
        File.WriteAllText(path, "[");
        var malformed = LauncherRuntimePatchValidationEvidence.ReadSnapshot(fixture.DataDir);
        True(malformed.Present, "A malformed existing marker must still be reported present.");
        Equal("<read failed>", malformed.Status, "Malformed JSON remains diagnostic read failure.");
        File.Delete(path);
        Equal("<none>", LauncherRuntimePatchValidationEvidence.ReadSnapshot(fixture.DataDir).Status, "Absent JSON retains its sentinel.");
        True(captured.UtcParseable, "Captured values remain usable after the file is deleted.");
        var cachePath = LauncherRuntimeCacheEvidence.MarkerPath(fixture.DataDir);
        File.WriteAllText(cachePath, "package: first  \nPackage: second\n");
        var cache = LauncherRuntimeCacheEvidence.ReadSnapshot(fixture.DataDir);
        Equal("first", cache.Package, "Text markers keep first-match, case-insensitive and trimmed-value behavior.");
        Equal("<missing>", cache.ActiveBranch, "Missing text fields retain their sentinel.");
        File.Delete(cachePath);
        True(!LauncherRuntimeCacheEvidence.ReadSnapshot(fixture.DataDir).Present, "Presence is captured with the report.");
        Equal("<none>", LauncherRuntimeCacheEvidence.ReadSnapshot(fixture.DataDir).Package, "Missing text markers retain their sentinel.");
    }

    private static void DiagnosticSnapshotCannotAuthorizeStaleLaunch()
    {
        using var fixture = GameInstallFixture.Create();
        var identity = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        var path = LauncherRuntimeSlotEvidence.MarkerPath(fixture.DataDir);
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            schemaVersion = 1, branch = identity.Branch, gameIdentityId = identity.Id,
            runtimePackId = "pack", installGeneration = identity.InstallGeneration,
            pckSha256 = identity.PckSha256, sourceAssemblySha256 = identity.SourceAssemblySha256,
            filesReady = true, playable = true, runtimeCompatible = true, patchCompatible = true
        }));
        var capture = LauncherRuntimeSlotEvidence.ReadSnapshot(fixture.DataDir);
        True(LauncherRuntimeSlotEvidence.IsAuthorized(fixture.DataDir, identity, "pack", out var problem), problem);
        File.WriteAllText(path, "{\"gameIdentityId\":\"replaced\"}");
        Equal(identity.Id, capture.GameIdentityId, "Diagnostics preserve their original report snapshot.");
        True(!LauncherRuntimeSlotEvidence.IsAuthorized(fixture.DataDir, identity, "pack", out _), "Launch authorization must reread the current marker, independently of diagnostics.");
    }
}
