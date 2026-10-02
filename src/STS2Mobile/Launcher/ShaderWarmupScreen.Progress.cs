using Godot;
using System;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;
internal sealed partial class ShaderWarmupScreen
{
    private sealed class ShaderWarmupProgress
    {
        private readonly struct ProgressUpdate
        {
            private ProgressUpdate(string? status, string? detail, double? progress)
            {
                Status = status;
                Detail = detail;
                Progress = progress;
            }

            private string? Status { get; }
            private string? Detail { get; }
            private double? Progress { get; }

            internal static ProgressUpdate Complete(WarmupCompletion completion) => new(Message.DoneStatus, Message.Compiled(completion), 100);
            internal static ProgressUpdate CompileProgress(int completed, int total) => new(status: null, Message.CompilingProgress(completed, total), 50 + (double)completed / total * 50);
            internal static ProgressUpdate SceneScanProgress(int index, int total) => new(status: null, Message.ScanningScenes(index, total), total > 0 ? (double)index / total * 50 : null);
            internal static ProgressUpdate DetailOnly(string detail) => new(status: null, detail, progress: null);
            internal static ProgressUpdate StatusOnly(string status) => new(status, detail: null, progress: null);
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
        private ShaderWarmupProgress(Label statusLabel, Label detailLabel, ProgressBar progressBar)
        {
            _statusLabel = statusLabel;
            _detailLabel = detailLabel;
            _progressBar = progressBar;
        }

        internal static ShaderWarmupProgress ForLabels(Label statusLabel, Label detailLabel, ProgressBar progressBar) => new(statusLabel, detailLabel, progressBar);
        private void SetStatus(string text) => _statusLabel.Text = text;
        private void SetDetail(string text) => _detailLabel.Text = text;
        private void SetProgress(double progress) => _progressBar.Value = progress;
        private void Apply(ProgressUpdate update) => update.ApplyTo(this);
        internal void Complete(WarmupCompletion completion) => Apply(ProgressUpdate.Complete(completion));
        internal void ReportCompileProgress(int completed, int total) => Apply(ProgressUpdate.CompileProgress(completed, total));
        internal void ReportSceneScanProgress(int index, int total) => Apply(ProgressUpdate.SceneScanProgress(index, total));
        internal void ShowMaterialsFound(int materialCount) => Apply(ProgressUpdate.DetailOnly(Message.FoundMaterialsDetail(materialCount)));
        internal void ShowCompiling() => Apply(ProgressUpdate.StatusOnly(Message.CompilingStatus));
        internal void ShowScanning() => Apply(ProgressUpdate.StatusOnly(Message.ScanningStatus));
    }

    private static partial class Message
    {
        internal const string ScanningStatus = "Scanning for shaders...";
        internal const string CompilingStatus = "Compiling shaders...";
        internal const string DoneStatus = "Done!";
        internal const string InitialDetail = "Enumerating resources...";
        internal const string ScreenInitialized = "[ShaderWarmup] Screen initialized";
        internal static string Collected(int materialCount) => $"[ShaderWarmup] Collected {materialCount} materials to warm";
        internal static string Completed(WarmupCompletion completion) => $"[ShaderWarmup] Completed: {completion.MaterialCount} materials in {completion.ElapsedMilliseconds}ms";
        internal static string CompletedPartial(WarmupPartialCompletion completion) => $"[ShaderWarmup] Time-budgeted: rendered {completion.RenderedMaterialCount}/{completion.TotalMaterialCount} materials in {completion.ElapsedMilliseconds}ms";
        internal static string ScreenBuildFailed(Exception ex) => $"[ShaderWarmup] BuildUI failed: {ex}";
        internal static string RunFailed(Exception ex) => $"[ShaderWarmup] Failed: {ex}";
        internal static string StatusMarkerWriteFailed(Exception ex) => $"[ShaderWarmup] Failed to write status marker: {ex.Message}";
        internal static string WatchdogWarning(int seconds) => $"[ShaderWarmup] Still active after {seconds}s; wrote watchdog status marker";
        internal static string TimeBudgetReached(int seconds) => $"[ShaderWarmup] Time budget reached after {seconds}s; continuing startup with partial warmup";
        internal static string Compiled(WarmupCompletion completion) => $"Compiled {completion.MaterialCount} shaders in {completion.ElapsedMilliseconds}ms";
        internal static string FoundMaterialsDetail(int materialCount) => $"Found {materialCount} materials...";
        internal static string ScanningScenes(int index, int total) => $"Scanning scenes... {index} / {total}";
        internal static string CompilingProgress(int completed, int total) => $"Compiling {completed} / {total}";
    }

    private const float ReferenceShortEdge = 720f;
    private const float MinimumScale = 0.85f;
    private const float AndroidMinimumScale = 1.06f;
    private const float MaximumScale = 1.6f;
    private const float MinimumPanelWidth = 420f;
    private const float AndroidMinimumPanelWidth = 320f;
    private const float MaximumPanelWidth = 980f;
    private const float PanelHeightRatio = 0.72f;
    private const float AndroidPanelWidthRatio = 0.94f;
    private void BuildUI(Vector2 vpSize)
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        var androidCompact = OperatingSystem.IsAndroid();
        var scale = CalculateAdaptiveScale(vpSize, androidCompact);
        var bg = new ScreenBackground();
        AddChild(bg);
        var panel = new StyledPanel(scale, widthRatio: CalculatePanelWidthRatio(vpSize, androidCompact), compact: androidCompact);
        panel.UpdateSizeFromViewport(CalculateWarmupPanelSize(vpSize, androidCompact), PanelHeightRatio);
        AddChild(panel);
        _statusLabel = new StyledLabel(Message.CompilingStatus, scale, fontSize: 20);
        _statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        panel.AddContent(_statusLabel);
        _progressBar = new StyledProgressBar(scale, androidCompact);
        _progressBar.MinValue = 0;
        _progressBar.MaxValue = 100;
        _progressBar.Value = 0;
        _progressBar.ShowPercentage = true;
        panel.AddContent(_progressBar);
        _detailLabel = new StyledLabel(Message.InitialDetail, scale, fontSize: 14);
        _detailLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
        _detailLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        panel.AddContent(_detailLabel);
    }

    private static float CalculateAdaptiveScale(Vector2 vpSize, bool androidCompact)
    {
        var shortEdge = Math.Max(1f, Math.Min(vpSize.X, vpSize.Y));
        return Math.Clamp(shortEdge / ReferenceShortEdge, androidCompact ? AndroidMinimumScale : MinimumScale, MaximumScale);
    }

    private static float CalculatePanelWidthRatio(Vector2 vpSize, bool androidCompact)
    {
        if (androidCompact)
            return AndroidPanelWidthRatio;
        var aspect = Math.Max(vpSize.X, vpSize.Y) / Math.Max(1f, Math.Min(vpSize.X, vpSize.Y));
        return aspect >= 2.0f ? 0.62f : 0.56f;
    }

    private static Vector2 CalculateWarmupPanelSize(Vector2 vpSize, bool androidCompact)
    {
        var safeMargin = CalculateSafeMargin(vpSize);
        var safeWidth = Math.Max(1f, vpSize.X - safeMargin * 2f);
        var widthRatio = CalculatePanelWidthRatio(vpSize, androidCompact);
        var minimumWidth = androidCompact ? AndroidMinimumPanelWidth : MinimumPanelWidth;
        var maximumWidth = MaximumPanelWidth / widthRatio;
        if (androidCompact)
            maximumWidth = Math.Min(safeWidth, maximumWidth);
        return new Vector2(Math.Clamp(safeWidth, Math.Min(minimumWidth, maximumWidth), maximumWidth), Math.Max(1f, vpSize.Y - safeMargin * 2f));
    }

    private static float CalculateSafeMargin(Vector2 vpSize)
    {
        var shortEdge = Math.Min(vpSize.X, vpSize.Y);
        return Math.Clamp(shortEdge * 0.04f, 16f, 48f);
    }
}
