using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace ZantesEngine.Services
{
    public sealed class SmartModulePlan
    {
        public required string Id { get; init; }
        public required string NameKey { get; init; }
        public required string DescriptionKey { get; init; }
        public required string ReasonKey { get; init; }
        public IReadOnlyList<object>? ReasonArgs { get; init; }
        public bool DefaultEnabled { get; init; } = true;
        public required IReadOnlyList<string> TweakKeys { get; init; }
    }

    public sealed class SmartOptimizePlan
    {
        public required string HardwareSummary { get; init; }
        public required IReadOnlyList<SmartModulePlan> Modules { get; init; }
    }

    public sealed class MaxFpsSafePlan
    {
        public required string HardwareSummary { get; init; }
        public required string Reason { get; init; }
        public required IReadOnlyList<string> TweakKeys { get; init; }
    }

    public static class SmartOptimizeService
    {
        private static readonly string[] EvidenceBackedFpsKeys =
        {
            "enable_game_mode",
            "disable_game_dvr"
        };

        public static MaxFpsSafePlan BuildMaxFpsSafePlan()
        {
            var hw = DetectHardware();
            var keys = BuildEvidenceBackedFpsTweaks()
                .Select(t => t.Key)
                .ToArray();

            string reason = "Evidence-backed FPS baseline selected. Mixed-result power, network, scheduler, service, and cleanup tweaks were excluded.";

            return new MaxFpsSafePlan
            {
                HardwareSummary = $"{hw.CpuName} | {hw.RamGb:F1} GB RAM | {hw.GpuName}",
                Reason = reason,
                TweakKeys = keys
            };
        }

        public static SmartOptimizePlan BuildPlan()
        {
            var hw = DetectHardware();
            var modules = new List<SmartModulePlan>();
            var gamingKeys = BuildEvidenceBackedFpsTweaks()
                .Select(t => t.Key)
                .ToArray();

            if (gamingKeys.Length > 0)
            {
                modules.Add(new SmartModulePlan
                {
                    Id = "gaming",
                    NameKey = "quick.card.gaming",
                    DescriptionKey = "quick.card.gaming.desc",
                    ReasonKey = "quick.reason.gaming",
                    TweakKeys = gamingKeys
                });
            }

            return new SmartOptimizePlan
            {
                HardwareSummary = $"{hw.CpuName} | {hw.RamGb:F1} GB RAM | {hw.GpuName}",
                Modules = modules
            };
        }

        public static IReadOnlyList<SystemTweakDefinition> BuildDashboardApplyTweaks(IEnumerable<string> selectionIds)
        {
            _ = selectionIds;
            return BuildEvidenceBackedFpsTweaks();
        }

        private static SystemTweakDefinition[] BuildEvidenceBackedFpsTweaks()
            => EvidenceBackedFpsKeys
                .Select(SystemTweakCatalog.Get)
                .Where(t => t != null)
                .Cast<SystemTweakDefinition>()
                .ToArray();

        public static async Task<IReadOnlyList<SystemTweakResult>> ApplyModulesAsync(
            IEnumerable<SmartModulePlan> modules,
            CancellationToken token)
        {
            var selectedKeys = modules
                .SelectMany(m => m.TweakKeys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(SystemTweakCatalog.Get)
                .Where(t => t != null)
                .Cast<SystemTweakDefinition>()
                .ToArray();

            if (selectedKeys.Length == 0)
                return Array.Empty<SystemTweakResult>();

            return await SystemTweakEngine.ApplyAsync(selectedKeys, token);
        }

        private static (string CpuName, int CpuCores, double RamGb, string GpuName, GpuVendor GpuVendor, bool IsLaptop, bool PrimaryDiskIsSsd) DetectHardware()
        {
            string cpuName = "Unknown CPU";
            int cores = Environment.ProcessorCount;
            double ramGb = 8;
            string gpuName = "Unknown GPU";
            GpuVendor gpuVendor = GpuVendor.Unknown;
            bool isLaptop = false;
            bool primaryDiskIsSsd = true;

            try
            {
                using var cpuSearch = new ManagementObjectSearcher("SELECT Name,NumberOfLogicalProcessors FROM Win32_Processor");
                foreach (ManagementObject obj in cpuSearch.Get())
                {
                    cpuName = obj["Name"]?.ToString()?.Trim() ?? cpuName;
                    if (int.TryParse(obj["NumberOfLogicalProcessors"]?.ToString(), out int parsed))
                        cores = parsed;
                    break;
                }
            }
            catch { }

            try
            {
                using var memSearch = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in memSearch.Get())
                {
                    if (ulong.TryParse(obj["TotalPhysicalMemory"]?.ToString(), out var total))
                        ramGb = total / (1024d * 1024d * 1024d);
                    break;
                }
            }
            catch { }

            try
            {
                using var gpuSearch = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
                foreach (ManagementObject obj in gpuSearch.Get())
                {
                    gpuName = obj["Name"]?.ToString()?.Trim() ?? gpuName;
                    if (gpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                        gpuVendor = GpuVendor.Nvidia;
                    else if (gpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase) || gpuName.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                        gpuVendor = GpuVendor.Amd;
                    else if (gpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                        gpuVendor = GpuVendor.Intel;
                    break;
                }
            }
            catch { }

            try
            {
                using var batterySearch = new ManagementObjectSearcher("SELECT BatteryStatus FROM Win32_Battery");
                isLaptop = batterySearch.Get().Count > 0;
            }
            catch { }

            try
            {
                using var diskSearch = new ManagementObjectSearcher("SELECT MediaType,Model FROM Win32_DiskDrive");
                foreach (ManagementObject obj in diskSearch.Get())
                {
                    string raw = $"{obj["MediaType"]} {obj["Model"]}";
                    if (raw.Contains("NVMe", StringComparison.OrdinalIgnoreCase) ||
                        raw.Contains("SSD", StringComparison.OrdinalIgnoreCase) ||
                        raw.Contains("Solid State", StringComparison.OrdinalIgnoreCase))
                    {
                        primaryDiskIsSsd = true;
                        break;
                    }

                    if (raw.Contains("HDD", StringComparison.OrdinalIgnoreCase) ||
                        raw.Contains("Hard Disk", StringComparison.OrdinalIgnoreCase))
                    {
                        primaryDiskIsSsd = false;
                        break;
                    }
                }
            }
            catch { }

            return (cpuName, cores, ramGb, gpuName, gpuVendor, isLaptop, primaryDiskIsSsd);
        }
    }
}
