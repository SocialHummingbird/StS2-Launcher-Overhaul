using Godot;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private readonly struct ForcedMainMenuLoadAttempt
    {
        private readonly object _game;
        private readonly Label _startupStatus;
        private readonly int _timeoutMs;
        private readonly Task _loadMainMenu;

        private ForcedMainMenuLoadAttempt(
            object game,
            Label startupStatus,
            int timeoutMs,
            Task loadMainMenu
        )
        {
            _game = game;
            _startupStatus = startupStatus;
            _timeoutMs = timeoutMs;
            _loadMainMenu = loadMainMenu;
        }

        internal static bool TryStart(
            object game,
            Label startupStatus,
            int timeoutMs,
            out ForcedMainMenuLoadAttempt attempt
        )
        {
            var loadMainMenu = StartLoadMainMenu(game);
            if (loadMainMenu == null)
            {
                attempt = default;
                return false;
            }

            attempt = new ForcedMainMenuLoadAttempt(
                game,
                startupStatus,
                timeoutMs,
                loadMainMenu
            );
            return true;
        }

        internal async Task<bool> CompleteWithinTimeoutAsync()
        {
            if (!await WaitForLoadAsync())
                return false;

            await _loadMainMenu;
            return PublishResult();
        }

        private async Task<bool> WaitForLoadAsync()
        {
            if (await LauncherTimeout.CompletesWithinAsync(_loadMainMenu, _timeoutMs))
                return true;

            PatchHelper.Log($"Forced LoadMainMenu timed out after {_timeoutMs}ms");
            LauncherStartupStatus.Set(
                _startupStatus,
                "Forced main menu load timed out."
            );
            return false;
        }

        private bool PublishResult()
        {
            var scene = InspectCurrentScene(_game);
            PatchHelper.Log(ForcedLoadResultMessage(scene));
            LauncherStartupStatus.Set(_startupStatus, ForcedLoadStatus(scene));
            return scene.IsMainMenu;
        }
    }

    private static async Task<bool> ForceLoadMainMenuAsync(
        object game,
        Label startupStatus,
        int forceTimeoutMs
    )
    {
        if (!ForcedMainMenuLoadAttempt.TryStart(
            game,
            startupStatus,
            forceTimeoutMs,
            out var attempt
        ))
            return false;

        return await attempt.CompleteWithinTimeoutAsync();
    }
}
