using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private (VBoxContainer Group, Label StateLine, Button HelpButton) BuildHomeJourney(
        float scale,
        bool compact
    )
    {
        var group = BuildActionGroup(scale);
        group.Name = "HomeJourney";
        AddChild(group);

        var stateLine = new StyledLabel(
            "Public · Vanilla saves · Not synced yet",
            scale,
            fontSize: compact ? 15 : 16,
            align: HorizontalAlignment.Left
        )
        {
            Name = "HomeStateLine",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        stateLine.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        group.AddChild(stateLine);

        var helpButton = AddSecondaryHiddenButton(
            group,
            "Open Help",
            scale,
            () => HomeHelpPressed?.Invoke()
        );
        helpButton.AccessibilityName = "Open Help";
        LauncherButtonStyles.ApplySupportAction(helpButton, scale);

        return (group, stateLine, helpButton);
    }

    private void UpdateHomeStateLine()
    {
        var normalizedBranch = SteamGameBranch.Normalize(_gameBranch);
        var branch = string.Equals(
            normalizedBranch,
            SteamGameBranch.Public,
            System.StringComparison.OrdinalIgnoreCase
        )
            ? "Public"
            : string.Equals(
                normalizedBranch,
                SteamGameBranch.Beta,
                System.StringComparison.OrdinalIgnoreCase
            )
                ? "Beta"
                : SteamGameBranch.DisplayName(_gameBranch);
        _homeStateLine.Text = $"{branch} · {_homeSaveNamespace} · {_homeSyncState}";
    }

    private static string PresentationText(string value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    internal void ShowHomeHelpAction()
        => _homeHelpButton.Visible = true;
}
