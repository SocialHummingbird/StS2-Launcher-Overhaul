using System.IO;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal static partial class LauncherRuntimeSlotEvidence
{
    private const string RuntimeSlotIdProperty = "runtimeSlotId";
    private const string BranchProperty = "branch";
    private const string FilesReadyProperty = "filesReady";
    private const string ReadinessProblemProperty = "readinessProblem";
    private const string RuntimePackUsabilityStatusProperty = "runtimePackUsabilityStatus";
    private const string PatchCompatibilityStatusProperty = "patchCompatibilityStatus";
    private const string PckSha256Property = "pckSha256";
    private const string SourceAssemblySha256Property = "sourceAssemblySha256";

    internal static string RuntimeSlotId(string dataDir)
        => ReadString(dataDir, RuntimeSlotIdProperty);

    internal static string Branch(string dataDir)
        => ReadString(dataDir, BranchProperty);

    internal static string FilesReady(string dataDir)
        => ReadString(dataDir, FilesReadyProperty);

    internal static string ReadinessProblem(string dataDir)
        => ReadString(dataDir, ReadinessProblemProperty);

    internal static string RuntimePackUsabilityStatus(string dataDir)
        => ReadString(dataDir, RuntimePackUsabilityStatusProperty);

    internal static string PatchCompatibilityStatus(string dataDir)
        => ReadString(dataDir, PatchCompatibilityStatusProperty);

    private static bool IsMissing(string value)
        => string.IsNullOrWhiteSpace(value) || value.StartsWith("<", System.StringComparison.Ordinal);

    private static string ReadString(string dataDir, string property)
    {
        try
        {
            if (!File.Exists(MarkerPath(dataDir)))
                return "<none>";

            using var document = JsonDocument.Parse(File.ReadAllText(MarkerPath(dataDir)));
            if (!document.RootElement.TryGetProperty(property, out var value))
                return "<missing>";

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? "<missing>",
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => value.ToString(),
                _ => "<unsupported>"
            };
        }
        catch
        {
            return "<read failed>";
        }
    }
}
