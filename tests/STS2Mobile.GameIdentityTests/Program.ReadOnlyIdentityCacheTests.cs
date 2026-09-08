using System;
using System.IO;
using System.Linq;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void ReadOnlyIdentityReusesCurrentPckCache()
    {
        using var fixture = GameInstallFixture.Create();
        var expected = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        var cachePath = GameIdentityPckCache.PathFor(fixture.DataDir, fixture.Branch);
        var cacheBytes = File.ReadAllBytes(cachePath);
        var cacheTimestamp = File.GetLastWriteTimeUtc(cachePath);
        var hasher = new CountingHasher();

        var identity = new GameIdentityReader(hasher).ReadReadOnly(fixture.DataDir, fixture.Branch);

        Equal(expected, identity, "Read-only inspection must resolve the same complete identity.");
        Equal(0, hasher.CountFor(fixture.PckPath), "Launcher routing must reuse a valid PCK digest without rereading the entire game archive.");
        Equal(1, hasher.CountFor(fixture.SourceAssemblyPath), "Read-only inspection must still directly hash the source DLL.");
        True(cacheBytes.SequenceEqual(File.ReadAllBytes(cachePath)), "A read-only cache hit must not rewrite cache contents.");
        Equal(cacheTimestamp, File.GetLastWriteTimeUtc(cachePath), "A read-only cache hit must not touch its timestamp.");
    }

    private static void ReadOnlyIdentityCacheMissDoesNotWrite()
    {
        using var fixture = GameInstallFixture.Create();
        var expected = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        var cachePath = GameIdentityPckCache.PathFor(fixture.DataDir, fixture.Branch);
        File.Delete(cachePath);
        var hasher = new CountingHasher();

        var identity = new GameIdentityReader(hasher).ReadReadOnly(fixture.DataDir, fixture.Branch);

        Equal(expected, identity, "A read-only cache miss must still calculate the authoritative identity.");
        Equal(1, hasher.CountFor(fixture.PckPath), "An uncached PCK must be hashed directly.");
        True(!File.Exists(cachePath), "Read-only inspection must not create a missing cache.");
    }

    private static void ReadOnlyIdentityRejectsStalePckCache()
    {
        using var fixture = GameInstallFixture.Create();
        var previous = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        var cachePath = GameIdentityPckCache.PathFor(fixture.DataDir, fixture.Branch);
        var cacheBytes = File.ReadAllBytes(cachePath);
        fixture.WritePck(0x42);
        fixture.CompleteGeneration(1002);
        var hasher = new CountingHasher();

        var identity = new GameIdentityReader(hasher).ReadReadOnly(fixture.DataDir, fixture.Branch);

        NotEqual(previous.PckSha256, identity.PckSha256, "Changed installation content must reject the old cached digest.");
        Equal(1, hasher.CountFor(fixture.PckPath), "A stale PCK cache must force an actual archive hash.");
        True(cacheBytes.SequenceEqual(File.ReadAllBytes(cachePath)), "Read-only inspection must not repair a stale cache on disk.");
    }

    private static void ReadOnlyIdentityDetectsSourceChangeOnPckCacheHit()
    {
        using var fixture = GameInstallFixture.Create(sourceText: "AAAAAAAAAAAAAAAAAAAA");
        var previous = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        var timestamp = File.GetLastWriteTimeUtc(fixture.SourceAssemblyPath);
        fixture.WriteSourceAssembly("BBBBBBBBBBBBBBBBBBBB");
        File.SetLastWriteTimeUtc(fixture.SourceAssemblyPath, timestamp);
        var hasher = new CountingHasher();

        var identity = new GameIdentityReader(hasher).ReadReadOnly(fixture.DataDir, fixture.Branch);

        NotEqual(previous.SourceAssemblySha256, identity.SourceAssemblySha256, "A same-size and same-timestamp DLL replacement must change the identity.");
        Equal(0, hasher.CountFor(fixture.PckPath), "Changing the DLL must not invalidate an unchanged generation-bound PCK digest.");
        Equal(1, hasher.CountFor(fixture.SourceAssemblyPath), "The source DLL must be read even when the archive cache hits.");
    }
}
