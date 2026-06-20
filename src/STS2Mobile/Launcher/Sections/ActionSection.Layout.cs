using System;
using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void MoveCompactCloudSafetyCueBeforeCloudActions()
    {
        if (!_compact)
            return;

        _cloudGroup.MoveChild(_cloudSafetyToggle, 0);
        MoveChildAfter(_cloudGroup, _cloudSafetyLabel, _cloudSafetyToggle);
        MoveChildAfter(_cloudGroup, _pushPullRow, _cloudSafetyLabel);
    }

    private void ArrangeCompactCloudGroupPriority()
    {
        if (!_compact)
            return;

        var launchParent = _launchButton.GetParent();
        if (launchParent != _cloudGroup)
        {
            launchParent?.RemoveChild(_launchButton);
            _cloudGroup.AddChild(_launchButton);
        }

        MoveCompactCloudSafetyCueBeforeCloudActions();
        MoveChildAfter(_cloudGroup, _launchButton, _pushPullRow);
        MoveChildAfter(_cloudGroup, _cloudOptionsToggle, _launchButton);
        if (_compactCloudOptionsRow != null)
            MoveChildAfter(_cloudGroup, _compactCloudOptionsRow, _cloudOptionsToggle);
    }

    private void ArrangeCompactReadyStatePriority()
    {
        if (!_compact)
            return;

        var readyPrimaryPath = _launchButton.GetParent() == _cloudGroup
            ? _cloudGroup
            : (Control)_launchButton;
        MoveChild(_readyVersionSummaryPanel, _branchDetailsToggle.GetIndex());
        MoveAfter(_branchDetailsToggle, readyPrimaryPath);
        MoveAfter(_branchDropdown, _branchDetailsToggle);
        MoveAfter(_branchHelpLabel, _branchDropdown);
    }

    private void MoveAfter(Control child, Control previous)
    {
        var previousIndex = previous.GetIndex();
        var targetIndex = child.GetIndex() < previousIndex
            ? previousIndex
            : previousIndex + 1;
        MoveChild(child, Math.Min(targetIndex, GetChildCount() - 1));
    }

    private static void MoveChildAfter(Node parent, Node child, Node previous)
    {
        var previousIndex = previous.GetIndex();
        var targetIndex = child.GetIndex() < previousIndex
            ? previousIndex
            : previousIndex + 1;
        parent.MoveChild(child, Math.Min(targetIndex, parent.GetChildCount() - 1));
    }
}
