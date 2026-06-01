using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private sealed class ShaderWarmupProgress
    {
        private readonly Label _statusLabel;
        private readonly Label _detailLabel;
        private readonly ProgressBar _progressBar;

        public ShaderWarmupProgress(Label statusLabel, Label detailLabel, ProgressBar progressBar)
        {
            _statusLabel = statusLabel;
            _detailLabel = detailLabel;
            _progressBar = progressBar;
        }

        private void SetStatus(string text) => _statusLabel.Text = text;

        public void SetDetail(string text) => _detailLabel.Text = text;

        public void SetProgress(double progress) => _progressBar.Value = progress;

        public void Complete(int materialCount, long elapsedMilliseconds)
        {
            _progressBar.Value = 100;
            _statusLabel.Text = Message.DoneStatus;
            _detailLabel.Text = Message.Compiled(materialCount, elapsedMilliseconds);
        }

        public void ShowCompiling() => SetStatus(Message.CompilingStatus);

        public void ShowScanning() => SetStatus(Message.ScanningStatus);
    }
}
