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
        public static MaxFpsSafePlan BuildMaxFpsSafePlan()
        {
            var hw = DetectHardware();
            var keys = SystemTweakCatalog.All.Values
                .Where(t => !IsBsodRiskKey(t.Key))
                .Where(t => ShouldApplyForHardware(t.Key, hw))
                .OrderBy(t => GetCategoryOrder(t.Category))
                .ThenByDescending(t => t.Recommended)
                .ThenBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
                .Select(t => t.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            string reason = hw.IsLaptop
                ? "WinUtil + Cortex inspired deep profile selected for laptop (BSOD-risk set excluded)."
                : $"WinUtil + Cortex inspired deep profile selected for desktop ({hw.CpuCores} threads / {hw.RamGb:F0} GB RAM, BSOD-risk set excluded).";

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
            var allowedTweaks = BuildAllowedSmartOptimizeTweaks(hw);
            var powerKeys = GetKeysByCategory(allowedTweaks, "System").ToList();

            string powerReasonKey;
            IReadOnlyList<object>? powerReasonArgs = null;
            if (hw.IsLaptop)
            {
                powerReasonKey = "quick.reason.power.laptop";
            }
            else
            {
                powerReasonKey = "quick.reason.power.desktop";
                powerReasonArgs = new object[] { hw.CpuCores, Math.Round(hw.RamGb, 0) };
            }

            modules.Add(new SmartModulePlan
            {
                Id = "power",
                NameKey = "quick.card.power",
                DescriptionKey = "quick.card.power.desc",
                ReasonKey = powerReasonKey,
                ReasonArgs = powerReasonArgs,
                TweakKeys = powerKeys
            });

            var gamingKeys = GetKeysByCategory(allowedTweaks, "Gaming");

            modules.Add(new SmartModulePlan
            {
                Id = "gaming",
                NameKey = "quick.card.gaming",
                DescriptionKey = "quick.card.gaming.desc",
                ReasonKey = "quick.reason.gaming",
                TweakKeys = gamingKeys
            });

            var networkKeys = GetKeysByCategory(allowedTweaks, "Network");

            modules.Add(new SmartModulePlan
            {
                Id = "network",
                NameKey = "quick.card.network",
                DescriptionKey = "quick.card.network.desc",
                ReasonKey = "quick.reason.network",
                TweakKeys = networkKeys
            });

            var gpuKeys = allowedTweaks
                .Where(t => IsGpuFocusedKey(t.Key))
                .Select(t => t.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string gpuReasonKey = "quick.reason.gpu.unknown";
            IReadOnlyList<object>? gpuReasonArgs = new object[] { hw.GpuName };
            if (hw.GpuVendor == GpuVendor.Nvidia)
            {
                gpuReasonKey = "quick.reason.gpu.nvidia";
            }
            else if (hw.GpuVendor == GpuVendor.Amd)
            {
                gpuReasonKey = "quick.reason.gpu.amd";
            }
            else if (hw.GpuVendor == GpuVendor.Intel)
            {
                gpuReasonKey = "quick.reason.gpu.intel";
            }

            var serviceKeys = allowedTweaks
                .Where(t => t.Category is "Privacy" or "Services")
                .Select(t => t.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            modules.Add(new SmartModulePlan
            {
                Id = "gpu",
                NameKey = "quick.card.gpu",
                DescriptionKey = "quick.card.gpu.desc",
                ReasonKey = gpuReasonKey,
                ReasonArgs = gpuReasonArgs,
                TweakKeys = gpuKeys
            });

            modules.Add(new SmartModulePlan
            {
                Id = "services",
                NameKey = "quick.card.services",
                DescriptionKey = "quick.card.services.desc",
                ReasonKey = "quick.reason.services",
                TweakKeys = serviceKeys
            });

            var maintenanceKeys = GetKeysByCategory(allowedTweaks, "Maintenance");

            modules.Add(new SmartModulePlan
            {
                Id = "maintenance",
                NameKey = "quick.card.maintenance",
                DescriptionKey = "quick.card.maintenance.desc",
                ReasonKey = hw.PrimaryDiskIsSsd ? "quick.reason.maintenance.ssd" : "quick.reason.maintenance.hdd",
                TweakKeys = maintenanceKeys
            });

            return new SmartOptimizePlan
            {
                HardwareSummary = $"{hw.CpuName} | {hw.RamGb:F1} GB RAM | {hw.GpuName}",
                Modules = modules
            };
        }

        public static IReadOnlyList<SystemTweakDefinition> BuildDashboardApplyTweaks(IEnumerable<string> selectionIds)
        {
            var hw = DetectHardware();
            var allowedTweaks = BuildAllowedSmartOptimizeTweaks(hw);
            var selected = new HashSet<string>(selectionIds, StringComparer.OrdinalIgnoreCase);
            var collected = new List<SystemTweakDefinition>();

            foreach (string id in selected)
            {
                switch (id)
                {
                    case "kernel":
                        collected.AddRange(allowedTweaks.Where(t => t.Category.Equals("System", StringComparison.OrdinalIgnoreCase)));
                        break;
                    case "tcp":
                        collected.AddRange(allowedTweaks.Where(t => t.Category.Equals("Network", StringComparison.OrdinalIgnoreCase)));
                        break;
                    case "mmcss":
                        collected.AddRange(allowedTweaks.Where(t => t.Category.Equals("Gaming", StringComparison.OrdinalIgnoreCase)));
                        break;
                    case "ram":
                        collected.AddRange(allowedTweaks.Where(t => t.Key is
                            "disable_memory_compression" or
                            "visualfx_performance" or
                            "clean_temp" or
                            "clear_directx_shader_cache" or
                            "clean_thumbnail_cache" or
                            "clean_windows_update_cache"));
                        break;
                    case "driver":
                        collected.AddRange(allowedTweaks.Where(t => IsGpuFocusedKey(t.Key)));
                        break;
                    case "debloat":
                        collected.AddRange(allowedTweaks.Where(t => t.Category is "Privacy" or "Services"));
                        break;
                }
            }

            return collected
                .GroupBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(t => GetCategoryOrder(t.Category))
                .ThenByDescending(t => t.Recommended)
                .ThenBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static SystemTweakDefinition[] BuildAllowedSmartOptimizeTweaks(
            (string CpuName, int CpuCores, double RamGb, string GpuName, GpuVendor GpuVendor, bool IsLaptop, bool PrimaryDiskIsSsd) hw)
            => SystemTweakCatalog.All.Values
                .Where(t => !IsBsodRiskKey(t.Key))
                .Where(t => !IsSmartOptimizeExcludedKey(t.Key))
                .Where(t => ShouldApplyForHardware(t.Key, hw))
                .OrderBy(t => GetCategoryOrder(t.Category))
                .ThenByDescending(t => t.Recommended)
                .ThenBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        private static IReadOnlyList<string> GetKeysByCategory(IEnumerable<SystemTweakDefinition> tweaks, params string[] categories)
        {
            var categorySet = new HashSet<string>(categories, StringComparer.OrdinalIgnoreCase);
            return tweaks
                .Where(t => categorySet.Contains(t.Category))
                .Select(t => t.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

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

        private static bool IsBsodRiskKey(string key)
            => key is "disable_cpu_mitigations"
                or "force_msi_all_devices"
                or "gpu_tdr_level_off"
                or "kernel_sehop_off"
                or "bcd_disable_integrity_checks"
                or "bcd_nx_alwaysoff";

        private static bool IsSmartOptimizeExcludedKey(string key)
            => key is "disable_fast_startup"
                or "priority_separation"
                or "winsock_reset"
                or "clear_prefetch"
                || key.StartsWith("fivem_", StringComparison.OrdinalIgnoreCase);

        private static bool IsGpuFocusedKey(string key)
            => key is "disable_mpo"
                or "hw_scheduling"
                or "nvidia_disable_telemetry"
                or "nvidia_clean_shader_cache";

        private static bool ShouldApplyForHardware(
            string key,
            (string CpuName, int CpuCores, double RamGb, string GpuName, GpuVendor GpuVendor, bool IsLaptop, bool PrimaryDiskIsSsd) hw)
        {
            if (hw.IsLaptop && key is "cpu_minimum_state_100" or "disable_hibernate")
                return false;

            if (hw.RamGb < 8 && key is "disable_memory_compression" or "clear_prefetch")
                return false;

            if (hw.GpuVendor == GpuVendor.Intel && key is "nvidia_disable_telemetry" or "nvidia_clean_shader_cache")
                return false;

            if (hw.CpuCores < 8 && key is "tcp_rsc_disabled")
                return false;

            return true;
        }

        private static int GetCategoryOrder(string category)
            => category switch
            {
                "System" => 0,
                "Gaming" => 1,
                "Network" => 2,
                "Privacy" => 3,
                "Services" => 4,
                "Maintenance" => 5,
                _ => 6
            };
    }
}
