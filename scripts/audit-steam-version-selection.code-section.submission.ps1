function Add-SteamVersionSelectionCodeSectionSubmissionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CodeSection.cs" `
        "wires Steam Guard code input to alphanumeric normalization and one-shot submit" `
        @(
            "TextChanged \+= NormalizeCodeText",
            "TextSubmitted \+= _ => OnSubmit\(\)",
            "submitButton\.Pressed \+= OnSubmit",
            "CodeSubmitted"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CodeSection.Submission.cs" `
        "normalizes Steam Guard codes to uppercase alphanumeric text before submitting once" `
        @(
            "NormalizeCodeText",
            "NormalizeCode\(string text\)",
            "char\.IsLetterOrDigit",
            "char\.ToUpperInvariant",
            "CodeSubmitted\?\.Invoke\(code\)"
        )
}
