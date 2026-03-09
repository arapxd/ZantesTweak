using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace ZantesEngine.Services
{
    public enum GpuVendor
    {
        Unknown,
        Nvidia,
        Amd,
        Intel
    }

    public sealed class DriverPresetApplyResult
    {
        public bool Success { get; init; }
        public GpuVendor Vendor { get; init; }
        public string VendorLabel { get; init; } = string.Empty;
        public int AppliedSuccessCount { get; init; }
        public int AppliedFailCount { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    public static class DriverPresetService
    {
        private static readonly IReadOnlyDictionary<GpuVendor, string[]> PresetKeys =
            new Dictionary<GpuVendor, string[]>
            {
                [GpuVendor.Nvidia] = new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr"
                },
                [GpuVendor.Amd] = new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr"
                },
                [GpuVendor.Intel] = new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr"
                },
                [GpuVendor.Unknown] = new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr"
                }
            };

        public static (GpuVendor Vendor, string Label) DetectPrimaryVendor()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
                string? best = searcher.Get()
                    .Cast<ManagementObject>()
                    .Select(mo => mo["Name"]?.ToString())
                    .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));

                if (string.IsNullOrWhiteSpace(best))
                    return (GpuVendor.Unknown, "Unknown GPU");

                if (best.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                    return (GpuVendor.Nvidia, best);
                if (best.Contains("AMD", StringComparison.OrdinalIgnoreCase) || best.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                    return (GpuVendor.Amd, best);
                if (best.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                    return (GpuVendor.Intel, best);

                return (GpuVendor.Unknown, best);
            }
            catch
            {
                return (GpuVendor.Unknown, "Unknown GPU");
            }
        }

        public static IReadOnlyList<string> GetPresetKeys(GpuVendor vendor)
            => PresetKeys.TryGetValue(vendor, out var keys) ? keys : PresetKeys[GpuVendor.Unknown];

        public static async Task<DriverPresetApplyResult> ApplyBestPresetAsync(bool createRestorePoint, CancellationToken token)
        {
            if (!SystemTweakEngine.IsAdministrator())
            {
                return new DriverPresetApplyResult
                {
                    Success = false,
                    Vendor = GpuVendor.Unknown,
                    VendorLabel = "Unknown GPU",
                    Message = "Administrator privileges required."
                };
            }

            var detect = DetectPrimaryVendor();
            return await ApplyPresetAsync(detect.Vendor, createRestorePoint, token, detect.Label);
        }

        public static async Task<DriverPresetApplyResult> ApplyPresetAsync(GpuVendor vendor, bool createRestorePoint, CancellationToken token, string? vendorLabelOverride = null)
        {
            if (!SystemTweakEngine.IsAdministrator())
            {
                return new DriverPresetApplyResult
                {
                    Success = false,
                    Vendor = vendor,
                    VendorLabel = vendorLabelOverride ?? vendor.ToString(),
                    Message = "Administrator privileges required."
                };
            }

            string vendorLabel = vendorLabelOverride ?? vendor switch
            {
                GpuVendor.Nvidia => "NVIDIA",
                GpuVendor.Amd => "AMD",
                GpuVendor.Intel => "Intel",
                _ => "Unknown GPU"
            };

            var tweaks = GetPresetKeys(vendor)
                .Select(SystemTweakCatalog.Get)
                .Where(t => t != null)
                .Cast<SystemTweakDefinition>()
                .ToArray();

            if (tweaks.Length == 0)
            {
                return new DriverPresetApplyResult
                {
                    Success = false,
                    Vendor = vendor,
                    VendorLabel = vendorLabel,
                    Message = "No preset tweaks available."
                };
            }

            if (createRestorePoint)
            {
                var restore = SystemTweakEngine.CreateRestorePoint($"Zantes Tweak Driver Preset {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                if (!restore.Success)
                {
                    return new DriverPresetApplyResult
                    {
                        Success = false,
                        Vendor = vendor,
                        VendorLabel = vendorLabel,
                        Message = restore.Message
                    };
                }
            }

            var results = await SystemTweakEngine.ApplyAsync(tweaks, token);
            int ok = results.Count(r => r.Success);
            int fail = results.Count - ok;

            return new DriverPresetApplyResult
            {
                Success = fail == 0,
                Vendor = vendor,
                VendorLabel = vendorLabel,
                AppliedSuccessCount = ok,
                AppliedFailCount = fail,
                Message = $"Success: {ok}, Failed: {fail}"
            };
        }
    }
}
