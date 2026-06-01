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

        private void SetStatus(string text) => _statusLabel.Text = text;

        private void SetDetail(string text) => _detailLabel.Text = text;

        private void SetProgress(double progress) => _progressBar.Value = progress;

        private void Complete(int materialCount, long elapsedMilliseconds)
        {
            _progressBar.Value = 100;
            _statusLabel.Text = Message.DoneStatus;
            _detailLabel.Text = Message.Compiled(materialCount, elapsedMilliseconds);
        }

        private void ShowCompiling() => SetStatus(Message.CompilingStatus);

        private void ShowScanning() => SetStatus(Message.ScanningStatus);
    }
}
