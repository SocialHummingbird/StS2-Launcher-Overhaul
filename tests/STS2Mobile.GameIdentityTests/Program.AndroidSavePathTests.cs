using STS2Mobile.Patches;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void AndroidSavePathsOnlyNormalizeDefaultAccount()
    {
        const string account = "user://default/0";
        var cases = new (string Original, string Expected)[]
        {
            (account, "user://"),
            (account + "/", "user://"),
            (account + "/profile.save", "user://profile.save"),
            (account + "/profile1/progress.save", "user://profile1/progress.save"),
            (account + "/modded/profile3/history/run.json", "user://modded/profile3/history/run.json"),
            (account + "/modded/", "user://modded/"),
            ("user://default/1/profile1/progress.save", "user://default/1/profile1/progress.save"),
            ("user://steam/0/profile1/progress.save", "user://steam/0/profile1/progress.save"),
            ("user://default/01/profile.save", "user://default/01/profile.save"),
            ("user://default/0-other/profile.save", "user://default/0-other/profile.save"),
            ("user://DEFAULT/0/profile.save", "user://DEFAULT/0/profile.save"),
            ("user://profile.save", "user://profile.save"),
        };
        foreach (var test in cases)
            Equal(test.Expected, AndroidSavePathNormalizer.Normalize(test.Original, account),
                "Only the exact default account boundary may change: " + test.Original);
        Equal(account + "/profile1/progress.save",
            AndroidSavePathNormalizer.Normalize(account + "/profile1/progress.save", account, hasOtherAccountOverride: true),
            "An explicit different platform/account override must survive even if its native directory aliases the default.");
    }
}
