using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void SceneReadinessPreservesOrigin()
    {
        var binding = new LauncherSceneReadinessBinding();
        var game = new object();
        var oldScene = new object();
        var newScene = new object();
        binding.Begin(game, HandoffAttemptA);
        binding.Bind(oldScene);
        binding.End(HandoffAttemptA);
        binding.Begin(game, HandoffAttemptB);
        binding.Bind(oldScene); // A reused instance must never acquire B's identity.
        True(!binding.MarkReady(oldScene, game), "A delayed A callback must not become B's readiness.");
        True(!binding.IsReady(game, HandoffAttemptB, oldScene), "The stale current scene must not authorize handoff.");
        binding.Bind(newScene);
        True(binding.MarkReady(newScene, game), "B's own callback must be accepted.");
        True(binding.IsReady(game, HandoffAttemptB, newScene), "B's current scene must authorize render observation.");
        True(binding.TryGetReadyAttempt(game, HandoffAttemptB, newScene, out var captured)
            && captured == HandoffAttemptB, "The ready producer must return its construction-time attempt identity.");
        binding.End(HandoffAttemptA);
        True(binding.IsReady(game, HandoffAttemptB, newScene), "A late reset must not clear B.");
        binding.End(HandoffAttemptB);
        True(!binding.IsReady(game, HandoffAttemptB, newScene), "Ending the operation must invalidate its scene.");
    }

    private static void SceneReadinessRequiresSourceAndInstance()
    {
        var binding = new LauncherSceneReadinessBinding();
        var game = new object();
        var scene = new object();
        var replacement = new object();
        binding.Begin(game, HandoffAttemptA);
        binding.Bind(scene);
        True(!binding.MarkReady(scene, new object()), "A callback from another game must be rejected.");
        True(!binding.IsReady(game, HandoffAttemptA, scene), "Rejected callbacks must leave readiness false.");
        True(binding.MarkReady(scene, game), "The originating game callback must be accepted.");
        True(!binding.IsReady(game, HandoffAttemptA, replacement), "A different current scene must not inherit readiness.");
        True(!binding.IsReady(new object(), HandoffAttemptA, scene), "A different consumer game must be rejected.");
        var unowned = new object();
        binding.End(HandoffAttemptA);
        binding.Bind(unowned);
        binding.Begin(game, HandoffAttemptB);
        binding.Bind(unowned);
        True(!binding.MarkReady(unowned, game), "A scene created outside startup cannot be adopted later.");
        binding.End(HandoffAttemptB);
        binding.Begin(game, HandoffAttemptA);
        True(!binding.MarkReady(scene, game), "Even a reused attempt ID cannot adopt a scene from an ended operation.");
    }
}
