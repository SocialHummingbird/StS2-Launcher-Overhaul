using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private (
        VBoxContainer Group,
        Label AccountState,
        Label GameState,
        Label SaveState,
        Label SaveNamespaceState
    ) BuildHomeJourney(float scale, bool compact)
    {
        var group = BuildActionGroup(scale);
        group.Name = "HomeJourney";
        AddChild(group);

        var rows = BuildStateRows(scale);
        rows.Name = "HomeJourneyRows";
        group.AddChild(rows);

        var accountState = AddStateRow(
            rows,
            "Steam account",
            "Checking...",
            "HomeAccountState",
            scale,
            compact
        );
        var gameState = AddStateRow(
            rows,
            "Game installation",
            "Checking...",
            "HomeGameState",
            scale,
            compact
        );
        var saveState = AddStateRow(
            rows,
            "Saves",
            "Sign in required",
            "HomeSaveState",
            scale,
            compact
        );
        var saveNamespaceState = AddStateRow(
            rows,
            "Next save set",
            "Vanilla saves",
            "HomeSaveNamespaceState",
            scale,
            compact
        );

        return (group, accountState, gameState, saveState, saveNamespaceState);
    }

    private static GridContainer BuildStateRows(float scale)
    {
        var rows = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        rows.AddThemeConstantOverride(
            "h_separation",
            LauncherViewLayoutMetrics.ScaleInt(12, scale)
        );
        rows.AddThemeConstantOverride(
            "v_separation",
            LauncherViewLayoutMetrics.ScaleInt(8, scale)
        );
        return rows;
    }

    private static Label AddStateRow(
        GridContainer rows,
        string title,
        string value,
        string valueName,
        float scale,
        bool compact
    )
    {
        var titleLabel = new StyledLabel(
            title,
            scale,
            fontSize: compact ? 12 : 13,
            align: HorizontalAlignment.Left
        );
        titleLabel.AddThemeColorOverride("font_color", LauncherComponentTheme.TextSecondary);
        rows.AddChild(titleLabel);

        var valueLabel = new StyledLabel(
            value,
            scale,
            fontSize: compact ? 12 : 13,
            align: HorizontalAlignment.Left
        )
        {
            Name = valueName,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        valueLabel.AddThemeColorOverride("font_color", LauncherComponentTheme.TextPrimary);
        rows.AddChild(valueLabel);
        return valueLabel;
    }

    internal void SetHomeAccountState(string state)
        => _homeAccountState.Text = PresentationText(state, "Checking...");

    internal void SetHomeGameState(string state)
        => _homeGameState.Text = PresentationText(state, "Checking...");

    private static string PresentationText(string value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
