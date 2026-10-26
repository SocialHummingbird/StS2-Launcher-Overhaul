namespace STS2Mobile.Launcher;

internal static partial class PatchCompatibilityValidator
{
    internal sealed class SymbolCheck
    {
        internal SymbolCheck(string category, string kind, string symbol, bool present)
        {
            Category = category;
            Kind = kind;
            Symbol = symbol;
            Present = present;
        }

        public string Category { get; }
        public string Kind { get; }
        public string Symbol { get; }
        public bool Present { get; }
        internal string FailureMessage => $"missing {Kind}: {Symbol}";
    }

    private sealed class RequiredCriticalSymbol
    {
        internal RequiredCriticalSymbol(string category, string kind, string symbol)
        {
            Category = category;
            Kind = kind;
            Symbol = symbol;
        }

        internal string Category { get; }
        internal string Kind { get; }
        internal string Symbol { get; }
    }

    private static readonly RequiredCriticalSymbol[] RequiredCriticalSymbols =
    {
        new("startup", "namespace", "MegaCrit.Sts2.Core.Nodes"),
        new("startup", "type", "GameStartupWrapper"),
        new("startup", "method", "StartOnMainMenu"),
        new("startup", "method", "InitializePlatform"),
        new("startup", "type", "NGame"),
        new("local-save", "namespace", "MegaCrit.Sts2.Core.Saves"),
        new("local-save", "type", "SaveManager"),
        new("local-save", "method", "ConstructDefault"),
        new("mod-loader", "namespace", "MegaCrit.Sts2.Core.Modding"),
        new("mod-loader", "type", "ModManager"),
        new("mod-loader", "method", "Initialize"),
        new("mod-loader", "method", "ReadModsInDirRecursive"),
        new("mod-loader", "method", "ReadModManifest"),
        new("mod-loader", "method", "ReadSteamMods"),
        new("mod-loader", "method", "TryLoadMod"),
        new("mod-loader", "type", "IModManagerFileIo"),
        new("mod-loader", "type", "ReleaseInfoManager"),
        new("mod-loader", "method", "GetPossibleReleaseInfoPaths"),
        new("platform", "namespace", "MegaCrit.Sts2.Core.Platform"),
        new("platform", "type", "PlatformUtil"),
        new("platform", "property", "PrimaryPlatform"),
        new("platform", "method", "GetPlatformUtil"),
        new("platform", "namespace", "MegaCrit.Sts2.Core.Platform.Null"),
        new("platform", "type", "NullPlatformUtilStrategy"),
    };
}
