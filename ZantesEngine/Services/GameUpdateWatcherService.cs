using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ZantesEngine.Services
{
    public sealed class GameUpdateWatchResult
    {
        public string Code { get; init; } = string.Empty;
        public bool IsTracked { get; init; }
        public bool HasChanged { get; init; }
        public string ExecutablePath { get; init; } = string.Empty;
        public DateTime? TrackedLastWriteUtc { get; init; }
        public DateTime? CurrentLastWriteUtc { get; init; }
        public string ErrorDetail { get; init; } = string.Empty;
    }

    public static class GameUpdateWatcherService
    {
        private sealed class SnapshotStore
        {
            public Dictionary<string, SnapshotEntry> Games { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class SnapshotEntry
        {
            public string ExecutablePath { get; set; } = string.Empty;
            public long FileSize { get; set; }
            public DateTime LastWriteUtc { get; set; }
        }

        private static readonly string StorePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZantesEngine", "game_update_watch.json");

        private static readonly IReadOnlyDictionary<string, string[]> ProcessHints =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["valorant"] = new[] { "valorant", "valorant-win64-shipping" },
                ["cs2"] = new[] { "cs2", "csgo" },
                ["hoi4"] = new[] { "hoi4", "heartsofiron4" },
                ["fivem"] = new[] { "fivem", "gta5" },
                ["lol"] = new[] { "leagueclient", "leagueclientux" },
                ["fortnite"] = new[] { "fortniteclient-win64-shipping" },
                ["apex"] = new[] { "r5apex" },
                ["pubg"] = new[] { "tslgame" },
                ["r6"] = new[] { "rainbowsix", "rainbowsix_vulkan" },
                ["ow2"] = new[] { "overwatch" }
            };

        private static readonly IReadOnlyDictionary<string, string[]> KnownPaths =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["valorant"] = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Riot Games", "VALORANT", "live", "ShooterGame", "Binaries", "Win64", "VALORANT-Win64-Shipping.exe")
                },
                ["cs2"] = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Counter-Strike Global Offensive", "game", "bin", "win64", "cs2.exe")
                },
                ["hoi4"] = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Hearts of Iron IV", "hoi4.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam", "steamapps", "common", "Hearts of Iron IV", "hoi4.exe")
                },
                ["fivem"] = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "FiveM.app", "FiveM.exe")
                },
                ["lol"] = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Riot Games", "League of Legends", "LeagueClient.exe")
                },
                ["fortnite"] = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", "Fortnite", "FortniteGame", "Binaries", "Win64", "FortniteClient-Win64-Shipping.exe")
                },
                ["apex"] = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Apex Legends", "r5apex.exe")
                },
                ["pubg"] = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "PUBG", "TslGame", "Binaries", "Win64", "TslGame.exe")
                },
                ["r6"] = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Ubisoft", "Ubisoft Game Launcher", "games", "Tom Clancy's Rainbow Six Siege", "RainbowSix.exe")
                },
                ["ow2"] = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Overwatch", "_retail_", "Overwatch.exe")
                }
            };

        public static GameUpdateWatchResult RegisterSnapshot(string gameId)
        {
            if (!TryResolveExecutablePath(gameId, out string exePath))
            {
                return new GameUpdateWatchResult
                {
                    Code = "resolve_failed"
                };
            }

            return RegisterSnapshot(gameId, exePath);
        }

        public static GameUpdateWatchResult RegisterSnapshot(string gameId, string executablePath)
        {
            try
            {
                if (!File.Exists(executablePath))
                {
                    return new GameUpdateWatchResult
                    {
                        Code = "exe_not_found",
                        ExecutablePath = executablePath
                    };
                }

                var file = new FileInfo(executablePath);
                var store = LoadStore();
                store.Games[gameId] = new SnapshotEntry
                {
                    ExecutablePath = executablePath,
                    FileSize = file.Length,
                    LastWriteUtc = file.LastWriteTimeUtc
                };
                SaveStore(store);

                return new GameUpdateWatchResult
                {
                    Code = "registered",
                    IsTracked = true,
                    ExecutablePath = executablePath,
                    TrackedLastWriteUtc = file.LastWriteTimeUtc,
                    CurrentLastWriteUtc = file.LastWriteTimeUtc
                };
            }
            catch (Exception ex)
            {
                return new GameUpdateWatchResult
                {
                    Code = "error",
                    ErrorDetail = ex.Message
                };
            }
        }

        public static GameUpdateWatchResult CheckForUpdate(string gameId)
        {
            try
            {
                var store = LoadStore();
                if (!store.Games.TryGetValue(gameId, out var tracked))
                {
                    return new GameUpdateWatchResult
                    {
                        Code = "not_tracked",
                        IsTracked = false
                    };
                }

                string exePath = tracked.ExecutablePath;
                if (!File.Exists(exePath))
                {
                    if (TryResolveExecutablePath(gameId, out var resolved))
                        exePath = resolved;
                    else
                    {
                        return new GameUpdateWatchResult
                        {
                            Code = "exe_not_found",
                            IsTracked = true,
                            HasChanged = true,
                            ExecutablePath = tracked.ExecutablePath,
                            TrackedLastWriteUtc = tracked.LastWriteUtc
                        };
                    }
                }

                var current = new FileInfo(exePath);
                bool changed = current.LastWriteTimeUtc != tracked.LastWriteUtc || current.Length != tracked.FileSize;
                return new GameUpdateWatchResult
                {
                    Code = changed ? "updated" : "up_to_date",
                    IsTracked = true,
                    HasChanged = changed,
                    ExecutablePath = exePath,
                    TrackedLastWriteUtc = tracked.LastWriteUtc,
                    CurrentLastWriteUtc = current.LastWriteTimeUtc
                };
            }
            catch (Exception ex)
            {
                return new GameUpdateWatchResult
                {
                    Code = "error",
                    ErrorDetail = ex.Message
                };
            }
        }

        public static bool TryResolveExecutablePath(string gameId, out string executablePath)
        {
            executablePath = string.Empty;

            foreach (string running in FindRunningExecutables(gameId))
            {
                if (File.Exists(running))
                {
                    executablePath = running;
                    return true;
                }
            }

            if (!KnownPaths.TryGetValue(gameId, out var candidates))
                return false;

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    executablePath = candidate;
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> FindRunningExecutables(string gameId)
        {
            if (!ProcessHints.TryGetValue(gameId, out var hints))
                yield break;

            foreach (var process in Process.GetProcesses())
            {
                string processName;
                try { processName = process.ProcessName; }
                catch { continue; }

                if (!hints.Any(h => processName.Contains(h, StringComparison.OrdinalIgnoreCase)))
                    continue;

                string? path = null;
                try { path = process.MainModule?.FileName; }
                catch { }

                if (!string.IsNullOrWhiteSpace(path))
                    yield return path;
            }
        }

        private static SnapshotStore LoadStore()
        {
            try
            {
                if (!File.Exists(StorePath))
                    return new SnapshotStore();

                string json = File.ReadAllText(StorePath);
                return JsonSerializer.Deserialize<SnapshotStore>(json) ?? new SnapshotStore();
            }
            catch
            {
                return new SnapshotStore();
            }
        }

        private static void SaveStore(SnapshotStore store)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
            string json = JsonSerializer.Serialize(store, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(StorePath, json);
        }
    }
}
