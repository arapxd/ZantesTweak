using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ZantesEngine.Services
{
    public sealed class OneTapOptimizeResult
    {
        public bool Success { get; init; }
        public bool RestorePointCreated { get; init; }
        public string RestoreMessage { get; init; } = string.Empty;
        public string HardwareSummary { get; init; } = string.Empty;
        public string DriverVendorLabel { get; init; } = string.Empty;
        public string NetworkProfileLabel { get; init; } = string.Empty;
        public string RecommendedDnsLabel { get; init; } = string.Empty;
        public int RecommendedMtuPayload { get; init; }
        public int AppliedSuccessCount { get; init; }
        public int AppliedFailCount { get; init; }
        public int UpdatedGpuPreferenceCount { get; init; }
        public int UpdatedProcessPolicyCount { get; init; }
        public IReadOnlyList<SystemTweakResult> Results { get; init; } = Array.Empty<SystemTweakResult>();
        public string Message { get; init; } = string.Empty;
    }

    public static class OneTapOptimizeService
    {
        public static async Task<OneTapOptimizeResult> RunAsync(CancellationToken token)
        {
            if (!SystemTweakEngine.IsAdministrator())
            {
                return new OneTapOptimizeResult
                {
                    Success = false,
                    Message = "Administrator privileges required."
                };
            }

            var fullPlan = SmartOptimizeService.BuildMaxFpsSafePlan();

            var restore = SystemTweakEngine.CreateRestorePoint($"Zantes Tweak Full Optimize {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            if (!restore.Success)
            {
                return new OneTapOptimizeResult
                {
                    Success = false,
                    RestorePointCreated = false,
                    RestoreMessage = restore.Message,
                    HardwareSummary = fullPlan.HardwareSummary,
                    DriverVendorLabel = "Skipped in FPS-safe mode",
                    NetworkProfileLabel = "Skipped in FPS-safe mode",
                    RecommendedDnsLabel = "-",
                    RecommendedMtuPayload = 0,
                    UpdatedGpuPreferenceCount = 0,
                    UpdatedProcessPolicyCount = 0,
                    Message = $"Restore point could not be created. No optimizations were applied. {restore.Message}"
                };
            }

            var orderedTweaks = fullPlan.TweakKeys
                .Select(SystemTweakCatalog.Get)
                .Where(t => t != null)
                .Cast<SystemTweakDefinition>()
                .GroupBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(t => GetCategoryOrder(t.Category))
                .ThenByDescending(t => t.Recommended)
                .ThenBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var results = await SystemTweakEngine.ApplyAsync(orderedTweaks, token);
            int ok = results.Count(r => r.Success);
            int fail = results.Count - ok;
            var gpuResult = GpuAutomationService.ApplyHighPerformanceForKnownGames();
            var processPolicyResult = GameProcessPolicyService.ApplyPerformancePolicyForKnownGames();

            return new OneTapOptimizeResult
            {
                Success = fail == 0,
                RestorePointCreated = true,
                RestoreMessage = restore.Message,
                HardwareSummary = fullPlan.HardwareSummary,
                DriverVendorLabel = "Skipped in FPS-safe mode",
                NetworkProfileLabel = "Skipped in FPS-safe mode",
                RecommendedDnsLabel = "-",
                RecommendedMtuPayload = 0,
                AppliedSuccessCount = ok,
                AppliedFailCount = fail,
                UpdatedGpuPreferenceCount = gpuResult.UpdatedEntryCount,
                UpdatedProcessPolicyCount = processPolicyResult.UpdatedProcessCount,
                Results = results,
                Message = $"Restore point created. Applied {orderedTweaks.Length} evidence-backed FPS optimizations. GPU preference: {gpuResult.UpdatedEntryCount}, Process policy: {processPolicyResult.UpdatedProcessCount}."
            };
        }

        private static int GetCategoryOrder(string category)
            => category switch
            {
                "System" => 0,
                "Gaming" => 1,
                "Network" => 2,
                "Services" => 3,
                "Privacy" => 4,
                "Maintenance" => 5,
                _ => 10
            };
    }
}
