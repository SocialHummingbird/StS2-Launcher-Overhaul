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
        new("cloud-save", "namespace", "MegaCrit.Sts2.Core.Saves"),
        new("cloud-save", "type", "SaveManager"),
        new("cloud-save", "method", "ConstructDefault"),
        new("cloud-save", "method", "SyncCloudToLocal"),
        new("cloud-save", "type", "CloudSaveStore"),
        new("model-db", "namespace", "MegaCrit.Sts2.Core.Models"),
        new("model-db", "type", "ModelDb"),
        new("model-db", "method", "Init"),
        new("model-db", "field", "AllAbstractModelSubtypes"),
        new("model-db", "method", "GetId"),
        new("model-db", "method", "Contains"),
        new("model-db", "field", "_contentById"),
        new("platform", "namespace", "MegaCrit.Sts2.Core.Platform"),
        new("platform", "type", "PlatformUtil"),
        new("platform", "property", "PrimaryPlatform"),
        new("platform", "method", "GetPlatformUtil"),
        new("platform", "namespace", "MegaCrit.Sts2.Core.Platform.Null"),
        new("platform", "type", "NullPlatformUtilStrategy"),
    };
}
