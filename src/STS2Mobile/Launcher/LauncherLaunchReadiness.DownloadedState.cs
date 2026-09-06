using System;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchReadiness
{
    internal static LauncherLaunchReadiness EvaluateDownloadedState(
        string dataDir,
        string branch,
        string phase
    )
    {
        branch = SteamGameBranch.Normalize(branch);
        try
        {
            LauncherLaunchMarkers.RecordPhase(
                $"{phase}: checking selected downloaded files",
                $"branch={branch}"
            );

            if (!LauncherGameFiles.DownloadedForValidation(dataDir, branch, out var downloadProblem))
            {
                var problem = downloadProblem
                    ?? "Selected game version is not ready to launch.";
                LauncherLaunchMarkers.RecordPhase($"{phase}: selected downloaded files blocked", problem);
                return new LauncherLaunchReadiness(
                    dataDir,
                    branch,
                    ready: false,
                    problem,
                    runtimeSlot: null,
                    phase,
                    LauncherLaunchReadinessCacheStatus.DownloadedStateOnly
                );
            }

            LauncherLaunchMarkers.RecordPhase(
                $"{phase}: selected downloaded files present",
                $"branch={branch}; final runtime validation deferred to Start Game"
            );
            return new LauncherLaunchReadiness(
                dataDir,
                branch,
                ready: true,
                readinessProblem: string.Empty,
                runtimeSlot: null,
                phase,
                LauncherLaunchReadinessCacheStatus.DownloadedStateOnly
            );
        }
        catch (Exception ex)
        {
            var diagnostic = DownloadedStateExceptionProblem(ex);
            var problem = "The selected branch could not be checked. Try again. If it happens again, create a new support report.";
            LauncherLaunchMarkers.RecordPhase($"{phase}: selected downloaded files check failed", diagnostic);
            PatchHelper.Log($"[Launcher] {diagnostic}");
            return new LauncherLaunchReadiness(
                dataDir,
                branch,
                ready: false,
                problem,
                runtimeSlot: null,
                phase,
                LauncherLaunchReadinessCacheStatus.DownloadedStateCheckFailed
            );
        }
    }

    private static string DownloadedStateExceptionProblem(Exception exception)
    {
        var exceptionName = exception?.GetType().Name ?? "Exception";
        var message = CleanExceptionMessage(exception?.Message);
        return $"Selected game version check failed before launch. Start Game will run the full readiness check after this is resolved. ({exceptionName}: {message})";
    }

    private static string CleanExceptionMessage(string message)
        => string.IsNullOrWhiteSpace(message)
            ? "No exception message was provided."
            : message.Replace('\r', ' ').Replace('\n', ' ').Trim();
}
