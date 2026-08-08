using STS2Mobile.Launcher;

var failures = new List<string>();

Expect(
    StartupPresentationLayerPolicy.HasValidOrdering,
    "startup, shader-warmup, and recovery layers should have a valid order"
);
Expect(
    StartupPresentationLayerPolicy.ShaderWarmupCanvasLayer
        > StartupPresentationLayerPolicy.StartupStatusCanvasLayer,
    "shader warmup should render above the startup cover"
);
Expect(
    StartupPresentationLayerPolicy.RecoveryCanvasLayer
        > StartupPresentationLayerPolicy.ShaderWarmupCanvasLayer,
    "recovery controls should render above shader warmup"
);
Expect(
    StartupPresentationLayerPolicy.StartupStatusZIndex == 4096,
    "the existing startup-cover z-index should remain unchanged"
);

var lifecycle = new ShaderWarmupPresentationLifecycle();
Expect(lifecycle.StartupCoverRetained, "the startup cover should exist below warmup");
Expect(!lifecycle.WarmupVisible, "warmup should start detached");
Expect(lifecycle.InputBlocked, "the retained startup cover should keep input blocked");
Expect(lifecycle.MarkVisible(), "the first warmup presentation should become visible");
Expect(lifecycle.WarmupVisible, "warmup should remain visible while it runs");
Expect(!lifecycle.MarkVisible(), "duplicate visible transitions should be rejected");
Expect(lifecycle.TryBeginCleanup(), "the first cleanup request should be accepted");
Expect(!lifecycle.WarmupVisible, "warmup should no longer be visible after cleanup begins");
Expect(lifecycle.StartupCoverRetained, "cleanup should not remove the startup cover");
Expect(lifecycle.InputBlocked, "input should remain blocked during warmup handoff");
Expect(!lifecycle.TryBeginCleanup(), "duplicate cleanup should be idempotent");
Expect(!lifecycle.MarkVisible(), "warmup should not reappear after cleanup begins");

var interrupted = new ShaderWarmupPresentationLifecycle();
Expect(
    interrupted.TryBeginCleanup(),
    "lifecycle teardown should be accepted before warmup becomes visible"
);
Expect(
    interrupted.StartupCoverRetained && interrupted.InputBlocked,
    "early teardown should preserve the underlying startup presentation"
);
Expect(
    !interrupted.MarkVisible(),
    "a presentation should not become visible after early lifecycle teardown"
);

if (failures.Count > 0)
{
    Console.Error.WriteLine("Shader-warmup presentation policy probe failed:");
    foreach (var failure in failures)
        Console.Error.WriteLine($"- {failure}");
    return 1;
}

Console.WriteLine(
    "Shader-warmup presentation policy probe passed "
    + "(layer ordering, retained input block, normal cleanup, early teardown)."
);
return 0;

void Expect(bool condition, string message)
{
    if (!condition)
        failures.Add(message);
}
