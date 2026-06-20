using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const int LocalLoginPollDelayMs = 500;
    private static readonly TimeSpan LocalLoginPollTimeout = TimeSpan.FromSeconds(180);

    private int _localLoginHandoffStarted;
}
