using System.IO;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimePackManifest
{
    private static bool PatchValidationReportMatches(string reportPath, RuntimePackManifest manifest)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
            var root = document.RootElement;
            if (!StringPropertyMatches(root, "status", "passed"))
                return false;
            return StringPropertyMatches(root, "runtimePackId", manifest.PackId)
                && StringPropertyMatches(root, "sourceRuntimeSlotId", manifest.SourceRuntimeSlotId)
                && StringPropertyMatches(root, "branch", manifest.SourceBranch)
                && StringPropertyMatches(root, "pckSha256", manifest.SourcePckSha256)
                && StringPropertyMatches(root, "sourceAssemblySha256", manifest.SourceAssemblySha256)
                && StringPropertyMatches(root, "androidAssemblySha256", manifest.AndroidAssemblySha256)
                && StringPropertyMatches(root, "patchSetVersion", manifest.PatchSetVersion)
                && StringPropertyMatches(root, "validationSurfaceVersion", manifest.ValidationSurfaceVersion)
                && StringArrayPropertyMatches(root, "supportAssemblies", manifest.SupportAssemblies)
                && StringDictionaryPropertyMatches(root, "supportAssemblySha256", manifest.SupportAssemblySha256)
                && BoolPropertyMatches(root, "generatedFromCleanDirectory", manifest.GeneratedFromCleanDirectory);
        }
        catch
        {
            return false;
        }
    }
}
