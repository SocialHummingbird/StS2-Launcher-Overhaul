using System;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private readonly struct ShaderWarmupRenderPlan
    {
        private const int DefaultBatchSize = 8;
        private const int AndroidBatchSize = 1;
        private const int AndroidLargeMaterialThreshold = 512;
        private const int AndroidLargeMaterialLimit = 128;
        private const int PreviousRenderCrashLimit = 64;

        private ShaderWarmupRenderPlan(
            string name,
            string reason,
            int totalMaterialCount,
            int targetMaterialCount,
            int batchSize,
            bool previousRenderCrashSuspected
        )
        {
            Name = name;
            Reason = reason;
            TotalMaterialCount = totalMaterialCount;
            TargetMaterialCount = targetMaterialCount;
            BatchSize = batchSize;
            PreviousRenderCrashSuspected = previousRenderCrashSuspected;
        }

        internal string Name { get; }
        internal string Reason { get; }
        internal int TotalMaterialCount { get; }
        internal int TargetMaterialCount { get; }
        internal int BatchSize { get; }
        internal bool PreviousRenderCrashSuspected { get; }

        internal bool IsPartial
            => TargetMaterialCount < TotalMaterialCount;

        internal static ShaderWarmupRenderPlan ForMaterialCount(
            int totalMaterialCount,
            bool previousRenderCrashSuspected
        )
        {
            int normalizedTotal = Math.Max(0, totalMaterialCount);

            if (OperatingSystem.IsAndroid() && previousRenderCrashSuspected)
            {
                return Create(
                    "android-render-crash-recovery",
                    "Previous shader warmup marker stopped during rendering; using a minimal compatibility warmup to avoid a crash loop",
                    normalizedTotal,
                    PreviousRenderCrashLimit,
                    AndroidBatchSize,
                    previousRenderCrashSuspected
                );
            }

            if (OperatingSystem.IsAndroid() && normalizedTotal > AndroidLargeMaterialThreshold)
            {
                return Create(
                    "android-bounded-large-shader-set",
                    "Android shader set is large; using bounded compatibility warmup so startup is not blocked by driver/resource pressure",
                    normalizedTotal,
                    AndroidLargeMaterialLimit,
                    AndroidBatchSize,
                    previousRenderCrashSuspected
                );
            }

            return Create(
                "full",
                "Full shader warmup selected",
                normalizedTotal,
                normalizedTotal,
                DefaultBatchSize,
                previousRenderCrashSuspected
            );
        }

        internal string[] ToEvidenceLines()
            =>
            [
                $"Render plan: {Name}",
                $"Render plan reason: {Reason}",
                $"Render target materials: {TargetMaterialCount}/{TotalMaterialCount}",
                $"Render batch size: {BatchSize}",
                $"Previous render crash suspected: {PreviousRenderCrashSuspected}",
            ];

        internal string CompletionClassification()
            => IsPartial
                ? $"{Name}; partial compatibility warmup completed; startup continued"
                : "full shader warmup completed";

        private static ShaderWarmupRenderPlan Create(
            string name,
            string reason,
            int totalMaterialCount,
            int requestedTargetMaterialCount,
            int batchSize,
            bool previousRenderCrashSuspected
        )
        {
            int target = Math.Min(
                Math.Max(0, requestedTargetMaterialCount),
                totalMaterialCount
            );
            return new ShaderWarmupRenderPlan(
                name,
                reason,
                totalMaterialCount,
                target,
                Math.Max(1, batchSize),
                previousRenderCrashSuspected
            );
        }

    }
}
