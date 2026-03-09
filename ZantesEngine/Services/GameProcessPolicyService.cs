using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace ZantesEngine.Services
{
    public sealed class GameProcessPolicyResult
    {
        public bool Executed { get; init; }
        public int UpdatedProcessCount { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    public static class GameProcessPolicyService
    {
        private const uint ProcessPowerThrottlingCurrentVersion = 1;
        private const uint ProcessPowerThrottlingExecutionSpeed = 0x1;

        private static readonly IReadOnlyDictionary<string, string[]> PresetProcessHints =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["valorant"] = new[] { "valorant", "valorant-win64-shipping" },
                ["cs2"] = new[] { "cs2", "csgo" },
                ["hoi4"] = new[] { "hoi4", "heartsofiron4" },
                ["fivem"] = new[] { "fivem", "fivem_gta", "gta5" },
                ["lol"] = new[] { "league of legends", "leagueclientuxrender" },
                ["fortnite"] = new[] { "fortniteclient-win64-shipping" },
                ["apex"] = new[] { "r5apex" },
                ["pubg"] = new[] { "tslgame" },
                ["r6"] = new[] { "rainbowsix", "rainbowsix_vulkan" },
                ["ow2"] = new[] { "overwatch" }
            };

        public static GameProcessPolicyResult ApplyPerformancePolicyForPreset(string presetId)
            => ApplyPerformancePolicy(new[] { presetId });

        public static GameProcessPolicyResult ApplyPerformancePolicyForKnownGames()
            => ApplyPerformancePolicy(PresetProcessHints.Keys);

        private static GameProcessPolicyResult ApplyPerformancePolicy(IEnumerable<string> presetIds)
        {
            int updated = 0;

            foreach (Process process in GetRunningGameProcesses(presetIds))
            {
                try
                {
                    var state = new ProcessPowerThrottlingState
                    {
                        Version = ProcessPowerThrottlingCurrentVersion,
                        ControlMask = ProcessPowerThrottlingExecutionSpeed,
                        StateMask = 0
                    };

                    if (SetProcessInformation(
                        process.Handle,
                        ProcessInformationClass.ProcessPowerThrottling,
                        ref state,
                        (uint)Marshal.SizeOf<ProcessPowerThrottlingState>()))
                    {
                        updated++;
                    }
                }
                catch
                {
                    // Skip protected or inaccessible processes.
                }
            }

            return new GameProcessPolicyResult
            {
                Executed = true,
                UpdatedProcessCount = updated,
                Message = updated == 0
                    ? "No supported running game process was found for process policy."
                    : $"Execution-speed throttling disabled for {updated} running game process(es)."
            };
        }

        private static IEnumerable<Process> GetRunningGameProcesses(IEnumerable<string> presetIds)
        {
            var hints = presetIds
                .Where(presetId => PresetProcessHints.TryGetValue(presetId, out _))
                .SelectMany(presetId => PresetProcessHints[presetId])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (hints.Length == 0)
                yield break;

            foreach (Process process in Process.GetProcesses())
            {
                string processName;
                try
                {
                    processName = process.ProcessName;
                }
                catch
                {
                    continue;
                }

                if (hints.Any(hint => processName.Contains(hint, StringComparison.OrdinalIgnoreCase)))
                    yield return process;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessInformation(
            IntPtr hProcess,
            ProcessInformationClass processInformationClass,
            ref ProcessPowerThrottlingState processInformation,
            uint processInformationSize);

        private enum ProcessInformationClass
        {
            ProcessMemoryPriority = 0,
            ProcessMemoryExhaustionInfo = 1,
            ProcessAppMemoryInfo = 2,
            ProcessInPrivateInfo = 3,
            ProcessPowerThrottling = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessPowerThrottlingState
        {
            public uint Version;
            public uint ControlMask;
            public uint StateMask;
        }
    }
}
