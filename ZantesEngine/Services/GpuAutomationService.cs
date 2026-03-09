using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace ZantesEngine.Services
{
    public sealed class GpuAutomationResult
    {
        public bool Executed { get; init; }
        public int UpdatedEntryCount { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    public static class GpuAutomationService
    {
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

        public static GpuAutomationResult ApplyHighPerformanceForPreset(string presetId)
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string p in GetRunningGameExePaths(presetId))
                paths.Add(p);

            foreach (string p in GetKnownPathCandidates(presetId))
            {
                if (File.Exists(p))
                    paths.Add(p);
            }

            int updated = ApplyHighPerformancePreference(paths);
            return new GpuAutomationResult
            {
                Executed = true,
                UpdatedEntryCount = updated,
                Message = updated == 0 ? "No game EXE found" : "Applied"
            };
        }

        public static GpuAutomationResult ApplyHighPerformanceForKnownGames()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string presetId in PresetProcessHints.Keys)
            {
                foreach (string path in GetRunningGameExePaths(presetId))
                    paths.Add(path);

                foreach (string path in GetKnownPathCandidates(presetId))
                {
                    if (File.Exists(path))
                        paths.Add(path);
                }

                if (GameUpdateWatcherService.TryResolveExecutablePath(presetId, out string resolvedPath) &&
                    File.Exists(resolvedPath))
                {
                    paths.Add(resolvedPath);
                }
            }

            int updated = ApplyHighPerformancePreference(paths);
            return new GpuAutomationResult
            {
                Executed = true,
                UpdatedEntryCount = updated,
                Message = updated == 0 ? "No known game EXE found" : "Applied"
            };
        }

        private static IEnumerable<string> GetRunningGameExePaths(string presetId)
        {
            if (!PresetProcessHints.TryGetValue(presetId, out var hints))
                yield break;

            foreach (var process in Process.GetProcesses())
            {
                string pname;
                try { pname = process.ProcessName; }
                catch { continue; }

                if (!hints.Any(h => pname.Contains(h, StringComparison.OrdinalIgnoreCase)))
                    continue;

                string? path = TryGetMainModulePath(process);
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    yield return path;
            }
        }

        private static IEnumerable<string> GetKnownPathCandidates(string presetId)
        {
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (presetId.Equals("fivem", StringComparison.OrdinalIgnoreCase))
            {
                yield return Path.Combine(local, "FiveM", "FiveM.exe");
                yield return Path.Combine(local, "FiveM", "FiveM.app", "FiveM.exe");
                yield return Path.Combine(pf, "Rockstar Games", "Grand Theft Auto V", "GTA5.exe");
                yield return Path.Combine(pf86, "Steam", "steamapps", "common", "Grand Theft Auto V", "GTA5.exe");
            }
            else if (presetId.Equals("valorant", StringComparison.OrdinalIgnoreCase))
            {
                yield return Path.Combine(pf, "Riot Games", "VALORANT", "live", "ShooterGame", "Binaries", "Win64", "VALORANT-Win64-Shipping.exe");
            }
            else if (presetId.Equals("cs2", StringComparison.OrdinalIgnoreCase))
            {
                yield return Path.Combine(pf86, "Steam", "steamapps", "common", "Counter-Strike Global Offensive", "game", "bin", "win64", "cs2.exe");
            }
            else if (presetId.Equals("hoi4", StringComparison.OrdinalIgnoreCase))
            {
                yield return Path.Combine(pf86, "Steam", "steamapps", "common", "Hearts of Iron IV", "hoi4.exe");
                yield return Path.Combine(pf, "Steam", "steamapps", "common", "Hearts of Iron IV", "hoi4.exe");
            }
            else if (presetId.Equals("fortnite", StringComparison.OrdinalIgnoreCase))
            {
                yield return Path.Combine(pf, "Epic Games", "Fortnite", "FortniteGame", "Binaries", "Win64", "FortniteClient-Win64-Shipping.exe");
            }
            else if (presetId.Equals("lol", StringComparison.OrdinalIgnoreCase))
            {
                yield return Path.Combine(pf, "Riot Games", "League of Legends", "Game", "League of Legends.exe");
                yield return Path.Combine(pf86, "Riot Games", "League of Legends", "Game", "League of Legends.exe");
            }
            else if (presetId.Equals("apex", StringComparison.OrdinalIgnoreCase))
            {
                yield return Path.Combine(pf86, "Steam", "steamapps", "common", "Apex Legends", "r5apex.exe");
                yield return Path.Combine(pf, "EA Games", "Apex", "Apex", "r5apex.exe");
            }
            else if (presetId.Equals("pubg", StringComparison.OrdinalIgnoreCase))
            {
                yield return Path.Combine(pf86, "Steam", "steamapps", "common", "PUBG", "TslGame", "Binaries", "Win64", "TslGame.exe");
            }
            else if (presetId.Equals("r6", StringComparison.OrdinalIgnoreCase))
            {
                yield return Path.Combine(pf86, "Steam", "steamapps", "common", "Tom Clancy's Rainbow Six Siege", "RainbowSix.exe");
                yield return Path.Combine(pf86, "Steam", "steamapps", "common", "Tom Clancy's Rainbow Six Siege", "RainbowSix_Vulkan.exe");
                yield return Path.Combine(pf86, "Ubisoft", "Ubisoft Game Launcher", "games", "Tom Clancy's Rainbow Six Siege", "RainbowSix.exe");
                yield return Path.Combine(pf86, "Ubisoft", "Ubisoft Game Launcher", "games", "Tom Clancy's Rainbow Six Siege", "RainbowSix_Vulkan.exe");
            }
            else if (presetId.Equals("ow2", StringComparison.OrdinalIgnoreCase))
            {
                yield return Path.Combine(pf86, "Overwatch", "_retail_", "Overwatch.exe");
                yield return Path.Combine(pf86, "Battle.net", "Games", "Overwatch", "_retail_", "Overwatch.exe");
            }
        }

        private static string? TryGetMainModulePath(Process process)
        {
            try
            {
                return process.MainModule?.FileName;
            }
            catch
            {
                return null;
            }
        }

        private static int ApplyHighPerformancePreference(IEnumerable<string> exePaths)
        {
            const string keyPath = @"Software\Microsoft\DirectX\UserGpuPreferences";
            int count = 0;

            try
            {
                using RegistryKey? key = Registry.CurrentUser.CreateSubKey(keyPath, true);
                if (key == null)
                    return 0;

                foreach (string exePath in exePaths)
                {
                    if (string.IsNullOrWhiteSpace(exePath))
                        continue;

                    key.SetValue(exePath, "GpuPreference=2;", RegistryValueKind.String);
                    count++;
                }
            }
            catch
            {
                return count;
            }

            return count;
        }
    }
}
