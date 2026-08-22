using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal sealed class RuntimePackCandidate
{
    internal RuntimePackCandidate(
        Guid attemptId,
        Guid installTransactionId,
        GameIdentity gameIdentity,
        string stagingDirectory,
        RuntimePackManifest manifest
    )
    {
        if (attemptId == Guid.Empty)
            throw new ArgumentException("A runtime-pack candidate attempt ID is required.", nameof(attemptId));
        if (installTransactionId == Guid.Empty)
            throw new ArgumentException("An install transaction ID is required.", nameof(installTransactionId));

        AttemptId = attemptId;
        InstallTransactionId = installTransactionId;
        GameIdentity = gameIdentity ?? throw new ArgumentNullException(nameof(gameIdentity));
        StagingDirectory = Path.GetFullPath(
            stagingDirectory ?? throw new ArgumentNullException(nameof(stagingDirectory))
        );
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
    }

    internal Guid AttemptId { get; }
    internal Guid InstallTransactionId { get; }
    internal GameIdentity GameIdentity { get; }
    internal string StagingDirectory { get; }
    internal string CompatibilityManifestPath => Path.Combine(StagingDirectory, "compatibility.json");
    internal string PatchValidationReportPath => Path.Combine(StagingDirectory, "patch_validation.json");
    internal string AndroidAssemblyPath => Path.Combine(StagingDirectory, RuntimePackManifest.AndroidAssemblyFileName);
    internal RuntimePackManifest Manifest { get; }
}

internal sealed class RuntimePackCandidateGenerationResult
{
    private RuntimePackCandidateGenerationResult(
        RuntimePackCandidate candidate,
        string problem
    )
    {
        Candidate = candidate;
        Problem = problem ?? string.Empty;
    }

    internal RuntimePackCandidate Candidate { get; }
    internal string Problem { get; }
    internal bool Succeeded => Candidate != null && string.IsNullOrWhiteSpace(Problem);

    internal static RuntimePackCandidateGenerationResult Success(RuntimePackCandidate candidate)
        => new(candidate ?? throw new ArgumentNullException(nameof(candidate)), string.Empty);

    internal static RuntimePackCandidateGenerationResult Rejected(string problem)
        => new(null, string.IsNullOrWhiteSpace(problem) ? "Runtime-pack candidate generation failed." : problem);
}

internal sealed class RuntimePackCandidateValidationResult
{
    private RuntimePackCandidateValidationResult(
        RuntimePackManifest manifest,
        string problem
    )
    {
        Manifest = manifest;
        Problem = problem ?? string.Empty;
    }

    internal RuntimePackManifest Manifest { get; }
    internal string Problem { get; }
    internal bool Usable => Manifest?.Usable == true && string.IsNullOrWhiteSpace(Problem);

    internal static RuntimePackCandidateValidationResult Valid(RuntimePackManifest manifest)
        => new(manifest ?? throw new ArgumentNullException(nameof(manifest)), string.Empty);

    internal static RuntimePackCandidateValidationResult Rejected(string problem)
        => new(null, string.IsNullOrWhiteSpace(problem) ? "Runtime-pack candidate is invalid." : problem);
}

internal sealed class RuntimePackGenerationHooks
{
    internal Action<string> AfterSourceCopied { get; init; }
    internal Action<string> AfterReportWritten { get; init; }
    internal Action<string> BeforeCandidateValidation { get; init; }
}
