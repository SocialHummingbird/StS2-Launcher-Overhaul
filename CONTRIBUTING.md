# Contributing

Keep each change focused on behavior the launcher currently supports.

## Pull requests

Include:

- What changed and why.
- Reproduction steps for a bug fix.
- The focused commands you ran and their results.
- Any device or real-Steam validation performed.
- Important limitations that remain untested.

Do not substitute source-shape checks, broad matrices, or generated evidence bundles for a direct behavior test. If a required check cannot run, state the concrete blocker.

## Focused validation

Use the smallest relevant checks:

```powershell
dotnet build src\STS2Mobile\STS2Mobile.csproj -c Release
.\scripts\test-local-gameplay-save-safety.ps1
.\scripts\test-launcher-ui-preview.ps1
```

The save tests use a small fake remote and do not prove Steam or Android transport. Desktop launcher interaction does not prove Android rendering or input. Report device and live-service results separately and only for the exact build tested.
