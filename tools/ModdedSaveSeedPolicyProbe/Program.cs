using STS2Mobile;
using STS2Mobile.Steam;

RunVanillaOnlyCopySetTest();
RunCloudModdedAuthorityTest();
RunAccountAuthorityAndCanonicalizationTest();
RunDeprecatedSavePathModIdentityTest();

Console.WriteLine("Modded save seed policy probe passed (4 scenarios). Steam Cloud Push was not run.");

static void RunVanillaOnlyCopySetTest()
{
    var plan = ModdedSaveSeedPlan.Create(new[]
    {
        "profile.save",
        "profile1/saves/progress.save",
        "profile1/saves/current_run.save",
        "profile1/saves/current_run_mp.save",
        "profile1/saves/prefs.save",
        "profile1/saves/history/2026-07-16.run",
        "profile1/saves/progress.save.backup",
        "profile1/saves/prefs",
    });

    AssertEqual(6, plan.SeedMappings.Count, "vanilla-only seed count");
    AssertEqual(0, plan.DirectCloudModdedTargets.Count, "vanilla-only direct modded count");
    AssertEqual(0, plan.CloudAuthoritativeNamespaces.Count, "vanilla-only authorities");
    AssertHasMapping(plan, "profile.save", "modded/profile.save");
    AssertHasMapping(
        plan,
        "profile1/saves/history/2026-07-16.run",
        "modded/profile1/saves/history/2026-07-16.run"
    );
    AssertNoTargetEnding(plan, ".backup");
    AssertNoTargetEnding(plan, "/prefs");
}

static void RunCloudModdedAuthorityTest()
{
    var plan = ModdedSaveSeedPlan.Create(new[]
    {
        "profile1/saves/progress.save",
        "profile1/saves/prefs.save",
        "modded/profile1/saves/progress.save",
        "profile2/saves/progress.save",
        "profile2/saves/history/run-a.run",
    });

    AssertEqual(2, plan.SeedMappings.Count, "mixed seed count");
    AssertEqual(1, plan.DirectCloudModdedTargets.Count, "mixed direct modded count");
    AssertEqual(1, plan.CloudAuthoritativeNamespaces.Count, "mixed authorities");
    AssertEqual("profile1", plan.CloudAuthoritativeNamespaces[0], "mixed authority name");
    AssertFalse(
        plan.SeedMappings.Any(mapping => mapping.SaveNamespace == "profile1"),
        "profile1 must not mix vanilla files into a cloud-provided modded profile"
    );
    AssertHasMapping(
        plan,
        "profile2/saves/progress.save",
        "modded/profile2/saves/progress.save"
    );
}

static void RunAccountAuthorityAndCanonicalizationTest()
{
    var plan = ModdedSaveSeedPlan.Create(new[]
    {
        "user://profile.save",
        "user://modded\\profile.save",
        "user://profile3/saves/progress.save",
        "user://modded/profile3/unknown.save",
    });

    AssertEqual(0, plan.SeedMappings.Count, "canonical authority seed count");
    AssertEqual(2, plan.DirectCloudModdedTargets.Count, "canonical direct modded count");
    AssertEqual(2, plan.CloudAuthoritativeNamespaces.Count, "canonical authorities");
    AssertTrue(
        plan.CloudAuthoritativeNamespaces.Contains("account"),
        "modded profile.save must make account metadata cloud-authoritative"
    );
    AssertTrue(
        plan.CloudAuthoritativeNamespaces.Contains("profile3"),
        "any cloud modded profile content must prevent namespace mixing"
    );
}

static void RunDeprecatedSavePathModIdentityTest()
{
    AssertTrue(
        DeprecatedSavePathMod.IsMatch("3747532120", "Vanilla and Modded Saves Merger"),
        "full Workshop Saves Merger title must be deprecated"
    );
    AssertTrue(
        DeprecatedSavePathMod.IsMatch("UnifiedSavePath", "unrelated title"),
        "legacy UnifiedSavePath id must be deprecated"
    );
    AssertFalse(
        DeprecatedSavePathMod.IsMatch("QuickRestart", "Quick Restart 2"),
        "unrelated mods must remain selectable"
    );
}

static void AssertHasMapping(ModdedSaveSeedPlan plan, string source, string target)
{
    AssertTrue(
        plan.SeedMappings.Any(mapping =>
            mapping.SourcePath.Equals(source, StringComparison.OrdinalIgnoreCase)
            && mapping.TargetPath.Equals(target, StringComparison.OrdinalIgnoreCase)),
        $"missing mapping {source} -> {target}"
    );
}

static void AssertNoTargetEnding(ModdedSaveSeedPlan plan, string suffix)
{
    AssertFalse(
        plan.SeedMappings.Any(mapping =>
            mapping.TargetPath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)),
        $"excluded path suffix was seeded: {suffix}"
    );
}

static void AssertEqual<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
}

static void AssertTrue(bool value, string message)
{
    if (!value)
        throw new InvalidOperationException(message);
}

static void AssertFalse(bool value, string message)
    => AssertTrue(!value, message);
