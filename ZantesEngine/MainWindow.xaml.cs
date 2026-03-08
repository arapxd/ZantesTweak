using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ZantesEngine.Services;

namespace ZantesEngine
{
    public partial class MainWindow : Window
    {
        private const string DiscordApplicationIdDefault = "1478153809044701446";

        private PerformanceCounter? _cpu;
        private CancellationTokenSource _cts = new();
        private DiscordRichPresenceService? _richPresence;
        private bool _richPresenceEnabled = true;
        private bool _launchMaximized;
        private readonly long _presenceStartUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        private bool _isAuthorized;
        private bool _uiReady;
        private bool _isNavigating;
        private bool _loginInProgress;
        private bool _autoCheckUpdates = true;
        private bool _updateBusy;
        private string _discordDisplayName = string.Empty;
        private string _currentNavTag = "dashboard";
        private GitHubReleaseInfo? _latestReleaseInfo;
        private volatile bool _isWindowActive = true;
        private volatile bool _isWindowMinimized;
        private readonly Brush? _defaultSessionBackgroundFill;
        private readonly string _discordRpcAppId = ResolveDiscordRpcAppId();
        private readonly Random _ambientRnd = new();
        private static readonly HttpClient AvatarHttp = BuildAvatarHttpClient();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buf);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                ApplyStoredSettings();
            }
            catch (Exception ex)
            {
                App.LogException("Stored settings could not be applied.", ex);
            }

            MainFrame.NavigationFailed += MainFrame_NavigationFailed;
            MainFrame.Navigated += MainFrame_Navigated;
            MainFrame.Opacity = 1;
            SidebarPanel.IsEnabled = false;
            MainFrame.IsEnabled = false;
            NavDashboard.IsChecked = true;
            _defaultSessionBackgroundFill = SessionBackgroundVisual.Fill?.CloneCurrentValue() as Brush;
            AvatarFallbackText.Text = "?";

            if (!TryLoadInitialDashboard())
                ShowStartupFallback("Dashboard could not be opened.", "Open another page from the sidebar or restart after checking the startup log.");

            LanguageManager.LanguageChanged += ApplyLanguage;
            ApplyLanguage();
            _uiReady = true;

            Loaded += OnLoaded;
            Closed += OnClosed;
            Activated += OnWindowActivated;
            Deactivated += OnWindowDeactivated;
            StateChanged += OnWindowStateChanged;
        }

        private bool TryLoadInitialDashboard()
        {
            try
            {
                MainFrame.Navigate(new Pages.Dashboard());
                return true;
            }
            catch (Exception ex)
            {
                App.LogException("Initial dashboard navigation failed.", ex);
                return false;
            }
        }

        private void ShowStartupFallback(string title, string body)
        {
            MainFrame.Content = new Border
            {
                Margin = new Thickness(18),
                Padding = new Thickness(24),
                CornerRadius = new CornerRadius(22),
                Background = new SolidColorBrush(Color.FromRgb(16, 30, 43)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(45, 78, 102)),
                BorderThickness = new Thickness(1),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            Foreground = new SolidColorBrush(Color.FromRgb(244, 247, 251)),
                            FontFamily = new FontFamily("Bahnschrift"),
                            FontSize = 26,
                            FontWeight = FontWeights.Bold,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Margin = new Thickness(0, 10, 0, 0),
                            Text = body,
                            Foreground = new SolidColorBrush(Color.FromRgb(159, 177, 199)),
                            FontSize = 12,
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                }
            };
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _isWindowMinimized = WindowState == WindowState.Minimized;
            UpdateMaximizeGlyph();

            try
            {
                _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                _cpu.NextValue();
            }
            catch { }

            // Keep startup lightweight: skip ambient particle generation.
            SetLoginBusyState(false, "login.status.ready");
            StartLoop();
            UpdateRichPresenceActivity();
            _ = CheckForUpdatesAsync(userInitiated: false);
            _ = RestoreDiscordSessionAsync();
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            _cts.Cancel();
            _cpu?.Dispose();
            _richPresence?.Dispose();
            MainFrame.NavigationFailed -= MainFrame_NavigationFailed;
            MainFrame.Navigated -= MainFrame_Navigated;
            LanguageManager.LanguageChanged -= ApplyLanguage;
        }

        private void OnWindowActivated(object? sender, EventArgs e)
            => _isWindowActive = true;

        private void OnWindowDeactivated(object? sender, EventArgs e)
            => _isWindowActive = false;

        private void OnWindowStateChanged(object? sender, EventArgs e)
        {
            _isWindowMinimized = WindowState == WindowState.Minimized;
            UpdateMaximizeGlyph();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            // Parallax disabled for stable UX.
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            // Parallax disabled for stable UX.
        }

        private static void AnimateParallax(TranslateTransform transform, double toX, double toY, int durationMs)
        {
            // Intentionally disabled.
        }

        private void ApplyLanguage()
        {
            Title = LanguageManager.T("app.name");
            TxtAppName.Text = LanguageManager.T("app.name");
            TxtAppControl.Text = LanguageManager.T("app.control");
            TxtHeaderHint.Text = LanguageManager.T("header.hint");

            TxtStatusStable.Text = LanguageManager.T("status.system_stable");
            TxtStatusDiscord.Text = LanguageManager.T("status.discord_ready");
            TxtStatusAuthorized.Text = LanguageManager.T("status.authorized");
            TxtBrandCredit.Text = LanguageManager.T("brand.credit");

            TxtSessionLabel.Text = LanguageManager.T("session");
            AuthStatusText.Text = _isAuthorized
                ? LanguageManager.T("session.unlocked")
                : LanguageManager.T("session.locked");

            TxtDiscordUserLabel.Text = LanguageManager.T("session.user_label");
            TxtDiscordUserName.Text = _isAuthorized && !string.IsNullOrWhiteSpace(_discordDisplayName)
                ? _discordDisplayName
                : LanguageManager.T("session.user_none");

            TxtLanguageLabel.Text = LanguageManager.T("lang.label");
            SetLanguageSelectorOptions();
            TxtSettingsButton.Text = LanguageManager.T("settings.button");

            TxtNavDashboard.Text = LanguageManager.T("nav.dashboard");
            TxtNavQuickBoost.Text = LanguageManager.T("nav.quickboost");
            TxtNavOptimizer.Text = LanguageManager.T("nav.optimizer");
            TxtNavTuner.Text = LanguageManager.T("nav.tuner");
            TxtNavNetwork.Text = LanguageManager.T("nav.network");
            TxtNavPerformance.Text = LanguageManager.T("nav.performance");
            TxtNavBenchmark.Text = LanguageManager.T("nav.benchmark");

            TxtSidebarTip.Text = LanguageManager.T("sidebar.tip");

            TxtLoginTitle.Text = LanguageManager.T("login.title");
            TxtLoginDesc.Text = LanguageManager.T("login.desc");
            TxtLoginBrand.Text = LanguageManager.T("brand.credit");
            TxtAuthLabel.Text = LanguageManager.T("login.auth");
            TxtDiscordTitle.Text = LanguageManager.T("login.discord");
            TxtOauthDesc.Text = LanguageManager.T("login.oauth_desc");
            TxtLoginFeatures.Text = LanguageManager.T("login.features");
            TxtLoginRestoreTitle.Text = LanguageManager.T("login.restore.title");
            TxtLoginRestoreDesc.Text = LanguageManager.T("login.restore.desc");
            TxtLoginProfileTitle.Text = LanguageManager.T("login.profile.title");
            TxtLoginProfileDesc.Text = LanguageManager.T("login.profile.desc");
            BtnDiscordLogin.Content = _loginInProgress
                ? LanguageManager.T("login.connecting")
                : LanguageManager.T("login.button");
            TxtLoginStatus.Text = _loginInProgress
                ? LanguageManager.T("login.status.connecting")
                : LanguageManager.T("login.status.ready");
            TxtLoginNote.Text = LanguageManager.T("login.note");
            TxtLoginConsoleLabel.Text = LanguageManager.T("login.console.label");
            TxtLoginConsoleTitle.Text = LanguageManager.T("login.console.title");
            TxtLoginUnlocksLabel.Text = LanguageManager.T("login.unlocks.label");
            TxtLoginUnlocksDesc.Text = LanguageManager.T("login.unlocks.desc");
            TxtLoginDashboardLabel.Text = LanguageManager.T("login.stat.dashboard.label");
            TxtLoginDashboardValue.Text = LanguageManager.T("login.stat.dashboard.value");
            TxtLoginSessionCardLabel.Text = LanguageManager.T("login.stat.session.label");
            TxtLoginSessionCardValue.Text = LanguageManager.T("login.stat.session.value");
            TxtLoginFooterNote.Text = LanguageManager.T("login.footer.note");

            TxtSettingsTitle.Text = LanguageManager.T("settings.title");
            TxtSettingsSub.Text = LanguageManager.T("settings.subtitle");
            TxtSettingsAccountLabel.Text = LanguageManager.T("settings.account");
            BtnSwitchAccount.Content = LanguageManager.T("settings.switch_account");
            BtnLogout.Content = LanguageManager.T("settings.logout");
            TxtSettingsPresenceLabel.Text = LanguageManager.T("settings.presence");
            TxtSettingsPresenceDesc.Text = LanguageManager.T("settings.presence_desc");
            TxtSettingsPinLabel.Text = LanguageManager.T("settings.pin");
            TxtSettingsPinDesc.Text = LanguageManager.T("settings.pin_desc");
            TxtSettingsWindowLabel.Text = LanguageManager.T("settings.window_mode");
            TxtSettingsWindowDesc.Text = LanguageManager.T("settings.window_mode_desc");
            TxtSettingsCurrentLanguage.Text = LanguageManager.T("settings.current_lang");
            TxtSettingsCurrentLanguageValue.Text = GetCurrentLanguageDisplayName();
            TxtSettingsLanguageHint.Text = LanguageManager.T("settings.current_lang_hint");
            TxtSettingsUpdateTitle.Text = LanguageManager.T("settings.update_title");
            TxtSettingsUpdateDesc.Text = LanguageManager.T("settings.update_desc");
            BtnCheckUpdates.Content = LanguageManager.T("settings.update_check");
            BtnOpenReleasePage.Content = LanguageManager.T("settings.update_open");
            TxtSettingsVersionLabel.Text = LanguageManager.T("settings.update_version");
            TxtSettingsVersionValue.Text = GitHubUpdateService.CurrentVersionDisplay;
            TxtSettingsAutoCheckLabel.Text = LanguageManager.T("settings.update_auto");
            if (!_updateBusy)
            {
                TxtSettingsUpdateStatus.Text = !UpdateChannelConfig.IsConfigured
                    ? LanguageManager.T("settings.update_status.not_configured")
                    : _latestReleaseInfo != null
                        ? string.Format(LanguageManager.T("settings.update_status.available"), _latestReleaseInfo.TagName)
                        : LanguageManager.T("settings.update_status.ready");
            }
            TxtSettingsCloseHint.Text = LanguageManager.T("settings.close_hint");

            SyncLanguageSelector();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                LanguageManager.LocalizeTree(this);
                SetLanguageSelectorOptions();
                SyncLanguageSelector();
                TxtDiscordUserName.Text = _isAuthorized && !string.IsNullOrWhiteSpace(_discordDisplayName)
                    ? _discordDisplayName
                    : LanguageManager.T("session.user_none");

                if (!_isAuthorized)
                    AvatarFallbackText.Text = "?";
            }));

            UpdateRichPresenceActivity();
        }

        private void MainFrame_Navigated(object? sender, NavigationEventArgs e)
        {
            if (e.Content is DependencyObject tree)
                LanguageManager.LocalizeTree(tree);
        }

        private void StartLoop()
        {
            var token = _cts.Token;
            Task.Run(async () =>
            {
                int presenceTick = 0;
                while (!token.IsCancellationRequested)
                {
                    int delayMs = _isWindowMinimized ? 4200 : (_isWindowActive ? 2000 : 3000);
                    try
                    {
                        if (_isWindowMinimized)
                        {
                            if (_richPresenceEnabled && ++presenceTick % 3 == 0)
                                _ = Dispatcher.BeginInvoke(new Action(UpdateRichPresenceActivity));

                            await Task.Delay(delayMs, token);
                            continue;
                        }

                        float cpu = _cpu?.NextValue() ?? 0f;

                        var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
                        GlobalMemoryStatusEx(ref mem);
                        float ram = mem.dwMemoryLoad;

                        _ = Dispatcher.BeginInvoke(new Action(() =>
                        {
                            HeaderCpu.Text = $"{cpu:F1}%";
                            HeaderRam.Text = $"{ram:F1}%";
                        }));

                        if (_richPresenceEnabled && ++presenceTick % 5 == 0)
                            _ = Dispatcher.BeginInvoke(new Action(UpdateRichPresenceActivity));
                    }
                    catch { }

                    await Task.Delay(delayMs, token);
                }
            }, token);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            else
                DragMove();
        }

        private void BtnMin_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void BtnMax_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        private void UpdateMaximizeGlyph()
        {
            if (BtnMax.Content is not TextBlock glyph)
                return;

            glyph.Text = WindowState == WindowState.Maximized
                ? "\uE923"
                : "\uE922";
        }

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (!_uiReady || MainFrame == null)
                return;

            if (sender is not RadioButton rb || rb.Tag is not string tag)
                return;

            NavigateTo(tag);
        }

        private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_uiReady)
                return;

            if (LanguageSelector.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
                return;

            LanguageSelector.Tag = item.Content?.ToString() ?? string.Empty;
            LanguageManager.SetLanguage(tag == "tr" ? UiLanguage.Turkish : UiLanguage.English);
            TxtSettingsCurrentLanguageValue.Text = GetCurrentLanguageDisplayName();
            SaveSettings();

            if (NavDashboard.IsChecked == true)
                NavigateTo("dashboard");
            else if (NavQuickBoost.IsChecked == true)
                NavigateTo("quickboost");
            else if (NavOptimizer.IsChecked == true)
                NavigateTo("optimizer");
            else if (NavGameTuner.IsChecked == true)
                NavigateTo("tuner");
            else if (NavNetwork.IsChecked == true)
                NavigateTo("network");
            else if (NavPerformance.IsChecked == true)
                NavigateTo("performance");
            else if (NavBenchmark.IsChecked == true)
                NavigateTo("benchmark");

            UpdateRichPresenceActivity();
        }

        private async void BtnDiscordLogin_Click(object sender, RoutedEventArgs e)
            => await BeginDiscordSignInAsync();

        private async Task<bool> BeginDiscordSignInAsync()
        {
            if (_loginInProgress)
                return false;

            _loginInProgress = true;
            BtnDiscordLogin.IsEnabled = false;
            BtnDiscordLogin.Content = LanguageManager.T("login.connecting");
            SetLoginBusyState(true, "login.status.connecting");
            BtnSwitchAccount.IsEnabled = false;
            BtnLogout.IsEnabled = false;

            DiscordAuthResult result;
            try
            {
                result = await DiscordAuthService.SignInAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                result = new DiscordAuthResult
                {
                    ErrorKey = "login.err.generic",
                    ErrorDetail = ex.Message
                };
            }

            _loginInProgress = false;
            BtnDiscordLogin.IsEnabled = true;
            BtnDiscordLogin.Content = LanguageManager.T("login.button");
            BtnSwitchAccount.IsEnabled = true;
            BtnLogout.IsEnabled = true;

            if (!result.Success || result.Profile == null)
            {
                string message = !string.IsNullOrWhiteSpace(result.ErrorKey)
                    ? LanguageManager.T(result.ErrorKey)
                    : LanguageManager.T("login.err.generic");

                if (!string.IsNullOrWhiteSpace(result.ErrorDetail))
                    message += Environment.NewLine + Environment.NewLine + result.ErrorDetail;

                SetLoginBusyState(false, "login.status.failed");
                MessageBox.Show(message, "Zantes Tweak", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            SetLoginBusyState(false, "login.status.success");
            ApplyAuthorizedSession(result.Profile, hideOverlayWithFade: true);
            return true;
        }

        private void NavigateTo(string tag)
        {
            if (MainFrame == null)
            {
                _currentNavTag = tag;
                return;
            }

            if (_isNavigating)
                return;

            Page? next;
            try
            {
                next = CreatePageByTag(tag);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Page load failed: {ex.Message}",
                    "Zantes Tweak",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (next == null)
                return;

            _currentNavTag = tag;
            _isNavigating = true;

            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(110),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            fadeOut.Completed += (_, _) =>
            {
                try
                {
                    MainFrame.Navigate(next);
                }
                catch (Exception ex)
                {
                    _isNavigating = false;
                    MainFrame.Opacity = 1;
                    MessageBox.Show(
                        $"Page load failed: {ex.Message}",
                        "Zantes Tweak",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var fadeIn = new DoubleAnimation
                {
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(170),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                fadeIn.Completed += (_, _) =>
                {
                    _isNavigating = false;
                    UpdateRichPresenceActivity();
                };
                MainFrame.BeginAnimation(OpacityProperty, fadeIn);
            };

            MainFrame.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void MainFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            _isNavigating = false;
            MainFrame.Opacity = 1;
            e.Handled = true;

            MessageBox.Show(
                $"Page navigation failed: {e.Exception.Message}",
                "Zantes Tweak",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private static Page? CreatePageByTag(string tag)
            => tag switch
            {
                "dashboard" => new Pages.Dashboard(),
                "quickboost" => new Pages.QuickBoostPage(),
                "optimizer" => new Pages.OptimizerPage(),
                "tuner" => new Pages.GameTunerPage(),
                "network" => new Pages.NetworkPage(),
                "performance" => new Pages.PerformancePage(),
                "benchmark" => new Pages.BenchmarkPage(),
                _ => null
            };

        private async Task RestoreDiscordSessionAsync()
        {
            DiscordAuthResult result;
            try
            {
                result = await DiscordAuthService.TryRestoreSessionAsync(CancellationToken.None);
            }
            catch
            {
                return;
            }

            if (!result.Success || result.Profile == null)
                return;

            Dispatcher.Invoke(() => ApplyAuthorizedSession(result.Profile, hideOverlayWithFade: false));
        }

        private void ApplyAuthorizedSession(DiscordUserProfile profile, bool hideOverlayWithFade)
        {
            _isAuthorized = true;
            _discordDisplayName = profile.DisplayName;
            SetLoginBusyState(false, "login.status.success");

            MainFrame.IsEnabled = true;
            SidebarPanel.IsEnabled = true;

            AuthStatusText.Text = LanguageManager.T("session.unlocked");
            TxtDiscordUserName.Text = profile.DisplayName;
            SetAvatarPlaceholder(profile.DisplayName);
            _ = SetAvatarAsync(profile.AvatarUrl, profile.FallbackAvatarUrl);
            HideSettingsOverlay(animated: false);
            UpdateRichPresenceActivity();
            EnsureFirstRunQuickBoostLanding();

            if (!hideOverlayWithFade)
            {
                LoginOverlay.Opacity = 0;
                LoginOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            var fade = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(280),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var shrink = new DoubleAnimation
            {
                To = 1.02,
                Duration = TimeSpan.FromMilliseconds(280),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            fade.Completed += (_, _) => LoginOverlay.Visibility = Visibility.Collapsed;
            LoginOverlay.BeginAnimation(OpacityProperty, fade);
            LoginOverlayScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, shrink);
            LoginOverlayScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, shrink);
        }

        private void ApplyStoredSettings()
        {
            var settings = AppSettingsService.Load();
            _richPresenceEnabled = settings.RichPresenceEnabled;
            _launchMaximized = settings.LaunchMaximized;
            _autoCheckUpdates = settings.AutoCheckUpdates;
            Topmost = settings.AlwaysOnTop;
            AlwaysOnTopToggle.IsChecked = settings.AlwaysOnTop;
            RichPresenceToggle.IsChecked = _richPresenceEnabled;
            LaunchMaximizedToggle.IsChecked = _launchMaximized;
            AutoCheckUpdatesToggle.IsChecked = _autoCheckUpdates;
            WindowState = _launchMaximized ? WindowState.Maximized : WindowState.Normal;
            UpdateMaximizeGlyph();

            UiLanguage preferredLanguage = settings.PreferredLanguage.Equals("tr", StringComparison.OrdinalIgnoreCase)
                ? UiLanguage.Turkish
                : UiLanguage.English;
            LanguageManager.SetLanguage(preferredLanguage);
            SyncLanguageSelector();
        }

        private void SaveSettings()
        {
            var existing = AppSettingsService.Load();
            AppSettingsService.Save(new AppSettings
            {
                RichPresenceEnabled = _richPresenceEnabled,
                AlwaysOnTop = Topmost,
                LaunchMaximized = _launchMaximized,
                AutoCheckUpdates = _autoCheckUpdates,
                PreferredLanguage = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "tr" : "en",
                HasSeenFirstBoot = existing.HasSeenFirstBoot,
                HasSeenQuickBoostLanding = existing.HasSeenQuickBoostLanding
            });
        }

        private void EnsureFirstRunQuickBoostLanding()
        {
            var settings = AppSettingsService.Load();
            if (settings.HasSeenQuickBoostLanding)
                return;

            settings.HasSeenQuickBoostLanding = true;
            AppSettingsService.Save(settings);

            NavQuickBoost.IsChecked = true;
            NavigateTo("quickboost");
        }

        private void EnsureRichPresenceService()
        {
            _richPresence ??= new DiscordRichPresenceService(_discordRpcAppId);
        }

        private void UpdateRichPresenceActivity()
        {
            if (!_richPresenceEnabled)
                return;

            EnsureRichPresenceService();
            if (_richPresence == null)
                return;

            string pageLabel = LanguageManager.T(GetPresencePageKey());
            string languageShort = GetPresenceLanguageShort();
            string details = _isAuthorized
                ? string.Format(LanguageManager.T("presence.details.auth"), pageLabel)
                : string.Format(LanguageManager.T("presence.details.guest"), pageLabel);
            string state = _isAuthorized
                ? string.Format(LanguageManager.T("presence.state.auth"), GetPresenceUserLabel(), pageLabel, languageShort)
                : string.Format(LanguageManager.T("presence.state.guest"), pageLabel, languageShort);
            var activity = new DiscordRichPresenceActivity
            {
                Details = details,
                State = state,
                LargeImageKey = "zantes_banner",
                LargeImageText = string.Format(LanguageManager.T("presence.large_text.page"), pageLabel),
                SmallImageKey = _isAuthorized ? "discord_online" : "discord_idle",
                SmallImageText = _isAuthorized
                    ? string.Format(LanguageManager.T("presence.small.online_user"), GetPresenceUserLabel(), languageShort)
                    : string.Format(LanguageManager.T("presence.small.offline_lang"), languageShort),
                StartTimestampUnix = _presenceStartUnix
            };

            _richPresence.SetActivity(activity);
        }

        private string GetPresencePageKey()
            => _currentNavTag switch
            {
                "dashboard" => "presence.page.dashboard",
                "quickboost" => "presence.page.quickboost",
                "optimizer" => "presence.page.optimizer",
                "tuner" => "presence.page.tuner",
                "network" => "presence.page.network",
                "performance" => "presence.page.performance",
                "benchmark" => "presence.page.benchmark",
                _ => "presence.page.dashboard"
            };

        private string GetPresenceUserLabel()
            => _isAuthorized && !string.IsNullOrWhiteSpace(_discordDisplayName)
                ? _discordDisplayName
                : LanguageManager.T("presence.user.guest");

        private string GetPresenceLanguageShort()
            => LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "TR" : "EN";

        private string GetCurrentLanguageDisplayName()
            => LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Turkish" : "English";

        private void SetLanguageSelectorOptions()
        {
            LangEnItem.Content = "English";
            LangTrItem.Content = "Turkish";
        }

        private void SyncLanguageSelector()
        {
            string expectedTag = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "tr" : "en";
            foreach (var raw in LanguageSelector.Items)
            {
                if (raw is ComboBoxItem item && item.Tag is string tag && tag == expectedTag)
                {
                    LanguageSelector.SelectedItem = item;
                    LanguageSelector.Tag = item.Content?.ToString() ?? string.Empty;
                    break;
                }
            }
        }

        private void ResetToLockedSession()
        {
            _isAuthorized = false;
            _discordDisplayName = string.Empty;
            MainFrame.IsEnabled = false;
            SidebarPanel.IsEnabled = false;

            AuthStatusText.Text = LanguageManager.T("session.locked");
            TxtDiscordUserName.Text = LanguageManager.T("session.user_none");
            SetAvatarPlaceholder(string.Empty);

            ShowLoginOverlay();
            HideSettingsOverlay(animated: false);
            UpdateRichPresenceActivity();
        }

        private void SetLoginBusyState(bool busy, string statusKey)
        {
            TxtLoginStatus.Text = LanguageManager.T(statusKey);
            LoginBusyPanel.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            LoginBusyProgress.IsIndeterminate = busy;
            LoginBusyProgress.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowLoginOverlay()
        {
            LoginOverlay.Visibility = Visibility.Visible;
            LoginOverlay.Opacity = 0;
            LoginOverlayScale.ScaleX = 0.985;
            LoginOverlayScale.ScaleY = 0.985;
            SetLoginBusyState(false, "login.status.ready");

            var fadeIn = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var scaleIn = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            LoginOverlay.BeginAnimation(OpacityProperty, fadeIn);
            LoginOverlayScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleIn);
            LoginOverlayScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleIn);
        }

        private void ShowSettingsOverlay()
        {
            if (SettingsOverlay.Visibility == Visibility.Visible)
                return;

            SettingsOverlay.Visibility = Visibility.Visible;
            SettingsOverlay.Opacity = 0;
            SettingsOverlay.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(190),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        private void HideSettingsOverlay(bool animated)
        {
            if (SettingsOverlay.Visibility != Visibility.Visible)
                return;

            if (!animated)
            {
                SettingsOverlay.Opacity = 0;
                SettingsOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            var fade = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(160),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            fade.Completed += (_, _) => SettingsOverlay.Visibility = Visibility.Collapsed;
            SettingsOverlay.BeginAnimation(OpacityProperty, fade);
        }

        private void BtnOpenSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAuthorized)
                return;

            ShowSettingsOverlay();
        }

        private void BtnCloseSettings_Click(object sender, RoutedEventArgs e)
            => HideSettingsOverlay(animated: true);

        private void SettingsOverlay_MouseDown(object sender, MouseButtonEventArgs e)
            => HideSettingsOverlay(animated: true);

        private void SettingsPanel_MouseDown(object sender, MouseButtonEventArgs e)
            => e.Handled = true;

        private void RichPresenceToggle_Click(object sender, RoutedEventArgs e)
        {
            _richPresenceEnabled = RichPresenceToggle.IsChecked == true;
            SaveSettings();

            if (_richPresenceEnabled)
            {
                UpdateRichPresenceActivity();
            }
            else
            {
                _richPresence?.ClearActivity();
                _richPresence?.Disconnect();
            }
        }

        private void AlwaysOnTopToggle_Click(object sender, RoutedEventArgs e)
        {
            Topmost = AlwaysOnTopToggle.IsChecked == true;
            SaveSettings();
        }

        private void LaunchMaximizedToggle_Click(object sender, RoutedEventArgs e)
        {
            _launchMaximized = LaunchMaximizedToggle.IsChecked == true;
            SaveSettings();
        }

        private void AutoCheckUpdatesToggle_Click(object sender, RoutedEventArgs e)
        {
            _autoCheckUpdates = AutoCheckUpdatesToggle.IsChecked == true;
            SaveSettings();
        }

        private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
            => await CheckForUpdatesAsync(userInitiated: true);

        private void BtnOpenReleasePage_Click(object sender, RoutedEventArgs e)
        {
            string? url = _latestReleaseInfo?.HtmlUrl;
            if (!GitHubUpdateService.TryOpenReleasePage(url))
            {
                MessageBox.Show(
                    LanguageManager.T("settings.update_status.failed"),
                    "Zantes Tweak",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void BtnSwitchAccount_Click(object sender, RoutedEventArgs e)
        {
            if (_loginInProgress)
                return;

            DiscordAuthService.SignOut();
            ResetToLockedSession();
            await BeginDiscordSignInAsync();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (_loginInProgress)
                return;

            DiscordAuthService.SignOut();
            ResetToLockedSession();
        }

        private void SetAvatarPlaceholder(string displayName)
        {
            char c = '?';
            if (!string.IsNullOrWhiteSpace(displayName))
                c = char.ToUpperInvariant(displayName.Trim()[0]);

            AvatarFallbackText.Text = c.ToString();
            AvatarFallbackText.Visibility = Visibility.Visible;
            DiscordAvatar.Fill = new SolidColorBrush(Color.FromRgb(30, 42, 58));
            SessionBackgroundVisual.Fill = _defaultSessionBackgroundFill?.CloneCurrentValue() as Brush
                ?? new SolidColorBrush(Color.FromRgb(18, 31, 44));
            SessionBackgroundVisual.Opacity = 0.24;
        }

        private async Task SetAvatarAsync(string avatarUrl, string fallbackUrl)
        {
            string[] urls = new[] { avatarUrl, fallbackUrl };
            foreach (string url in urls)
            {
                if (string.IsNullOrWhiteSpace(url))
                    continue;

                var brush = await TryCreateAvatarBrushAsync(url);
                if (brush == null)
                    continue;

                Dispatcher.Invoke(() =>
                {
                    DiscordAvatar.Fill = brush;
                    SessionBackgroundVisual.Fill = brush;
                    SessionBackgroundVisual.Opacity = 0.34;
                    AvatarFallbackText.Visibility = Visibility.Collapsed;
                });
                return;
            }
        }

        private static async Task<ImageBrush?> TryCreateAvatarBrushAsync(string url)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                using var res = await AvatarHttp.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                if (!res.IsSuccessStatusCode)
                    return null;

                await using var netStream = await res.Content.ReadAsStreamAsync();
                await using var mem = new MemoryStream();
                await netStream.CopyToAsync(mem);
                mem.Position = 0;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = mem;
                bitmap.EndInit();
                bitmap.Freeze();

                return new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
            }
            catch
            {
                return null;
            }
        }

        private static HttpClient BuildAvatarHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(12)
            };
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ZantesTweak/1.0");
            return client;
        }

        private void SpawnAmbientParticles()
        {
            if (AmbientCanvas.Children.Count > 0)
                return;

            double width = Math.Max(ActualWidth, SystemParameters.PrimaryScreenWidth);
            double height = Math.Max(ActualHeight, SystemParameters.PrimaryScreenHeight);
            int particleCount = (int)Math.Round((width * height) / (1920d * 1080d) * 32d);
            particleCount = Math.Clamp(particleCount, 24, 40);

            for (int i = 0; i < particleCount; i++)
            {
                double size = _ambientRnd.NextDouble() * 2.6 + 0.8;
                double x = _ambientRnd.NextDouble() * width;
                double y = _ambientRnd.NextDouble() * height;
                double delay = _ambientRnd.NextDouble() * 2.5;
                double drift = _ambientRnd.NextDouble() * 90 + 45;
                double duration = _ambientRnd.NextDouble() * 6 + 5;
                byte alpha = (byte)_ambientRnd.Next(40, 150);

                var dot = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(Color.FromArgb(alpha, 140, 96, 255)),
                    Opacity = 0
                };

                Canvas.SetLeft(dot, x);
                Canvas.SetTop(dot, y);
                AmbientCanvas.Children.Add(dot);

                var fade = new DoubleAnimationUsingKeyFrames
                {
                    BeginTime = TimeSpan.FromSeconds(delay),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                fade.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
                fade.KeyFrames.Add(new EasingDoubleKeyFrame(0.9, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(duration * 0.35))));
                fade.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(duration))));
                dot.BeginAnimation(OpacityProperty, fade);

                var shift = new TranslateTransform();
                dot.RenderTransform = shift;
                shift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation
                {
                    From = 0,
                    To = -drift,
                    Duration = TimeSpan.FromSeconds(duration),
                    BeginTime = TimeSpan.FromSeconds(delay),
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                });
            }
        }

        private static string ResolveDiscordRpcAppId()
        {
            string fromEnv = Environment.GetEnvironmentVariable("ZANTES_DISCORD_RPC_APP_ID") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(fromEnv))
                return fromEnv.Trim();

            return DiscordApplicationIdDefault;
        }

        private async Task CheckForUpdatesAsync(bool userInitiated)
        {
            if (_updateBusy)
                return;

            if (!_autoCheckUpdates && !userInitiated)
                return;

            _updateBusy = true;
            BtnCheckUpdates.IsEnabled = false;
            TxtSettingsUpdateStatus.Text = LanguageManager.T("settings.update_status.checking");

            UpdateCheckResult result = await GitHubUpdateService.CheckLatestReleaseAsync(CancellationToken.None);
            _latestReleaseInfo = result.Release;

            switch (result.State)
            {
                case UpdateCheckState.NotConfigured:
                    TxtSettingsUpdateStatus.Text = LanguageManager.T("settings.update_status.not_configured");
                    break;

                case UpdateCheckState.UpToDate:
                    TxtSettingsUpdateStatus.Text = LanguageManager.T("settings.update_status.uptodate");
                    if (userInitiated)
                    {
                        MessageBox.Show(
                            LanguageManager.T("settings.update_dialog.none"),
                            "Zantes Tweak",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    break;

                case UpdateCheckState.UpdateAvailable:
                    TxtSettingsUpdateStatus.Text = string.Format(
                        LanguageManager.T("settings.update_status.available"),
                        result.Release?.TagName ?? result.Release?.Version.ToString(3) ?? string.Empty);
                    if (userInitiated)
                    {
                        var open = MessageBox.Show(
                            string.Format(
                                "{0}{1}{1}{2}",
                                TxtSettingsUpdateStatus.Text,
                                Environment.NewLine,
                                LanguageManager.T("settings.update_dialog.available")),
                            "Zantes Tweak",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (open == MessageBoxResult.Yes)
                            GitHubUpdateService.TryOpenReleasePage(result.Release?.HtmlUrl);
                    }
                    break;

                default:
                    TxtSettingsUpdateStatus.Text = string.IsNullOrWhiteSpace(result.ErrorDetail)
                        ? LanguageManager.T("settings.update_status.failed")
                        : $"{LanguageManager.T("settings.update_status.failed")} {result.ErrorDetail}";
                    break;
            }

            BtnCheckUpdates.IsEnabled = true;
            _updateBusy = false;
        }
    }
}
