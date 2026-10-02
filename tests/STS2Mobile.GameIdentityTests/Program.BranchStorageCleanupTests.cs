using System;
using STS2Mobile.Steam;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void BranchStorageNamesPreserveBootstrapCompatibility()
    {
        Equal("public", SteamGameBranch.StateDirectoryName(""), "Missing selection must keep the public directory.");
        Equal("public", SteamGameBranch.StateDirectoryName(" PUBLIC "), "Public selection must keep its legacy directory.");
        Equal("beta", SteamGameBranch.StateDirectoryName("BeTa"), "Beta selection must keep its legacy directory.");
        // The pre-change characterization verified the bootstrap and managed algorithms agree.
        Equal("release_foo-96b15b8d", SteamGameBranch.StateDirectoryName("release/foo"), "Custom branch persisted identity changed.");
        Equal("release_foo-4f7ae024", SteamGameBranch.StateDirectoryName("release:foo"), "Branches sharing a safe prefix must remain distinct.");
        Equal("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx-37d12e35", SteamGameBranch.StateDirectoryName(new string('x', 60)), "Long branch persisted identity changed.");
        Equal("café-3308be7c", SteamGameBranch.StateDirectoryName("café"), "Unicode branch persisted identity changed.");
    }
}
