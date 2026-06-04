using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private sealed class ShaderWarmupProgress
    {
        private readonly Label _statusLabel;
        private readonly Label _detailLabel;
        private readonly ProgressBar _progressBar;

        private ShaderWarmupProgress(Label statusLabel, Label detailLabel, ProgressBar progressBar)
        {
            _statusLabel = statusLabel;
            _detailLabel = detailLabel;
            _progressBar = progressBar;
        }

        internal static ShaderWarmupProgress ForLabels(
            Label statusLabel,
            Label detailLabel,
            ProgressBar progressBar
        )
            => new(statusLabel, detailLabel, progressBar);

        private void SetStatus(string text) => _statusLabel.Text = text;

        private void SetDetail(string text) => _detailLabel.Text = text;

        private void SetProgress(double progress) => _progressBar.Value = progress;

        internal void Complete(int materialCount, long elapsedMilliseconds)
        {
            SetProgress(100);
            SetStatus(Message.DoneStatus);
            SetDetail(Message.Compiled(materialCount, elapsedMilliseconds));
        }

        internal void ReportCompileProgress(int completed, int total)
        {
            SetProgress(50 + (double)completed / total * 50);
            SetDetail(Message.CompilingProgress(completed, total));
        }

        internal void ReportSceneScanProgress(int index, int total)
        {
            SetDetail(Message.ScanningScenes(index, total));
            if (total > 0)
                SetProgress((double)index / total * 50);
        }

        internal void ShowMaterialsFound(int materialCount)
            => SetDetail(Message.FoundMaterialsDetail(materialCount));

        internal void ShowCompiling() => SetStatus(Message.CompilingStatus);

        internal void ShowScanning() => SetStatus(Message.ScanningStatus);
    }
}
