using System;
using System.IO;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal sealed partial class PatchCompatibilityEvidence
{
    private static PatchCompatibilityEvidence ReadValidationMarker(
        string path,
        string branch,
        string selectedPckSha256,
        string selectedSourceAssemblySha256,
        string source
    )
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return MissingValidationMarker(path, branch, source);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return ReadValidationDocument(
                document.RootElement,
                path,
                branch,
                selectedPckSha256,
                selectedSourceAssemblySha256,
                source
            );
        }
        catch (Exception ex)
        {
            return UnreadableValidationMarker(path, branch, source, ex);
        }
    }
}
