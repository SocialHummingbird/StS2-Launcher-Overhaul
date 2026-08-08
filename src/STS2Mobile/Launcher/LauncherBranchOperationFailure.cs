using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherBranchOperationFailure
{
    internal LauncherBranchOperationFailure(string branch, string message)
    {
        Branch = SteamGameBranch.Normalize(branch);
        Message = message;
    }

    internal string Branch { get; }
    internal string Message { get; }
}
