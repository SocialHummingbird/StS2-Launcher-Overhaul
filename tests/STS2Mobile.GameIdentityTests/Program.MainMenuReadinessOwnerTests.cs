using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void MainMenuReadinessNormal()
    {
        var readiness = new LauncherMainMenuReadinessOwner();

        True(readiness.Begin(HandoffAttemptA), "A launch attempt must establish readiness ownership.");
        True(!readiness.IsReady(HandoffAttemptA), "A new attempt must begin not ready.");
        True(readiness.MarkReady(HandoffAttemptA), "The active attempt must accept readiness.");
        True(readiness.IsReady(HandoffAttemptA), "The active attempt must retain readiness.");
    }

    private static void MainMenuReadinessRepeatedOperations()
    {
        var readiness = new LauncherMainMenuReadinessOwner();

        True(readiness.Begin(HandoffAttemptA), "The first begin must establish ownership.");
        True(readiness.MarkReady(HandoffAttemptA), "The first ready operation must change state.");
        True(!readiness.Begin(HandoffAttemptA), "Repeated begin must be idempotent.");
        True(!readiness.MarkReady(HandoffAttemptA), "Repeated ready must be idempotent.");
        True(readiness.IsReady(HandoffAttemptA), "Repeated operations must not clear readiness.");
    }

    private static void MainMenuReadinessIgnoresStaleAttempts()
    {
        var readiness = new LauncherMainMenuReadinessOwner();

        readiness.Begin(HandoffAttemptA);
        True(readiness.Begin(HandoffAttemptB), "A new attempt must replace earlier readiness ownership.");
        True(!readiness.MarkReady(HandoffAttemptA), "A stale producer must be ignored.");
        True(!readiness.IsReady(HandoffAttemptA), "A stale consumer must not observe current readiness.");
        True(!readiness.Reset(HandoffAttemptA), "A stale reset must be ignored.");
        True(readiness.MarkReady(HandoffAttemptB), "The active attempt must remain writable after stale callbacks.");
        True(readiness.IsReady(HandoffAttemptB), "Stale callbacks must not damage the active attempt.");
    }

    private static void MainMenuReadinessResetsForNewAttempt()
    {
        var readiness = new LauncherMainMenuReadinessOwner();

        readiness.Begin(HandoffAttemptA);
        readiness.MarkReady(HandoffAttemptA);
        True(readiness.Reset(HandoffAttemptA), "The active attempt must reset once.");
        True(!readiness.Reset(HandoffAttemptA), "Repeated reset must be idempotent.");
        True(!readiness.IsReady(HandoffAttemptA), "Reset must remove earlier readiness.");
        True(readiness.Begin(HandoffAttemptB), "A new attempt must begin after reset.");
        True(!readiness.IsReady(HandoffAttemptB), "A new attempt must not inherit readiness.");
    }

    private static void MainMenuReadinessProducerBeforeConsumer()
    {
        var readiness = new LauncherMainMenuReadinessOwner();

        readiness.Begin(HandoffAttemptA);
        readiness.MarkReady(HandoffAttemptA);

        True(
            readiness.IsReady(HandoffAttemptA),
            "A consumer attaching after the producer must observe durable attempt-bound readiness."
        );
    }

    private static void MainMenuReadinessConsumerBeforeProducer()
    {
        var readiness = new LauncherMainMenuReadinessOwner();

        readiness.Begin(HandoffAttemptA);
        True(!readiness.IsReady(HandoffAttemptA), "A consumer must observe not-ready before production.");
        readiness.MarkReady(HandoffAttemptA);
        True(readiness.IsReady(HandoffAttemptA), "The same consumer must observe readiness after production.");
    }
}
