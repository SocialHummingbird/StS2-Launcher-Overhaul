using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

// Thin wrapper Control that initializes the MVC launcher components and
// processes a main-thread action queue so SteamKit callbacks can update the UI.
internal sealed partial class LauncherUI : Control
{
    private const string AutoLaunchVariable = "STS2_AUTO_LAUNCH_GAME";
    private const string AutoSafeLaunchVariable = "STS2_AUTO_SAFE_LAUNCH";
    private const int LauncherZIndex = 100;
    private static readonly Vector2 DefaultViewportSize = new(1920, 1080);

    private readonly ConcurrentQueue<Action> _mainThreadActions = new();
    private LauncherModel _model;
    private LauncherView _view;
    private LauncherController _controller;
    private Vector2 _lastViewportSize;
    private bool _inGameMode;

    internal void SetGameMode(bool inGameMode) => _inGameMode = inGameMode;

    internal Task WaitForLaunch() => _model.WaitForLaunch();
}
