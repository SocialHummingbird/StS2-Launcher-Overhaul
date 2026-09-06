using System;
using System.Runtime.CompilerServices;

namespace STS2Mobile.Launcher;

// Engine-independent provenance. Entries live as long as their scene and are never
// rebound, including scenes constructed while no startup operation owns creation.
internal sealed class LauncherSceneReadinessBinding
{
    private sealed class SceneOrigin
    {
        internal object Operation;
        internal object Game;
        internal string AttemptId;
        internal bool Ready;
    }

    private readonly object _lock = new();
    private readonly ConditionalWeakTable<object, SceneOrigin> _scenes = new();
    private object _operation;
    private object _game;
    private string _attemptId;

    internal void Begin(object game, string attemptId)
    {
        ArgumentNullException.ThrowIfNull(game);
        LauncherMainMenuReadinessOwner.ValidateAttemptId(attemptId);
        lock (_lock)
        {
            if (_operation != null)
                throw new InvalidOperationException("A game scene startup operation is already active.");
            _operation = new object();
            _game = game;
            _attemptId = attemptId;
        }
    }

    internal void End(string attemptId)
    {
        lock (_lock)
        {
            if (!string.Equals(_attemptId, attemptId, StringComparison.Ordinal))
                return;
            _operation = null;
            _game = null;
            _attemptId = null;
        }
    }

    internal void Bind(object scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        lock (_lock)
        {
            if (!_scenes.TryGetValue(scene, out _))
                _scenes.Add(scene, new SceneOrigin { Operation = _operation, Game = _game, AttemptId = _attemptId });
        }
    }

    internal bool MarkReady(object scene, object sourceGame)
    {
        lock (_lock)
        {
            if (!TryGetActiveOrigin(scene, out var origin)
                || !ReferenceEquals(origin.Game, sourceGame))
                return false;
            origin.Ready = true;
            return true;
        }
    }

    internal bool IsReady(object game, string attemptId, object currentScene)
        => TryGetReadyAttempt(game, attemptId, currentScene, out _);

    internal bool TryGetReadyAttempt(object game, string attemptId, object currentScene, out string originatingAttemptId)
    {
        lock (_lock)
        {
            originatingAttemptId = null;
            if (!(ReferenceEquals(_game, game)
                && string.Equals(_attemptId, attemptId, StringComparison.Ordinal)
                && TryGetActiveOrigin(currentScene, out var origin)
                && origin.Ready))
                return false;
            originatingAttemptId = origin.AttemptId;
            return true;
        }
    }

    private bool TryGetActiveOrigin(object scene, out SceneOrigin origin)
    {
        origin = null;
        return scene != null && _operation != null
            && _scenes.TryGetValue(scene, out origin)
            && ReferenceEquals(origin.Operation, _operation);
    }
}
