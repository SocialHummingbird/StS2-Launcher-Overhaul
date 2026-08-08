using STS2Mobile.Steam;

var failures = new List<string>();

ExpectBackup("profile.save", expected: true);
ExpectBackup("modded/profile.save", expected: true);
ExpectBackup("profile1/saves/progress.save", expected: true);
ExpectBackup("modded/profile2/saves/prefs", expected: true);
ExpectBackup("profile3/saves/history/20260717.run", expected: true);
ExpectBackup("profile1/saves/current_run.save.backup", expected: true);
ExpectBackup("profile1/saves/current_run.save.tmp", expected: false);
ExpectBackup(
    ".sts2-launcher/transfer-backups/operation/profile1/saves/progress.save",
    expected: false
);
ExpectBackup(
    "user://.STS2-LAUNCHER/pull-incomplete/vanilla.save",
    expected: false
);
ExpectBackup("cloud_sync/last_manual_pull.json", expected: false);
ExpectBackup("profile1/saves/progress.save.bak", expected: false);

ExpectImportant("profile.save", expected: true);
ExpectImportant("modded/profile.save", expected: true);
ExpectImportant("profile1/saves/progress.save", expected: true);
ExpectImportant("profile1/saves/current_run.save", expected: true);
ExpectImportant("profile1/saves/prefs", expected: true);
ExpectImportant("profile1/saves/history/20260717.run", expected: false);
ExpectImportant("otherprofile.save", expected: false);

var root = Path.Combine(Path.GetTempPath(), "sts2-local-backup-probe");
ExpectResolved(root, "profile1/saves/progress.save", expected: true);
ExpectResolved(root, "modded/profile1/saves/progress.save", expected: true);
ExpectResolved(root, "../outside.save", expected: false);
ExpectResolved(root, "profile1/../../outside.save", expected: false);
ExpectResolved(root, "C:/outside.save", expected: false);

LocalSaveBackupPlan.TryResolveUnderRoot(
    root,
    "profile1/saves/progress.save",
    out var vanillaPath
);
LocalSaveBackupPlan.TryResolveUnderRoot(
    root,
    "modded/profile1/saves/progress.save",
    out var moddedPath
);
Expect(
    !string.Equals(vanillaPath, moddedPath, StringComparison.OrdinalIgnoreCase),
    "vanilla and modded saves must resolve to distinct mirror paths"
);

if (failures.Count > 0)
{
    Console.Error.WriteLine("Local save backup policy probe failed:");
    foreach (var failure in failures)
        Console.Error.WriteLine($"- {failure}");
    return 1;
}

Console.WriteLine("Local save backup policy probe passed (backup classification, path containment, namespace preservation).");
return 0;

void ExpectBackup(string path, bool expected)
    => Expect(
        LocalSaveBackupPlan.IsBackupEligible(path) == expected,
        $"backup eligibility for '{path}' should be {expected}"
    );

void ExpectImportant(string path, bool expected)
    => Expect(
        CloudSavePath.IsImportantForBackup(path) == expected,
        $"important backup classification for '{path}' should be {expected}"
    );

void ExpectResolved(string pathRoot, string relativePath, bool expected)
    => Expect(
        LocalSaveBackupPlan.TryResolveUnderRoot(pathRoot, relativePath, out _) == expected,
        $"path containment for '{relativePath}' should be {expected}"
    );

void Expect(bool condition, string message)
{
    if (!condition)
        failures.Add(message);
}
