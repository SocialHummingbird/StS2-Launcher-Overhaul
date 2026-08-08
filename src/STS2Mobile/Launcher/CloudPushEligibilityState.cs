namespace STS2Mobile.Launcher;

internal readonly record struct CloudPushEligibilityState(
    bool HasImportantLocalSaveEvidence,
    bool HasIncompletePull = false,
    bool HasRecoverySyncHold = false
);
