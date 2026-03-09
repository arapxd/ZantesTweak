using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using ZantesEngine.Services;

namespace ZantesEngine.Pages
{
    public partial class GameTunerPage : Page
    {
        private static readonly HashSet<string> AutoFpsSafeTweakKeys =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "enable_game_mode",
                "disable_game_dvr",
                "fivem_cache_cleanup",
                "fivem_logs_cleanup"
            };

        private static readonly string[] BaseGameTunerKeys =
        {
            "enable_game_mode",
            "disable_game_dvr"
        };

        private static readonly string[] NoAdditionalKeys = Array.Empty<string>();
        private static readonly string[] FiveMBalancedKeys = { "fivem_logs_cleanup" };
        private static readonly string[] FiveMPerformanceKeys = { "fivem_logs_cleanup", "fivem_cache_cleanup" };

        private sealed class GameProfile
        {
            public required string Id { get; init; }
            public required string Badge { get; init; }
            public required string Title { get; init; }
            public required string Description { get; init; }
            public required string QualityDescription { get; init; }
            public required string QualityDetail { get; init; }
            public required string BalancedDescription { get; init; }
            public required string BalancedDetail { get; init; }
            public required string PerformanceDescription { get; init; }
            public required string PerformanceDetail { get; init; }
            public required string[] BaseKeys { get; init; }
            public required string[] QualityKeys { get; init; }
            public required string[] BalancedKeys { get; init; }
            public required string[] PerformanceKeys { get; init; }
        }

        private static readonly IReadOnlyDictionary<string, GameProfile> Profiles =
            new Dictionary<string, GameProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["valorant"] = new GameProfile
                {
                    Id = "valorant",
                    Badge = "TACTICAL PROFILE",
                    Title = "VALORANT",
                    Description = "Riot-focused profile with anti-cheat-safe Windows gaming defaults, GPU preference, and live process tuning.",
                    QualityDescription = "Safer tactical profile with smoother presentation and lower-risk scheduler changes.",
                    QualityDetail = "Keeps Vanguard compatibility, reduces overlay noise, and favors consistent frametime.",
                    BalancedDescription = "Competitive-ready default for stable latency, safer quality, and cleaner foreground focus.",
                    BalancedDetail = "Blends capture cleanup, network tuning, and foreground scheduling without pushing too hard.",
                    PerformanceDescription = "Aggressive low-latency bias for stronger prioritization and faster game response.",
                    PerformanceDetail = "Pushes game-mode tuning harder and adds stronger GPU, network, and priority-focused tweaks.",
                    BaseKeys = BaseGameTunerKeys,
                    QualityKeys = NoAdditionalKeys,
                    BalancedKeys = NoAdditionalKeys,
                    PerformanceKeys = NoAdditionalKeys
                },
                ["cs2"] = new GameProfile
                {
                    Id = "cs2",
                    Badge = "COMPETITIVE PROFILE",
                    Title = "CS2",
                    Description = "Counter-Strike profile with safe Windows gaming defaults, GPU preference, and live process tuning.",
                    QualityDescription = "Leans toward cleaner visuals and safer low-latency tuning for stable matches.",
                    QualityDetail = "Targets stable frame pacing, simpler network tuning, and fewer background interruptions.",
                    BalancedDescription = "Balanced competitive profile for steady frametime and strong input response.",
                    BalancedDetail = "Keeps common gaming defaults, network cleanup, and stable priority behavior.",
                    PerformanceDescription = "Harder performance preset for aggressive foreground priority and reduced latency bias.",
                    PerformanceDetail = "Adds stronger GPU scheduling and network-focused tweaks for competitive response.",
                    BaseKeys = BaseGameTunerKeys,
                    QualityKeys = NoAdditionalKeys,
                    BalancedKeys = NoAdditionalKeys,
                    PerformanceKeys = NoAdditionalKeys
                },
                ["fivem"] = new GameProfile
                {
                    Id = "fivem",
                    Badge = "ROLEPLAY PROFILE",
                    Title = "FIVEM",
                    Description = "FiveM profile with safe local cleanup, GPU preference, and live process tuning for RP sessions.",
                    QualityDescription = "Safer FiveM path with no cleanup and a clean Windows gaming baseline.",
                    QualityDetail = "Keeps downloaded server files intact and only applies the FPS-safe Windows base.",
                    BalancedDescription = "Balanced FiveM pass with log cleanup, GPU preference, and lighter live tuning.",
                    BalancedDetail = "Adds log cleanup and per-game GPU preference without touching downloaded server cache.",
                    PerformanceDescription = "Performance-heavy FiveM pass with safe cache trim and stronger live process tuning.",
                    PerformanceDetail = "Adds safe local cache cleanup while keeping downloaded server assets intact.",
                    BaseKeys = BaseGameTunerKeys,
                    QualityKeys = NoAdditionalKeys,
                    BalancedKeys = FiveMBalancedKeys,
                    PerformanceKeys = FiveMPerformanceKeys
                },
                ["fortnite"] = new GameProfile
                {
                    Id = "fortnite",
                    Badge = "ARENA PROFILE",
                    Title = "FORTNITE",
                    Description = "Fortnite profile with FPS-safe Windows defaults, GPU preference, and stronger live game-process tuning.",
                    QualityDescription = "Safer Fortnite profile with lower-risk gaming tweaks and cleaner system overhead.",
                    QualityDetail = "Good for keeping a more stable desktop feel while still trimming gameplay overhead.",
                    BalancedDescription = "Balanced Fortnite tuning with lower capture overhead and solid scheduling cleanup.",
                    BalancedDetail = "Applies core game-mode and network tuning without the most aggressive performance extras.",
                    PerformanceDescription = "Faster performance-first preset for stronger prioritization and higher FPS bias.",
                    PerformanceDetail = "Pushes GPU scheduling and latency-focused tweaks harder for more competitive play.",
                    BaseKeys = BaseGameTunerKeys,
                    QualityKeys = NoAdditionalKeys,
                    BalancedKeys = NoAdditionalKeys,
                    PerformanceKeys = NoAdditionalKeys
                },
                ["hoi4"] = new GameProfile
                {
                    Id = "hoi4",
                    Badge = "STRATEGY PROFILE",
                    Title = "HEARTS OF IRON IV",
                    Description = "Long-session strategy profile with safe Windows gaming defaults, GPU preference, and live process tuning.",
                    QualityDescription = "Safer HOI4 preset with stable CPU scheduling and lower-risk background cleanup.",
                    QualityDetail = "Good for preserving desktop smoothness during longer strategy sessions.",
                    BalancedDescription = "Balanced strategy preset for steadier CPU time and lighter service pressure.",
                    BalancedDetail = "Blends core gaming defaults with moderate background and telemetry cleanup.",
                    PerformanceDescription = "Performance-heavy strategy preset with stronger process focus and reduced background interference.",
                    PerformanceDetail = "Pushes harder on service cleanup and scheduling to favor the game under sustained load.",
                    BaseKeys = BaseGameTunerKeys,
                    QualityKeys = NoAdditionalKeys,
                    BalancedKeys = NoAdditionalKeys,
                    PerformanceKeys = NoAdditionalKeys
                },
                ["lol"] = new GameProfile
                {
                    Id = "lol",
                    Badge = "ESPORTS PROFILE",
                    Title = "LEAGUE OF LEGENDS",
                    Description = "League profile with safe Windows gaming defaults, GPU preference, and live process tuning for matches.",
                    QualityDescription = "Safer LoL pass with stable desktop feel and low-risk gaming cleanup.",
                    QualityDetail = "Reduces popups and capture overhead while keeping changes conservative.",
                    BalancedDescription = "Default LoL preset for cleaner response and lighter background interruption.",
                    BalancedDetail = "Good middle ground for match stability without aggressive system changes.",
                    PerformanceDescription = "Performance-first LoL preset with stronger priority focus and lower background noise.",
                    PerformanceDetail = "Pushes cleaner scheduling and network defaults a bit harder for faster response.",
                    BaseKeys = BaseGameTunerKeys,
                    QualityKeys = NoAdditionalKeys,
                    BalancedKeys = NoAdditionalKeys,
                    PerformanceKeys = NoAdditionalKeys
                },
                ["apex"] = new GameProfile
                {
                    Id = "apex",
                    Badge = "BATTLE ROYALE PROFILE",
                    Title = "APEX LEGENDS",
                    Description = "Apex profile with FPS-safe Windows defaults, GPU preference, and stronger live process tuning.",
                    QualityDescription = "Safer Apex preset with steadier frame pacing and lower-risk competitive tuning.",
                    QualityDetail = "Targets cleaner gameplay without the hardest scheduling changes.",
                    BalancedDescription = "Balanced Apex preset with strong input response and moderate network cleanup.",
                    BalancedDetail = "Good default for most systems that want competitive response without over-pushing.",
                    PerformanceDescription = "Aggressive Apex preset for higher FPS bias, lower latency, and stronger game focus.",
                    PerformanceDetail = "Pushes GPU scheduling, network defaults, and foreground priority harder.",
                    BaseKeys = BaseGameTunerKeys,
                    QualityKeys = NoAdditionalKeys,
                    BalancedKeys = NoAdditionalKeys,
                    PerformanceKeys = NoAdditionalKeys
                },
                ["pubg"] = new GameProfile
                {
                    Id = "pubg",
                    Badge = "SURVIVAL PROFILE",
                    Title = "PUBG",
                    Description = "PUBG profile with safe Windows gaming defaults, GPU preference, and live process tuning.",
                    QualityDescription = "Safer PUBG preset that favors frame stability and lower-risk cleanup.",
                    QualityDetail = "Keeps the tuning pass moderate while reducing unnecessary Windows noise.",
                    BalancedDescription = "Balanced PUBG preset for stable pacing, lower popups, and better foreground focus.",
                    BalancedDetail = "Applies core game-mode and lighter service cleanup without going fully aggressive.",
                    PerformanceDescription = "Aggressive PUBG preset for stronger game priority and lower latency bias.",
                    PerformanceDetail = "Adds heavier scheduling, background trimming, and network-oriented tuning.",
                    BaseKeys = BaseGameTunerKeys,
                    QualityKeys = NoAdditionalKeys,
                    BalancedKeys = NoAdditionalKeys,
                    PerformanceKeys = NoAdditionalKeys
                },
                ["r6"] = new GameProfile
                {
                    Id = "r6",
                    Badge = "TACTICAL PROFILE",
                    Title = "RAINBOW SIX",
                    Description = "Rainbow Six profile with safe Windows gaming defaults, GPU preference, and live process tuning.",
                    QualityDescription = "Safer Rainbow Six preset with cleaner response and lower-risk system changes.",
                    QualityDetail = "Good for keeping a more stable desktop feel while trimming gaming overhead.",
                    BalancedDescription = "Balanced tactical preset for lower latency and consistent game responsiveness.",
                    BalancedDetail = "Uses core game tuning and modest network cleanup without the harshest tweaks.",
                    PerformanceDescription = "Stronger tactical preset for more aggressive prioritization and reduced latency bias.",
                    PerformanceDetail = "Pushes game focus, network cleanup, and GPU scheduling harder for competitive use.",
                    BaseKeys = BaseGameTunerKeys,
                    QualityKeys = NoAdditionalKeys,
                    BalancedKeys = NoAdditionalKeys,
                    PerformanceKeys = NoAdditionalKeys
                },
                ["ow2"] = new GameProfile
                {
                    Id = "ow2",
                    Badge = "ARENA PROFILE",
                    Title = "OVERWATCH 2",
                    Description = "Overwatch profile with safe Windows gaming defaults, GPU preference, and live process tuning.",
                    QualityDescription = "Safer Overwatch preset for cleaner visuals and lower-risk performance tuning.",
                    QualityDetail = "Targets consistent response without over-pushing Windows scheduling.",
                    BalancedDescription = "Balanced Overwatch preset for strong response and stable frametime.",
                    BalancedDetail = "Blends game-mode cleanup and moderate network tuning for general competitive play.",
                    PerformanceDescription = "Aggressive Overwatch preset for faster response, stronger priority, and higher FPS bias.",
                    PerformanceDetail = "Pushes scheduler and network choices harder to favor the game process.",
                    BaseKeys = BaseGameTunerKeys,
                    QualityKeys = NoAdditionalKeys,
                    BalancedKeys = NoAdditionalKeys,
                    PerformanceKeys = NoAdditionalKeys
                }
            };

        private bool _busy;
        private GameProfile? _selectedProfile;
        private string _applyStatusLiteral = "Choose a side, then apply the selected game profile.";
        private string _overlayStatusLiteral = "Applying game profile...";

        public GameTunerPage()
        {
            InitializeComponent();
            LanguageManager.LanguageChanged += ApplyLanguage;
            Unloaded += OnUnloaded;
            ApplyLanguage();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
            => LanguageManager.LanguageChanged -= ApplyLanguage;

        private void ApplyLanguage()
        {
            bool tr = LanguageManager.CurrentLanguage == UiLanguage.Turkish;
            TxtLibraryEyebrow.Text = tr ? "OYUNLAR" : "GAMES";
            TxtLibraryTitle.Text = tr ? "Bir oyun sec, sonra kendi tuning sayfasini ac." : "Pick a title, then open its own tuning page.";
            TxtLibraryBody.Text = tr
                ? "Her oyun kendi quality/performance geçisine, geri yukleme noktasina ve GPU oncelik adimina sahip."
                : "Each game gets its own quality or performance pass, restore point, and GPU priority step.";
            BtnBackToLibrary.Content = Localize("BACK");
            BtnApplyMode.Content = Localize("APPLY THIS PROFILE");
            ModeLabelLeft.Text = Localize("QUALITY");
            ModeLabelCenter.Text = Localize("BALANCED");
            ModeLabelRight.Text = Localize("PERFORMANCE");
            TxtLibraryBody.Text = tr
                ? "Her oyun kendi kalite veya performans gecisi, geri yukleme noktasi ve GPU oncelik adimi ile gelir."
                : "Each game gets its own quality or performance pass, restore point, and GPU priority step.";
            RefreshSelectedProfileUi();
            UpdateModeUi();
            SetApplyStatus(_applyStatusLiteral);
            SetOverlayStatus(_overlayStatusLiteral);
            Dispatcher.BeginInvoke(new Action(() => LanguageManager.LocalizeTree(this)));
        }

        private void GameCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button element || element.CommandParameter is not string profileId)
                return;

            OpenProfile(profileId);
        }

        private void BtnBackToLibrary_Click(object sender, RoutedEventArgs e)
        {
            _selectedProfile = null;
            DetailView.Visibility = Visibility.Collapsed;
            LibraryView.Visibility = Visibility.Visible;
            SetApplyStatus("Choose a side, then apply the selected game profile.");
            SetOverlayStatus("Applying game profile...");
        }

        private void OpenProfile(string profileId)
        {
            if (!Profiles.TryGetValue(profileId, out var profile))
                return;

            _selectedProfile = profile;
            RefreshSelectedProfileUi();
            ModeSlider.Value = 50;
            LibraryView.Visibility = Visibility.Collapsed;
            DetailView.Visibility = Visibility.Visible;
            SetApplyStatus($"{profile.Title} profile is ready.");
            UpdateModeUi();
        }

        private void ModeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => UpdateModeUi();

        private void UpdateModeUi()
        {
            if (_selectedProfile == null ||
                ModeSlider == null ||
                ModeBadge == null ||
                ModeDescription == null ||
                ModeDetail == null)
                return;

            if (ModeSlider.Value <= 35)
            {
                ModeBadge.Text = Localize("QUALITY");
                ModeDescription.Text = Localize("Keeps only evidence-backed Windows gaming defaults.");
                ModeDetail.Text = Localize("Applies Game Mode and disables Game DVR without adding mixed-result tweaks.");
            }
            else if (ModeSlider.Value >= 65)
            {
                ModeBadge.Text = Localize("PERFORMANCE");
                ModeDescription.Text = Localize("Adds high-performance GPU preference and stronger live process tuning when the selected game is running.");
                ModeDetail.Text = Localize("Keeps the FPS-safe base, applies per-game graphics preference, and enables runtime process policy.");
            }
            else
            {
                ModeBadge.Text = Localize("BALANCED");
                ModeDescription.Text = Localize("Uses the FPS-safe base and adds per-game graphics preference when the selected executable is found.");
                ModeDetail.Text = Localize("Good default if you want the safe baseline plus GPU preference without the heavier live process pass.");
            }
        }

        private async void BtnApplyMode_Click(object sender, RoutedEventArgs e)
        {
            if (_busy || _selectedProfile == null)
                return;

            if (!SystemTweakEngine.IsAdministrator())
            {
                SetApplyStatusRaw(Localize("Administrator privileges required."));
                SetOverlayStatusRaw(Localize("Open Zantes Tweak as administrator, then apply the selected game profile again."));
                return;
            }

            SetBusy(true);
            try
            {
                var restore = SystemTweakEngine.CreateRestorePoint($"Zantes Tweak {_selectedProfile.Title} {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                if (!restore.Success)
                {
                    SetApplyStatusRaw(
                        LanguageManager.CurrentLanguage == UiLanguage.Turkish
                            ? $"Geri yukleme noktasi olusturulamadi: {restore.Message}"
                            : $"Restore point failed: {restore.Message}");
                    SetOverlayStatusRaw(ApplyStatusText.Text);
                    return;
                }

                SetOverlayStatus($"{_selectedProfile.Title} profile is applying...");
                var tweaks = ResolveDefinitions(_selectedProfile).ToArray();
                var results = await SystemTweakEngine.ApplyAsync(tweaks, CancellationToken.None);
                var gpuResult = ShouldApplyGpuPreferenceForCurrentMode()
                    ? await Task.Run(() => GpuAutomationService.ApplyHighPerformanceForPreset(_selectedProfile.Id))
                    : new GpuAutomationResult
                    {
                        Executed = false,
                        UpdatedEntryCount = 0,
                        Message = LanguageManager.CurrentLanguage == UiLanguage.Turkish
                            ? "Kalite modunda GPU tercihi atlandi."
                            : "GPU preference skipped in quality mode."
                    };
                var processPolicyResult = ShouldApplyProcessPolicyForCurrentMode()
                    ? await Task.Run(() => GameProcessPolicyService.ApplyPerformancePolicyForPreset(_selectedProfile.Id))
                    : new GameProcessPolicyResult
                    {
                        Executed = false,
                        UpdatedProcessCount = 0,
                        Message = LanguageManager.CurrentLanguage == UiLanguage.Turkish
                            ? "Kalite modunda surec politikasi atlandi."
                            : "Process policy skipped in quality mode."
                    };
                int ok = results.Count(r => r.Success);
                int fail = results.Count - ok;

                SetApplyStatus(fail == 0
                    ? $"{_selectedProfile.Title} profile applied."
                    : $"{_selectedProfile.Title} profile applied with warnings.");

                await Task.Delay(900);

                bool tr = LanguageManager.CurrentLanguage == UiLanguage.Turkish;
                string summary =
                    $"{(tr ? "Geri yukleme" : "Restore")}: {restore.Message}{Environment.NewLine}" +
                    $"{(tr ? "Oyun" : "Game")}: {_selectedProfile.Title}{Environment.NewLine}" +
                    $"{(tr ? "Mod" : "Mode")}: {ModeBadge.Text}{Environment.NewLine}" +
                    $"{(tr ? "GPU tercihi" : "GPU preference")}: {gpuResult.Message}{Environment.NewLine}" +
                    $"{(tr ? "Surec politikasi" : "Process policy")}: {BuildProcessPolicySummary(processPolicyResult, tr)}{Environment.NewLine}" +
                    $"{(tr ? "Basarili" : "Success")}: {ok}{Environment.NewLine}" +
                    $"{(tr ? "Basarisiz" : "Failed")}: {fail}";

                SetApplyStatusRaw(summary);
                SetOverlayStatusRaw(summary);
            }
            catch (Exception ex)
            {
                SetApplyStatus("Game profile failed.");
                SetOverlayStatusRaw(ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private IEnumerable<SystemTweakDefinition> ResolveDefinitions(GameProfile profile)
        {
            IEnumerable<string> modeKeys = ModeSlider.Value switch
            {
                <= 35 => profile.QualityKeys,
                >= 65 => profile.PerformanceKeys,
                _ => profile.BalancedKeys
            };

            return profile.BaseKeys
                .Concat(modeKeys)
                .Where(AutoFpsSafeTweakKeys.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(SystemTweakCatalog.Get)
                .Where(t => t != null)
                .Cast<SystemTweakDefinition>();
        }

        private bool ShouldApplyGpuPreferenceForCurrentMode()
            => ModeSlider.Value >= 35;

        private bool ShouldApplyProcessPolicyForCurrentMode()
            => ModeSlider.Value >= 65;

        private static string BuildProcessPolicySummary(GameProcessPolicyResult result, bool tr)
        {
            if (!result.Executed || result.UpdatedProcessCount == 0)
                return tr ? "Desteklenen calisan oyun sureci bulunamadi." : "No supported running game process was found.";

            return tr
                ? $"{result.UpdatedProcessCount} calisan oyun surecinde execution-speed throttling kapatildi."
                : $"Execution-speed throttling disabled for {result.UpdatedProcessCount} running game process(es).";
        }

        private void RefreshSelectedProfileUi()
        {
            if (_selectedProfile == null)
                return;

            SelectedGameBadge.Text = Localize(_selectedProfile.Badge);
            SelectedGameTitle.Text = _selectedProfile.Title;
            SelectedGameDescription.Text = Localize(_selectedProfile.Description);
        }

        private void SetApplyStatus(string englishLiteral)
        {
            _applyStatusLiteral = englishLiteral;
            ApplyStatusText.Text = FormatStatus(englishLiteral);
        }

        private void SetApplyStatusRaw(string text)
        {
            _applyStatusLiteral = text;
            ApplyStatusText.Text = text;
        }

        private void SetOverlayStatus(string englishLiteral)
        {
            _overlayStatusLiteral = englishLiteral;
            OverlayStatusText.Text = FormatStatus(englishLiteral);
        }

        private void SetOverlayStatusRaw(string text)
        {
            _overlayStatusLiteral = text;
            OverlayStatusText.Text = text;
        }

        private static string Localize(string literal)
            => LanguageManager.LocalizeLiteral(literal);

        private static string FormatStatus(string englishLiteral)
        {
            bool tr = LanguageManager.CurrentLanguage == UiLanguage.Turkish;

            if (englishLiteral.EndsWith(" profile is ready.", StringComparison.Ordinal))
            {
                string title = englishLiteral[..^" profile is ready.".Length];
                return tr ? $"{title} profili hazir." : englishLiteral;
            }

            if (englishLiteral.EndsWith(" profile is applying...", StringComparison.Ordinal))
            {
                string title = englishLiteral[..^" profile is applying...".Length];
                return tr ? $"{title} profili uygulaniyor..." : englishLiteral;
            }

            if (englishLiteral.EndsWith(" profile applied with warnings.", StringComparison.Ordinal))
            {
                string title = englishLiteral[..^" profile applied with warnings.".Length];
                return tr ? $"{title} profili uyarilarla uygulandi." : englishLiteral;
            }

            if (englishLiteral.EndsWith(" profile applied.", StringComparison.Ordinal))
            {
                string title = englishLiteral[..^" profile applied.".Length];
                return tr ? $"{title} profili uygulandi." : englishLiteral;
            }

            return Localize(englishLiteral);
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            BtnApplyMode.IsEnabled = !busy;
            ModeSlider.IsEnabled = !busy;
            BtnBackToLibrary.IsEnabled = !busy;

            if (busy)
            {
                ProgressOverlay.Visibility = Visibility.Visible;
                ProgressOverlay.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(160)));
            }
            else
            {
                var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180));
                fade.Completed += (_, _) => ProgressOverlay.Visibility = Visibility.Collapsed;
                ProgressOverlay.BeginAnimation(OpacityProperty, fade);
            }
        }
    }
}
