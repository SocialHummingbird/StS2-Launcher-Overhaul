using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private sealed partial class StartupMode
    {
        private readonly struct PreviousStartupPhase
        {
            private PreviousStartupPhase(string phase)
            {
                Phase = phase;
            }

            private string Phase { get; }

            internal static PreviousStartupPhase FromMarkers()
                => new(LauncherLaunchMarkers.ReadStartupPhase());

            internal bool Matches(string phase)
                => string.Equals(Phase, phase, StringComparison.OrdinalIgnoreCase);

            internal string DescribePreviousStall(string message)
                => $"{message} {Phase}";
        }
    }
}
