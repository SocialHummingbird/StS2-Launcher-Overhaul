using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private sealed class ShaderWarmupProgress
    {
        private readonly Label _statusLabel;
        private readonly Label _detailLabel;
        private readonly ProgressBar _progressBar;

        internal ShaderWarmupProgress(Label statusLabel, Label detailLabel, ProgressBar progressBar)
        {
            _statusLabel = statusLabel;
            _detailLabel = detailLabel;
            _progressBar = progressBar;
        }

        internal void SetStatus(string text) => _statusLabel.Text = text;

        internal void SetDetail(string text) => _detailLabel.Text = text;

        internal void SetProgress(double progress) => _progressBar.Value = progress;

        internal void Complete(int materialCount, long elapsedMilliseconds)
        {
            _progressBar.Value = 100;
            _statusLabel.Text = Message.DoneStatus;
            _detailLabel.Text = Message.Compiled(materialCount, elapsedMilliseconds);
        }

        internal void ShowCompiling() => SetStatus(Message.CompilingStatus);

        internal void ShowScanning() => SetStatus(Message.ScanningStatus);
    }
}
