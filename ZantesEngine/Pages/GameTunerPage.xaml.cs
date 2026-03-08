using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZantesEngine.Services;

namespace ZantesEngine.Pages
{
    public partial class GameTunerPage : Page
    {
        private sealed class GameProfile
        {
            public required string Id { get; init; }
            public required string NameKey { get; init; }
            public required string ScoreLabel { get; init; }
            public required double Score { get; init; }
            public bool Aggressive { get; init; }
            public required string[] TweakKeys { get; init; }
        }

        private sealed class GamePreset
        {
            public required string Id { get; init; }
            public required string NameKey { get; init; }
            public bool Aggressive { get; init; }
            public required string[] TweakKeys { get; init; }
        }

        private readonly Dictionary<string, GameProfile> _profiles = new()
        {
            ["competitive"] = new GameProfile
            {
                Id = "competitive",
                NameKey = "tuner.profile.competitive",
                ScoreLabel = "76%",
                Score = 76,
                TweakKeys = new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr",
                    "disable_mouse_accel",
                    "disable_sticky_keys_shortcut",
                    "mmcss_system_responsiveness",
                    "tcp_autotune"
                }
            },
            ["balanced"] = new GameProfile
            {
                Id = "balanced",
                NameKey = "tuner.profile.balanced",
                ScoreLabel = "64%",
                Score = 64,
                TweakKeys = new[]
                {
                    "enable_game_mode",
                    "tcp_autotune",
                    "network_rss",
                    "disable_windows_tips",
                    "disable_feedback_notifications"
                }
            },
            ["ultra"] = new GameProfile
            {
                Id = "ultra",
                NameKey = "tuner.profile.ultra",
                ScoreLabel = "91%",
                Score = 91,
                Aggressive = true,
                TweakKeys = new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr",
                    "disable_mouse_accel",
                    "hw_scheduling",
                    "priority_separation",
                    "disable_network_throttling",
                    "mmcss_system_responsiveness",
                    "disable_sysmain",
                    "disable_search_index",
                    "disable_background_apps",
                    "disable_transparency",
                    "disable_power_throttling",
                    "winsock_reset"
                }
            }
        };

        private readonly Dictionary<string, GamePreset> _presets = new()
        {
            ["valorant"] = new GamePreset
            {
                Id = "valorant",
                NameKey = "tuner.preset.valorant",
                TweakKeys = new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr",
                    "disable_mouse_accel",
                    "valorant_vanguard_compat",
                    "disable_sticky_keys_shortcut",
                    "disable_network_throttling",
                    "mmcss_system_responsiveness",
                    "network_rss",
                    "tcp_timestamps_disabled",
                    "tcp_rsc_disabled"
                }
            },
            ["cs2"] = new GamePreset
            {
                Id = "cs2",
                NameKey = "tuner.preset.cs2",
                Aggressive = true,
                TweakKeys = new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr",
                    "disable_mouse_accel",
                    "hw_scheduling",
                    "priority_separation",
                    "disable_network_throttling",
                    "mmcss_system_responsiveness",
                    "network_rss",
                    "tcp_autotune",
                    "tcp_timestamps_disabled"
                }
            },
            ["hoi4"] = new GamePreset
            {
                Id = "hoi4",
                NameKey = "tuner.preset.hoi4",
                TweakKeys = new[]
                {
                    "power_high_performance",
                    "cpu_maximum_state_100",
                    "cpu_core_parking_off",
                    "enable_game_mode",
                    "disable_game_dvr",
                    "disable_mouse_accel",
                    "mmcss_system_responsiveness",
                    "mmcss_games_task_profile",
                    "usb_selective_suspend_off",
                    "disable_transparency"
                }
            },
            ["fivem"] = new GamePreset
            {
                Id = "fivem",
                NameKey = "tuner.preset.fivem",
                TweakKeys = new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr",
                    "disable_mouse_accel",
                    "disable_network_throttling",
                    "mmcss_system_responsiveness",
                    "network_rss",
                    "tcp_autotune",
                    "disable_windows_error_reporting",
                    "disable_diagtrack_service",
                    "fivem_cache_cleanup",
                    "fivem_logs_cleanup"
                }
            },
            ["lol"] = new GamePreset
            {
                Id = "lol",
                NameKey = "tuner.preset.lol",
                TweakKeys = new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr",
                    "disable_mouse_accel",
                    "tcp_autotune",
                    "network_rss",
                    "disable_windows_tips",
                    "disable_feedback_notifications",
                    "disable_transparency"
                }
            },
            ["fortnite"] = new GamePreset
            {
                Id = "fortnite",
                NameKey = "tuner.preset.fortnite",
                TweakKeys = new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr",
                    "disable_mouse_accel",
                    "hw_scheduling",
                    "disable_network_throttling",
                    "mmcss_system_responsiveness",
                    "network_rss",
                    "tcp_autotune",
                    "tcp_heuristics_disabled",
                    "disable_diagtrack_service"
                }
            },
            ["apex"] = new GamePreset
            {
                Id = "apex",
                NameKey = "tuner.preset.apex",
                Aggressive = true,
                TweakKeys = new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr",
                    "disable_mouse_accel",
                    "hw_scheduling",
                    "priority_separation",
                    "disable_network_throttling",
                    "mmcss_system_responsiveness",
                    "network_rss",
                    "tcp_autotune",
                    "tcp_rsc_disabled",
                    "disable_diagtrack_service"
                }
            },
            ["pubg"] = new GamePreset
            {
                Id = "pubg",
                NameKey = "tuner.preset.pubg",
                TweakKeys = new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr",
                    "disable_mouse_accel",
                    "disable_network_throttling",
                    "mmcss_system_responsiveness",
                    "network_rss",
                    "tcp_autotune",
                    "tcp_timestamps_disabled",
                    "disable_windows_error_reporting"
                }
            },
            ["r6"] = new GamePreset
            {
                Id = "r6",
                NameKey = "tuner.preset.r6",
                TweakKeys = new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr",
                    "disable_mouse_accel",
                    "disable_sticky_keys_shortcut",
                    "priority_separation",
                    "disable_network_throttling",
                    "mmcss_system_responsiveness",
                    "network_rss",
                    "tcp_heuristics_disabled",
                    "disable_activity_history"
                }
            },
            ["ow2"] = new GamePreset
            {
                Id = "ow2",
                NameKey = "tuner.preset.ow2",
                TweakKeys = new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr",
                    "disable_mouse_accel",
                    "hw_scheduling",
                    "disable_network_throttling",
                    "network_rss",
                    "tcp_autotune",
                    "tcp_chimney_disabled",
                    "disable_feedback_notifications"
                }
            }
        };

        private string _selectedProfileId = "competitive";
        private string _selectedPresetId = string.Empty;
        private bool _busy;
        private readonly DispatcherTimer _autoDetectTimer = new() { Interval = TimeSpan.FromSeconds(6) };
        private DateTime _lastAutoApplyUtc = DateTime.MinValue;
        private string _lastAutoPresetId = string.Empty;
        private bool _autoDetectEventsHooked;
        private static readonly IReadOnlyDictionary<string, string[]> GameImageAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["valorant"] = new[] { "valorant", "valo", "vanguard" },
            ["cs2"] = new[] { "cs2", "counter", "csgo" },
            ["hoi4"] = new[] { "hoi4", "heartsofiron4", "hearts_of_iron_iv" },
            ["fivem"] = new[] { "fivem", "gta5", "citizenfx" },
            ["lol"] = new[] { "lol", "league", "leagueoflegends" },
            ["fortnite"] = new[] { "fortnite", "fn" },
            ["apex"] = new[] { "apex", "r5apex" },
            ["pubg"] = new[] { "pubg", "tslgame" },
            ["r6"] = new[] { "r6", "rainbowsix", "rainbow6" },
            ["ow2"] = new[] { "ow2", "overwatch", "overwatch2" }
        };

        public GameTunerPage()
        {
            InitializeComponent();
            _autoDetectTimer.Tick += AutoDetectTimer_Tick;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_autoDetectEventsHooked)
            {
                CbAutoDetect.Checked += AutoDetectChanged;
                CbAutoDetect.Unchecked += AutoDetectChanged;
                _autoDetectEventsHooked = true;
            }

            LanguageManager.LanguageChanged += ApplyLanguage;
            ApplyLanguage();
            LoadGameImagesSafe();
            UpdateProfileUi();
            TxtApplyStatus.Text = LanguageManager.T("tuner.status.ready");
            UpdateAutoDetectUi();
            UpdateSelectedGameUi();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            LanguageManager.LanguageChanged -= ApplyLanguage;
            _autoDetectTimer.Stop();
            if (_autoDetectEventsHooked)
            {
                CbAutoDetect.Checked -= AutoDetectChanged;
                CbAutoDetect.Unchecked -= AutoDetectChanged;
                _autoDetectEventsHooked = false;
            }
        }

        private void ApplyLanguage()
        {
            TxtModule.Text = LanguageManager.T("tuner.module");
            TxtTitle.Text = LanguageManager.T("tuner.title");
            TxtProfileSelect.Text = LanguageManager.T("tuner.profile.label");
            TxtProfileSection.Text = LanguageManager.T("tuner.profile.section");
            BtnCompetitive.Content = LanguageManager.T("tuner.profile.competitive");
            BtnBalanced.Content = LanguageManager.T("tuner.profile.balanced");
            BtnUltraFps.Content = LanguageManager.T("tuner.profile.ultra");
            TxtPresetLabel.Text = LanguageManager.T("tuner.preset.label");
            TxtCardHint.Text = LanguageManager.T("tuner.preset.pick");
            TxtSelectedGameLabel.Text = LanguageManager.T("tuner.selected.label");
            CbAutoDetect.Content = LanguageManager.T("tuner.auto.detect_toggle");
            BtnApplyProfile.Content = LanguageManager.T("tuner.apply");
            TxtUpdateWatchLabel.Text = LanguageManager.T("tuner.watch.title");
            BtnWatchRegister.Content = LanguageManager.T("tuner.watch.register");
            BtnWatchCheck.Content = LanguageManager.T("tuner.watch.check");
            TxtFiveMToolsTitle.Text = LanguageManager.T("tuner.fivem.tools");
            BtnFiveMCleanCache.Content = LanguageManager.T("tuner.fivem.clean_cache");
            BtnFiveMCleanLogs.Content = LanguageManager.T("tuner.fivem.clean_logs");
            BtnFiveMFullPack.Content = LanguageManager.T("tuner.fivem.full_pack");

            if (CbAutoDetect.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(TxtAutoStatus.Text) || TxtAutoStatus.Text == LanguageManager.T("tuner.auto.off"))
                    TxtAutoStatus.Text = LanguageManager.T("tuner.auto.waiting");
            }
            else
            {
                TxtAutoStatus.Text = LanguageManager.T("tuner.auto.off");
            }

            UpdateProfileUi();
            UpdateSelectedGameUi();
            Dispatcher.BeginInvoke(new Action(() => LanguageManager.LocalizeTree(this)));
        }

        private void SetBusy(bool value)
        {
            _busy = value;
            BtnCompetitive.IsEnabled = !value;
            BtnBalanced.IsEnabled = !value;
            BtnUltraFps.IsEnabled = !value;
            BtnApplySelectedPreset.IsEnabled = !value;
            CardValorant.IsEnabled = !value;
            CardCs2.IsEnabled = !value;
            CardHoi4.IsEnabled = !value;
            CardFiveM.IsEnabled = !value;
            CardLol.IsEnabled = !value;
            CardFortnite.IsEnabled = !value;
            CardApex.IsEnabled = !value;
            CardPubg.IsEnabled = !value;
            CardR6.IsEnabled = !value;
            CardOw2.IsEnabled = !value;
            BtnApplyProfile.IsEnabled = !value;
            BtnWatchRegister.IsEnabled = !value;
            BtnWatchCheck.IsEnabled = !value;
            BtnFiveMCleanCache.IsEnabled = !value;
            BtnFiveMCleanLogs.IsEnabled = !value;
            BtnFiveMFullPack.IsEnabled = !value;
        }

        private void UpdateProfileUi()
        {
            if (!_profiles.TryGetValue(_selectedProfileId, out var profile))
                return;

            TxtActiveProfile.Text = LanguageManager.T(profile.NameKey);
            TxtProfileScore.Text = string.Format(LanguageManager.T("tuner.profile.score"), profile.ScoreLabel);
            ProfileScore.Value = profile.Score;
        }

        private async Task SelectAndApplyProfileAsync(string profileId)
        {
            if (_busy)
                return;

            _selectedProfileId = profileId;
            UpdateProfileUi();
            await ApplyCurrentProfileInternalAsync();
        }

        private async Task ApplyCurrentProfileInternalAsync()
        {
            if (_busy)
                return;

            if (!_profiles.TryGetValue(_selectedProfileId, out var profile))
            {
                TxtApplyStatus.Text = LanguageManager.T("tuner.msg.no_profile");
                return;
            }

            await ApplyTweakSetAsync(LanguageManager.T(profile.NameKey), profile.TweakKeys, profile.Aggressive);
        }

        private async Task ApplyTweakSetAsync(string displayName, string[] tweakKeys, bool aggressive, bool createRestorePoint = true)
        {
            if (_busy)
                return;

            if (!SystemTweakEngine.IsAdministrator())
            {
                TxtApplyStatus.Text = LanguageManager.T("tuner.need_admin");
                return;
            }

            var tweaks = tweakKeys
                .Select(SystemTweakCatalog.Get)
                .Where(t => t != null)
                .Cast<SystemTweakDefinition>()
                .ToArray();

            if (aggressive)
            {
                var cautionConfirm = MessageBox.Show(
                    LanguageManager.T("tuner.msg.preset_caution"),
                    "Zantes Tweak",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (cautionConfirm != MessageBoxResult.Yes)
                    return;
            }

            SetBusy(true);
            TxtApplyStatus.Text = string.Format(
                LanguageManager.T("tuner.status.applying"),
                LanguageManager.T("tuner.profile.active"),
                displayName);

            if (createRestorePoint)
            {
                var restore = await Task.Run(() =>
                    SystemTweakEngine.CreateRestorePoint($"Zantes Tweak Game Profile {DateTime.Now:yyyy-MM-dd HH:mm:ss}"));

                if (!restore.Success)
                {
                    var proceed = MessageBox.Show(
                        LanguageManager.T("tuner.msg.restore_fail"),
                        "Zantes Tweak",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (proceed != MessageBoxResult.Yes)
                    {
                        SetBusy(false);
                        TxtApplyStatus.Text = LanguageManager.LocalizeLiteral(restore.Message);
                        return;
                    }
                }
            }

            IReadOnlyList<SystemTweakResult> results;
            try
            {
                results = await SystemTweakEngine.ApplyAsync(tweaks, CancellationToken.None);
            }
            catch (Exception ex)
            {
                SetBusy(false);
                TxtApplyStatus.Text = ex.Message;
                return;
            }

            int ok = results.Count(r => r.Success);
            int fail = results.Count - ok;

            TxtApplyStatus.Text = $"{LanguageManager.T("tuner.applied")}: {displayName} | {string.Format(LanguageManager.T("tuner.msg.done"), ok, fail)}";

            SetBusy(false);
        }

        private async void Competitive_Click(object sender, RoutedEventArgs e)
            => await SelectAndApplyProfileAsync("competitive");

        private async void Balanced_Click(object sender, RoutedEventArgs e)
            => await SelectAndApplyProfileAsync("balanced");

        private async void UltraFps_Click(object sender, RoutedEventArgs e)
            => await SelectAndApplyProfileAsync("ultra");

        private async void ApplyCurrentProfile_Click(object sender, RoutedEventArgs e)
            => await ApplyCurrentProfileInternalAsync();

        private async void ApplySelectedPreset_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedPresetId))
            {
                TxtApplyStatus.Text = LanguageManager.T("tuner.msg.no_game");
                return;
            }

            await ApplyPresetAsync(_selectedPresetId, autoTriggered: false);
        }

        private void WatchRegister_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedPresetId))
            {
                TxtWatchStatus.Text = LanguageManager.T("tuner.watch.none");
                return;
            }

            var result = GameUpdateWatcherService.RegisterSnapshot(_selectedPresetId);
            TxtWatchStatus.Text = MapWatchResultToText(result);
        }

        private void WatchCheck_Click(object sender, RoutedEventArgs e)
            => UpdateWatchStatusForSelection(forceRefresh: true);

        private async void FiveMCleanCache_Click(object sender, RoutedEventArgs e)
            => await ApplyTweakSetAsync(
                LanguageManager.T("tuner.fivem.clean_cache"),
                new[] { "fivem_cache_cleanup" },
                aggressive: false,
                createRestorePoint: false);

        private async void FiveMCleanLogs_Click(object sender, RoutedEventArgs e)
            => await ApplyTweakSetAsync(
                LanguageManager.T("tuner.fivem.clean_logs"),
                new[] { "fivem_logs_cleanup" },
                aggressive: false,
                createRestorePoint: false);

        private async void FiveMFullPack_Click(object sender, RoutedEventArgs e)
            => await ApplyTweakSetAsync(
                LanguageManager.T("tuner.fivem.full_pack"),
                new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr",
                    "disable_mouse_accel",
                    "disable_network_throttling",
                    "mmcss_system_responsiveness",
                    "network_rss",
                    "tcp_autotune",
                    "disable_windows_error_reporting",
                    "disable_diagtrack_service",
                    "fivem_cache_cleanup",
                    "fivem_logs_cleanup",
                    "fivem_nui_storage_reset"
                },
                aggressive: true,
                createRestorePoint: true);

        private void GameCard_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton rb || rb.Tag is not string presetId)
                return;

            SelectGamePreset(presetId);
        }

        private void SelectGamePreset(string presetId)
        {
            if (!_presets.ContainsKey(presetId))
                return;

            _selectedPresetId = presetId;
            UpdateSelectedGameUi();
        }

        private void UpdateSelectedGameUi()
        {
            GamePreset? preset = null;
            bool hasSelection = !string.IsNullOrWhiteSpace(_selectedPresetId) &&
                _presets.TryGetValue(_selectedPresetId, out preset);

            SelectedGamePanel.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
            TxtCardHint.Visibility = hasSelection ? Visibility.Collapsed : Visibility.Visible;
            if (!hasSelection || preset == null)
            {
                TxtSelectedGameName.Text = LanguageManager.T("tuner.selected.none");
                TxtSelectedGameHint.Text = LanguageManager.T("tuner.selected.hint.none");
                BtnApplySelectedPreset.Content = LanguageManager.T("tuner.preset.apply");
                TxtWatchStatus.Text = LanguageManager.T("tuner.watch.none");
                FiveMToolsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            string presetName = LanguageManager.T(preset.NameKey);
            TxtSelectedGameName.Text = presetName;
            TxtSelectedGameHint.Text = LanguageManager.T(GetSelectedGameHintKey(_selectedPresetId));
            BtnApplySelectedPreset.Content = string.Format(LanguageManager.T("tuner.preset.apply_selected"), presetName);
            FiveMToolsPanel.Visibility = _selectedPresetId == "fivem" ? Visibility.Visible : Visibility.Collapsed;
            UpdateWatchStatusForSelection(forceRefresh: false);
        }

        private static string GetSelectedGameHintKey(string presetId)
            => presetId switch
            {
                "valorant" => "tuner.selected.hint.valorant",
                "cs2" => "tuner.selected.hint.cs2",
                "hoi4" => "tuner.selected.hint.hoi4",
                "fivem" => "tuner.selected.hint.fivem",
                "lol" => "tuner.selected.hint.lol",
                "fortnite" => "tuner.selected.hint.fortnite",
                "apex" => "tuner.selected.hint.apex",
                "pubg" => "tuner.selected.hint.pubg",
                "r6" => "tuner.selected.hint.r6",
                "ow2" => "tuner.selected.hint.ow2",
                _ => "tuner.selected.hint.none"
            };

        private async Task ApplyPresetAsync(string presetId, bool autoTriggered)
        {
            if (_busy)
                return;

            if (!_presets.TryGetValue(presetId, out var preset))
                return;

            SelectGamePreset(presetId);

            string presetName = LanguageManager.T(preset.NameKey);
            if (!autoTriggered)
            {
                var confirm = MessageBox.Show(
                    string.Format(LanguageManager.T("tuner.msg.preset_confirm"), presetName),
                    "Zantes Tweak",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;
            }
            else
            {
                TxtAutoStatus.Text = string.Format(LanguageManager.T("tuner.auto.detected"), presetName);
            }

            await ApplyTweakSetAsync(presetName, preset.TweakKeys, preset.Aggressive);
            ApplyGpuAutomationForPreset(presetId);
            UpdateWatchStatusForSelection(forceRefresh: false);
        }

        private void UpdateWatchStatusForSelection(bool forceRefresh)
        {
            if (string.IsNullOrWhiteSpace(_selectedPresetId))
            {
                TxtWatchStatus.Text = LanguageManager.T("tuner.watch.none");
                return;
            }

            var result = GameUpdateWatcherService.CheckForUpdate(_selectedPresetId);
            TxtWatchStatus.Text = MapWatchResultToText(result);

            if (forceRefresh && result.Code == "updated")
                TxtApplyStatus.Text = LanguageManager.T("tuner.watch.updated_warn");
        }

        private static string MapWatchResultToText(GameUpdateWatchResult result)
        {
            return result.Code switch
            {
                "registered" => LanguageManager.T("tuner.watch.registered"),
                "up_to_date" => LanguageManager.T("tuner.watch.up_to_date"),
                "updated" => LanguageManager.T("tuner.watch.updated"),
                "not_tracked" => LanguageManager.T("tuner.watch.not_tracked"),
                "exe_not_found" => LanguageManager.T("tuner.watch.exe_not_found"),
                "resolve_failed" => LanguageManager.T("tuner.watch.resolve_failed"),
                _ => LanguageManager.T("tuner.watch.error")
            };
        }

        private void AutoDetectChanged(object sender, RoutedEventArgs e)
        {
            UpdateAutoDetectUi();
        }

        private void UpdateAutoDetectUi()
        {
            if (CbAutoDetect == null || TxtAutoStatus == null)
                return;

            if (CbAutoDetect.IsChecked == true)
            {
                TxtAutoStatus.Text = LanguageManager.T("tuner.auto.waiting");
                _autoDetectTimer.Start();
            }
            else
            {
                TxtAutoStatus.Text = LanguageManager.T("tuner.auto.off");
                _autoDetectTimer.Stop();
            }
        }

        private async void AutoDetectTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (CbAutoDetect.IsChecked != true || _busy)
                    return;

                if (!SystemTweakEngine.IsAdministrator())
                {
                    TxtAutoStatus.Text = LanguageManager.T("tuner.auto.need_admin");
                    return;
                }

                string detectedPresetId = DetectPresetFromRunningProcesses();
                if (string.IsNullOrWhiteSpace(detectedPresetId))
                {
                    if (TxtAutoStatus.Text != LanguageManager.T("tuner.auto.waiting"))
                        TxtAutoStatus.Text = LanguageManager.T("tuner.auto.waiting");
                    return;
                }

                if (_lastAutoPresetId == detectedPresetId && DateTime.UtcNow - _lastAutoApplyUtc < TimeSpan.FromMinutes(20))
                    return;

                _lastAutoPresetId = detectedPresetId;
                _lastAutoApplyUtc = DateTime.UtcNow;

                SelectGamePreset(detectedPresetId);
                await ApplyPresetAsync(detectedPresetId, autoTriggered: true);
            }
            catch
            {
                TxtAutoStatus.Text = LanguageManager.T("tuner.auto.waiting");
            }
        }

        private static string DetectPresetFromRunningProcesses()
        {
            try
            {
                var running = Process.GetProcesses()
                    .Select(p =>
                    {
                        try { return p.ProcessName; } catch { return string.Empty; }
                    })
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n.ToLowerInvariant())
                    .ToHashSet();

                if (running.Contains("valorant-win64-shipping") || running.Contains("valorant") || running.Contains("riotclientservices"))
                    return "valorant";
                if (running.Contains("cs2") || running.Contains("csgo"))
                    return "cs2";
                if (running.Contains("hoi4") || running.Contains("heartsofiron4"))
                    return "hoi4";
                if (running.Any(n => n.Contains("fivem")))
                    return "fivem";
                if (running.Contains("leagueclient") || running.Contains("leagueclientux") || running.Contains("leagueclientuxrender"))
                    return "lol";
                if (running.Contains("fortniteclient-win64-shipping") || running.Contains("fortniteclient-win64-shipping_eac_eos"))
                    return "fortnite";
                if (running.Contains("r5apex"))
                    return "apex";
                if (running.Contains("tslgame"))
                    return "pubg";
                if (running.Contains("rainbowsix") || running.Contains("rainbowsix_vulkan"))
                    return "r6";
                if (running.Contains("overwatch"))
                    return "ow2";
            }
            catch
            {
                return string.Empty;
            }

            return string.Empty;
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
            => NavigationService?.GoBack();

        private void LoadGameImagesSafe()
        {
            SetGameImage(ImgValorant, "valorant");
            SetGameImage(ImgCs2, "cs2");
            SetGameImage(ImgHoi4, "hoi4");
            SetGameImage(ImgFiveM, "fivem");
            SetGameImage(ImgLol, "lol");
            SetGameImage(ImgFortnite, "fortnite");
            SetGameImage(ImgApex, "apex");
            SetGameImage(ImgPubg, "pubg");
            SetGameImage(ImgR6, "r6");
            SetGameImage(ImgOw2, "ow2");
        }

        private static void SetGameImage(Image target, string baseName)
        {
            if (target == null)
                return;

            string[] extensions = { ".png", ".jpg", ".jpeg", ".webp", ".bmp" };

            string? localMatch = FindBestLocalImage(baseName, extensions);
            if (!string.IsNullOrWhiteSpace(localMatch))
            {
                var bitmap = TryCreateBitmapFromUri(localMatch, UriKind.Absolute);
                if (bitmap != null)
                {
                    target.Source = bitmap;
                    return;
                }
            }

            foreach (string ext in extensions)
            {
                string localPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Games", baseName + ext);
                if (!File.Exists(localPath))
                    continue;

                var localBitmap = TryCreateBitmapFromUri(localPath, UriKind.Absolute);
                if (localBitmap != null)
                {
                    target.Source = localBitmap;
                    return;
                }
            }

            foreach (string ext in extensions)
            {
                string packUri = $"pack://application:,,,/Assets/Games/{baseName}{ext}";
                var resourceBitmap = TryCreateBitmapFromUri(packUri, UriKind.Absolute);
                if (resourceBitmap != null)
                {
                    target.Source = resourceBitmap;
                    return;
                }
            }
        }

        private static string? FindBestLocalImage(string baseName, IReadOnlyList<string> extensions)
        {
            string gameDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Games");
            if (!Directory.Exists(gameDir))
                return null;

            foreach (string ext in extensions)
            {
                string exact = Path.Combine(gameDir, baseName + ext);
                if (File.Exists(exact))
                    return exact;
            }

            string[] aliases = GameImageAliases.TryGetValue(baseName, out var mapped)
                ? mapped
                : new[] { baseName };

            var available = Directory.GetFiles(gameDir)
                .Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .ToArray();

            foreach (string alias in aliases)
            {
                string? contains = available.FirstOrDefault(path =>
                    Path.GetFileNameWithoutExtension(path).Contains(alias, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(contains))
                    return contains;
            }

            return null;
        }

        private static BitmapImage? TryCreateBitmapFromUri(string pathOrPackUri, UriKind kind)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(pathOrPackUri, kind);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private void ApplyGpuAutomationForPreset(string presetId)
        {
            try
            {
                var result = GpuAutomationService.ApplyHighPerformanceForPreset(presetId);
                if (!result.Executed)
                    return;

                if (result.UpdatedEntryCount > 0)
                    TxtApplyStatus.Text += $" | GPU: {result.UpdatedEntryCount} profile(s)";
                else if (!string.IsNullOrWhiteSpace(result.Message))
                    TxtApplyStatus.Text += $" | GPU: {result.Message}";
            }
            catch
            {
                // ignored: GPU automation is best-effort
            }
        }
    }
}
