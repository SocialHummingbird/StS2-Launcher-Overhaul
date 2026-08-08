namespace STS2Mobile.Launcher;

internal interface IPostStartupDiagnosticsConfigurationSource
{
    string EnvironmentValue { get; }
    bool MarkerExists { get; }
}

internal static class PostStartupDiagnosticsConfiguration
{
    internal static bool DetailedTraceEnabled(
        IPostStartupDiagnosticsConfigurationSource source
    )
        => PostStartupDiagnosticsPolicy.DetailedTraceEnabled(
            source.EnvironmentValue,
            source.MarkerExists
        );
}
