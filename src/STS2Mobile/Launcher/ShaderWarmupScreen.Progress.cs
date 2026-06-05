using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private sealed class ShaderWarmupProgress
    {
        private readonly struct ProgressUpdate
        {
            private ProgressUpdate(
                string? status,
                string? detail,
                double? progress
            )
            {
                Status = status;
                Detail = detail;
                Progress = progress;
            }

            private string? Status { get; }
            private string? Detail { get; }
            private double? Progress { get; }

            internal static ProgressUpdate Complete(WarmupCompletion completion)
                => new(
                    Message.DoneStatus,
                    Message.Compiled(completion),
                    100
                );

            internal static ProgressUpdate CompileProgress(int completed, int total)
                => new(
                    status: null,
                    Message.CompilingProgress(completed, total),
                    50 + (double)completed / total * 50
                );

            internal static ProgressUpdate SceneScanProgress(int index, int total)
                => new(
                    status: null,
                    Message.ScanningScenes(index, total),
                    total > 0 ? (double)index / total * 50 : null
                );

            internal static ProgressUpdate Detail(string detail)
                => new(status: null, detail, progress: null);

            internal static ProgressUpdate StatusOnly(string status)
                => new(status, detail: null, progress: null);

            internal void ApplyTo(ShaderWarmupProgress progress)
            {
                if (Progress.HasValue)
                    progress.SetProgress(Progress.Value);

                if (Status != null)
                    progress.SetStatus(Status);

                if (Detail != null)
                    progress.SetDetail(Detail);
            }
        }

        private readonly Label _statusLabel;
        private readonly Label _detailLabel;
        private readonly ProgressBar _progressBar;

        private ShaderWarmupProgress(
            Label statusLabel,
            Label detailLabel,
            ProgressBar progressBar
        )
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

        private void Apply(ProgressUpdate update)
            => update.ApplyTo(this);

        internal void Complete(WarmupCompletion completion)
            => Apply(ProgressUpdate.Complete(completion));

        internal void ReportCompileProgress(int completed, int total)
            => Apply(ProgressUpdate.CompileProgress(completed, total));

        internal void ReportSceneScanProgress(int index, int total)
            => Apply(ProgressUpdate.SceneScanProgress(index, total));

        internal void ShowMaterialsFound(int materialCount)
            => Apply(ProgressUpdate.Detail(Message.FoundMaterialsDetail(materialCount)));

        internal void ShowCompiling()
            => Apply(ProgressUpdate.StatusOnly(Message.CompilingStatus));

        internal void ShowScanning()
            => Apply(ProgressUpdate.StatusOnly(Message.ScanningStatus));
    }
}
