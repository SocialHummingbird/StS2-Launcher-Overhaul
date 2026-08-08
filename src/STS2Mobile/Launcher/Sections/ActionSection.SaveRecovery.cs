#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    internal event Action? SaveRecoveryScanPressed;
    internal event Action? SaveRecoveryCurrentExportPressed;
    internal event Action<string>? SaveRecoveryExportPressed;
    internal event Action<string>? SaveRecoveryRestorePressed;
    internal event Action? SaveRecoveryUndoPressed;
    internal event Action? SaveRecoveryApprovePressed;

    private readonly VBoxContainer _saveRecoveryGroup;
    private readonly Button _saveRecoveryScanButton;
    private readonly Button _saveRecoveryCurrentExportButton;
    private readonly OptionButton _saveRecoveryCandidateDropdown;
    private readonly Button _saveRecoveryExportButton;
    private readonly Button _saveRecoveryRestoreButton;
    private readonly Button _saveRecoveryUndoButton;
    private readonly Button _saveRecoveryApproveButton;
    private readonly Label _saveRecoveryStatusLabel;
    private readonly List<SaveRecoveryCandidatePresentation>
        _saveRecoveryCandidates = new();
    private string _saveRecoveryStatus =
        "No recovery copies scanned. Recovery actions use local files only; Steam is not changed.";
    private bool _saveRecoveryBusy;
    private bool _saveRecoveryCanUndo;
    private bool _saveRecoveryCanApprove;

    internal void SetSaveRecoveryCandidates(
        IReadOnlyList<SaveRecoveryCandidatePresentation>? candidates,
        string selectedCandidateId = ""
    )
    {
        _saveRecoveryCandidates.Clear();
        _saveRecoveryCandidateDropdown.Clear();

        if (candidates is not null)
        {
            foreach (var candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate.Id))
                    continue;

                _saveRecoveryCandidates.Add(candidate);
                _saveRecoveryCandidateDropdown.AddItem(
                    string.IsNullOrWhiteSpace(candidate.Title)
                        ? "Recovery copy"
                        : candidate.Title.Trim()
                );
            }
        }

        if (_saveRecoveryCandidates.Count == 0)
        {
            _saveRecoveryCandidateDropdown.AddItem(
                "No recovery copies found"
            );
            _saveRecoveryCandidateDropdown.Select(0);
            _saveRecoveryStatus =
                "No recovery copies found in known local snapshot and backup locations. Steam was not contacted.";
            RefreshSaveRecoveryControls();
            return;
        }

        var selectedIndex = 0;
        if (!string.IsNullOrWhiteSpace(selectedCandidateId))
        {
            var requestedIndex = _saveRecoveryCandidates.FindIndex(
                candidate => string.Equals(
                    candidate.Id,
                    selectedCandidateId,
                    StringComparison.Ordinal
                )
            );
            if (requestedIndex >= 0)
                selectedIndex = requestedIndex;
        }

        _saveRecoveryCandidateDropdown.Select(selectedIndex);
        _saveRecoveryStatus =
            "Review the selected copy's provenance before exporting or restoring it locally. Steam is not changed by these controls.";
        RefreshSaveRecoveryControls();
    }

    internal void SetSaveRecoveryBusy(bool busy, string status)
    {
        _saveRecoveryBusy = busy;
        if (!string.IsNullOrWhiteSpace(status))
            _saveRecoveryStatus = status.Trim();
        RefreshSaveRecoveryControls();
    }

    internal void SetSaveRecoveryState(
        string status,
        bool canUndo,
        bool canApprove
    )
    {
        if (!string.IsNullOrWhiteSpace(status))
            _saveRecoveryStatus = status.Trim();
        _saveRecoveryCanUndo = canUndo;
        _saveRecoveryCanApprove = canApprove;
        RefreshSaveRecoveryControls();
    }

    private void SelectSaveRecoveryCandidate(long index)
    {
        if (index < 0 || index >= _saveRecoveryCandidates.Count)
            return;

        RefreshSaveRecoveryControls();
    }

    private void RequestSelectedSaveRecoveryExport()
    {
        if (_saveRecoveryBusy || !TrySelectedSaveRecoveryCandidate(out var candidate))
            return;

        SaveRecoveryExportPressed?.Invoke(candidate.Id);
    }

    private void RequestSelectedSaveRecoveryRestore()
    {
        if (
            _saveRecoveryBusy
            || !TrySelectedSaveRecoveryCandidate(out var candidate)
            || !candidate.CanRestore
        )
        {
            return;
        }

        SaveRecoveryRestorePressed?.Invoke(candidate.Id);
    }

    private void RefreshSaveRecoveryControls()
    {
        var hasCandidate = TrySelectedSaveRecoveryCandidate(out var candidate);
        _saveRecoveryScanButton.Disabled = _saveRecoveryBusy;
        _saveRecoveryCurrentExportButton.Disabled = _saveRecoveryBusy;
        _saveRecoveryCandidateDropdown.Disabled =
            _saveRecoveryBusy || !hasCandidate;
        _saveRecoveryExportButton.Disabled =
            _saveRecoveryBusy || !hasCandidate;
        _saveRecoveryRestoreButton.Disabled =
            _saveRecoveryBusy || !hasCandidate || !candidate.CanRestore;

        _saveRecoveryUndoButton.Visible = _saveRecoveryCanUndo;
        _saveRecoveryUndoButton.Disabled =
            _saveRecoveryBusy || !_saveRecoveryCanUndo;
        _saveRecoveryApproveButton.Visible = _saveRecoveryCanApprove;
        _saveRecoveryApproveButton.Disabled =
            _saveRecoveryBusy || !_saveRecoveryCanApprove;

        var detail = hasCandidate ? candidate.Detail?.Trim() : "";
        _saveRecoveryStatusLabel.Text = string.IsNullOrWhiteSpace(detail)
            ? _saveRecoveryStatus
            : $"{detail}\n{_saveRecoveryStatus}";
        _saveRecoveryCandidateDropdown.TooltipText = detail;
        _saveRecoveryCandidateDropdown.AccessibilityDescription =
            string.IsNullOrWhiteSpace(detail)
                ? "Select a local recovery copy and review its provenance before restoring."
                : detail;
    }

    private bool TrySelectedSaveRecoveryCandidate(
        out SaveRecoveryCandidatePresentation candidate
    )
    {
        var selected = _saveRecoveryCandidateDropdown.Selected;
        if (selected >= 0 && selected < _saveRecoveryCandidates.Count)
        {
            candidate = _saveRecoveryCandidates[selected];
            return true;
        }

        candidate = default;
        return false;
    }
}
