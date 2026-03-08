using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZantesEngine.Services;

namespace ZantesEngine.Pages
{
    public partial class OptimizerPage : Page
    {
        private readonly Dictionary<CheckBox, string> _toggleMap = new();
        private static readonly string[] OneClickMaxFpsFallbackKeys =
        {
            "power_high_performance",
            "disable_startup_delay",
            "enable_taskbar_end_task",
            "disable_menu_show_delay",
            "disable_window_animations",
            "disable_widgets",
            "disable_copilot",
            "disable_start_recommendations",
            "disable_windows_spotlight",
            "disable_advertising_id",
            "ntfs_disable_8dot3",
            "disable_fast_startup",
            "cpu_maximum_state_100",
            "cpu_minimum_state_100",
            "cpu_core_parking_off",
            "visualfx_performance",
            "disable_remote_assistance",
            "disable_mouse_accel",
            "enable_game_mode",
            "disable_game_dvr",
            "disable_xbox_gamebar",
            "hw_scheduling",
            "disable_network_throttling",
            "mmcss_system_responsiveness",
            "mmcss_games_task_profile",
            "usb_selective_suspend_off",
            "disable_mpo",
            "tcp_autotune",
            "network_rss",
            "tcp_timestamps_disabled",
            "tcp_ecn_disabled",
            "tcp_heuristics_disabled",
            "tcp_chimney_disabled",
            "tcp_rsc_disabled",
            "disable_nagle_algorithm",
            "disable_qos_reserved_bandwidth",
            "disable_delivery_opt",
            "disable_diagtrack_service",
            "disable_windows_error_reporting",
            "disable_edge_background_mode",
            "disable_phone_service",
            "disable_consumer_features",
            "disable_remote_registry",
            "disable_storage_sense",
            "disable_onedrive_startup",
            "disable_teams_autostart",
            "nvidia_disable_telemetry",
            "disable_windows_tips",
            "disable_feedback_notifications",
            "disable_activity_history",
            "disable_clipboard_history",
            "disable_search_web_results",
            "disable_powershell_telemetry",
            "disable_background_apps",
            "disable_transparency",
            "prefer_ipv4_over_ipv6",
            "nvidia_clean_shader_cache",
            "clear_directx_shader_cache"
        };
        private bool _busy;
        private int _toastVersion;

        public OptimizerPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_toggleMap.Count == 0)
                BindToggleMap();

            AdminStatusText.Text = SystemTweakEngine.IsAdministrator()
                ? LanguageManager.T("optimizer.status.admin_active")
                : LanguageManager.T("optimizer.status.admin_needed");

            UpdateSelectionState();
            LanguageManager.LanguageChanged += OnLanguageChanged;
            OnLanguageChanged();
            AppendOutput(LanguageManager.T("optimizer.status.ready"));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
            => LanguageManager.LanguageChanged -= OnLanguageChanged;

        private void OnLanguageChanged()
            => Dispatcher.BeginInvoke(new Action(() =>
            {
                LanguageManager.LocalizeTree(this);
                AdminStatusText.Text = SystemTweakEngine.IsAdministrator()
                    ? LanguageManager.T("optimizer.status.admin_active")
                    : LanguageManager.T("optimizer.status.admin_needed");
                BtnOneClickMaxFps.Content = LanguageManager.T("optimizer.btn.one_click");
                UpdateSelectionState();
            }));

        private void BindToggleMap()
        {
            _toggleMap[CbPowerHighPerformance] = "power_high_performance";
            _toggleMap[CbDisableHibernate] = "disable_hibernate";
            _toggleMap[CbDisableStartupDelay] = "disable_startup_delay";
            _toggleMap[CbDisableFastStartup] = "disable_fast_startup";
            _toggleMap[CbDisableRemoteAssistance] = "disable_remote_assistance";
            _toggleMap[CbCpuMinimumState100] = "cpu_minimum_state_100";
            _toggleMap[CbCpuMaximumState100] = "cpu_maximum_state_100";
            _toggleMap[CbCpuCoreParkingOff] = "cpu_core_parking_off";
            _toggleMap[CbVisualFxPerformance] = "visualfx_performance";
            _toggleMap[CbDisablePowerThrottling] = "disable_power_throttling";
            _toggleMap[CbNtfsDisableLastAccess] = "ntfs_disable_last_access";
            _toggleMap[CbNtfsDisable8dot3] = "ntfs_disable_8dot3";
            _toggleMap[CbDisableMemoryCompression] = "disable_memory_compression";
            _toggleMap[CbDisableStartRecommendations] = "disable_start_recommendations";
            _toggleMap[CbDisableWindowsSpotlight] = "disable_windows_spotlight";
            _toggleMap[CbDisableAdvertisingId] = "disable_advertising_id";
            _toggleMap[CbDisableMenuShowDelay] = "disable_menu_show_delay";
            _toggleMap[CbDisableWindowAnimations] = "disable_window_animations";

            _toggleMap[CbDisableMouseAccel] = "disable_mouse_accel";
            _toggleMap[CbEnableGameMode] = "enable_game_mode";
            _toggleMap[CbDisableGameDvr] = "disable_game_dvr";
            _toggleMap[CbDisableXboxGamebar] = "disable_xbox_gamebar";
            _toggleMap[CbDisableFullscreenOptimizations] = "disable_fullscreen_optimizations";
            _toggleMap[CbHwScheduling] = "hw_scheduling";
            _toggleMap[CbPrioritySeparation] = "priority_separation";
            _toggleMap[CbDisableStickyKeys] = "disable_sticky_keys_shortcut";
            _toggleMap[CbDisableNetworkThrottling] = "disable_network_throttling";
            _toggleMap[CbMmcssSystemResponsiveness] = "mmcss_system_responsiveness";
            _toggleMap[CbMmcssGamesTaskProfile] = "mmcss_games_task_profile";
            _toggleMap[CbUsbSelectiveSuspendOff] = "usb_selective_suspend_off";
            _toggleMap[CbDisableMpo] = "disable_mpo";

            _toggleMap[CbTcpAutoTune] = "tcp_autotune";
            _toggleMap[CbNetworkRss] = "network_rss";
            _toggleMap[CbTcpTimestampsDisabled] = "tcp_timestamps_disabled";
            _toggleMap[CbTcpEcnDisabled] = "tcp_ecn_disabled";
            _toggleMap[CbTcpHeuristicsDisabled] = "tcp_heuristics_disabled";
            _toggleMap[CbTcpNonsackRttResiliencyDisabled] = "tcp_nonsack_rtt_resiliency_disabled";
            _toggleMap[CbTcpInitialRto2000] = "tcp_initial_rto_2000";
            _toggleMap[CbTcpChimneyDisabled] = "tcp_chimney_disabled";
            _toggleMap[CbTcpRscDisabled] = "tcp_rsc_disabled";
            _toggleMap[CbDisableNagleAlgorithm] = "disable_nagle_algorithm";
            _toggleMap[CbDisableQosReservedBandwidth] = "disable_qos_reserved_bandwidth";
            _toggleMap[CbDnsFlush] = "dns_flush";
            _toggleMap[CbDisableDeliveryOpt] = "disable_delivery_opt";
            _toggleMap[CbWinsockReset] = "winsock_reset";

            _toggleMap[CbDisableTelemetry] = "disable_telemetry";
            _toggleMap[CbDisableSysMain] = "disable_sysmain";
            _toggleMap[CbDisableSearchIndex] = "disable_search_index";
            _toggleMap[CbDisableWindowsTips] = "disable_windows_tips";
            _toggleMap[CbDisableFeedback] = "disable_feedback_notifications";
            _toggleMap[CbDisableActivityHistory] = "disable_activity_history";
            _toggleMap[CbDisableClipboardHistory] = "disable_clipboard_history";
            _toggleMap[CbDisableSearchWebResults] = "disable_search_web_results";
            _toggleMap[CbDisableBackgroundApps] = "disable_background_apps";
            _toggleMap[CbDisableXboxServices] = "disable_xbox_services";
            _toggleMap[CbDisableDiagTrackService] = "disable_diagtrack_service";
            _toggleMap[CbDisableWindowsErrorReporting] = "disable_windows_error_reporting";
            _toggleMap[CbNvidiaDisableTelemetry] = "nvidia_disable_telemetry";
            _toggleMap[CbDisableMapsBroker] = "disable_maps_broker";
            _toggleMap[CbDisableLocationService] = "disable_location_service";
            _toggleMap[CbDisablePrintSpooler] = "disable_print_spooler";
            _toggleMap[CbDisablePcaService] = "disable_pca_service";
            _toggleMap[CbDisableFaxService] = "disable_fax_service";
            _toggleMap[CbDisableTransparency] = "disable_transparency";
            _toggleMap[CbDisableWidgets] = "disable_widgets";
            _toggleMap[CbDisableCopilot] = "disable_copilot";
            _toggleMap[CbDisableConsumerFeatures] = "disable_consumer_features";
            _toggleMap[CbDisableEdgeBackgroundMode] = "disable_edge_background_mode";
            _toggleMap[CbDisablePhoneService] = "disable_phone_service";
            _toggleMap[CbDisableRemoteRegistry] = "disable_remote_registry";
            _toggleMap[CbDisableSsdpUpnpServices] = "disable_ssdp_upnp_services";
            _toggleMap[CbDisableAutoMaintenance] = "disable_auto_maintenance";
            _toggleMap[CbDisableStorageSense] = "disable_storage_sense";
            _toggleMap[CbDisableOneDriveStartup] = "disable_onedrive_startup";
            _toggleMap[CbDisableTeamsAutoStart] = "disable_teams_autostart";

            _toggleMap[CbCleanTemp] = "clean_temp";
            _toggleMap[CbNvidiaShaderCacheClean] = "nvidia_clean_shader_cache";
            _toggleMap[CbDirectXShaderCacheClean] = "clear_directx_shader_cache";
            _toggleMap[CbCleanWindowsUpdateCache] = "clean_windows_update_cache";
            _toggleMap[CbCleanThumbnailCache] = "clean_thumbnail_cache";
            _toggleMap[CbFiveMCacheCleanup] = "fivem_cache_cleanup";
            _toggleMap[CbFiveMLogsCleanup] = "fivem_logs_cleanup";
            _toggleMap[CbFiveMNuiStorageReset] = "fivem_nui_storage_reset";
            _toggleMap[CbClearPrefetch] = "clear_prefetch";
        }

        private IEnumerable<SystemTweakDefinition> GetSelectedTweaks()
        {
            foreach (var pair in _toggleMap)
            {
                if (pair.Key.IsChecked == true)
                {
                    var tweak = SystemTweakCatalog.Get(pair.Value);
                    if (tweak != null)
                        yield return tweak;
                }
            }
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            BtnApplyTweaks.IsEnabled = !busy;
            BtnCreateRestorePoint.IsEnabled = !busy;
            BtnSelectAll.IsEnabled = !busy;
            BtnOneClickMaxFps.IsEnabled = !busy;
        }

        private void UpdateSelectionState()
        {
            int selected = _toggleMap.Keys.Count(cb => cb.IsChecked == true);
            SelectedCountText.Text = selected == 1
                ? LanguageManager.T("optimizer.selected.one")
                : string.Format(LanguageManager.T("optimizer.selected.many"), selected);

            BtnSelectAll.Content = selected == _toggleMap.Count
                ? LanguageManager.T("optimizer.btn.unselect_all")
                : LanguageManager.T("optimizer.btn.select_all");
        }

        private void AppendOutput(string message)
        {
            OutputBox.Text += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
            OutputBox.ScrollToEnd();
        }

        private void ShowActionToast(string title, string body, bool inProgress, bool isError = false)
        {
            _toastVersion++;
            int activeVersion = _toastVersion;

            ActionToastTitle.Text = title;
            ActionToastBody.Text = body;
            ActionToastProgress.Visibility = inProgress ? Visibility.Visible : Visibility.Collapsed;
            ActionToast.Visibility = Visibility.Visible;

            if (inProgress)
            {
                ActionToast.Background = new SolidColorBrush(Color.FromArgb(227, 23, 18, 35));
                ActionToast.BorderBrush = new SolidColorBrush(Color.FromRgb(90, 69, 162));
                return;
            }

            if (isError)
            {
                ActionToast.Background = new SolidColorBrush(Color.FromArgb(232, 52, 18, 24));
                ActionToast.BorderBrush = new SolidColorBrush(Color.FromRgb(166, 70, 86));
            }
            else
            {
                ActionToast.Background = new SolidColorBrush(Color.FromArgb(230, 14, 40, 28));
                ActionToast.BorderBrush = new SolidColorBrush(Color.FromRgb(78, 166, 122));
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(3600);
                Dispatcher.Invoke(() =>
                {
                    if (activeVersion == _toastVersion)
                        ActionToast.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void TweakSelectionChanged(object sender, RoutedEventArgs e)
            => UpdateSelectionState();

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (_busy)
                return;

            bool setAll = _toggleMap.Keys.Any(cb => cb.IsChecked != true);
            foreach (var checkBox in _toggleMap.Keys)
                checkBox.IsChecked = setAll;

            UpdateSelectionState();
        }

        private void Recommended_Click(object sender, RoutedEventArgs e)
        {
            if (_busy)
                return;

            foreach (var checkBox in _toggleMap.Keys)
                checkBox.IsChecked = false;

            foreach (var pair in _toggleMap)
            {
                var tweak = SystemTweakCatalog.Get(pair.Value);
                if (tweak?.Recommended == true)
                    pair.Key.IsChecked = true;
            }

            UpdateSelectionState();
            AppendOutput(LanguageManager.T("optimizer.status.recommended"));
        }

        private async void CreateRestorePoint_Click(object sender, RoutedEventArgs e)
        {
            if (_busy)
                return;

            if (!SystemTweakEngine.IsAdministrator())
            {
                ShowActionToast(
                    LanguageManager.T("common.warning").ToUpperInvariant(),
                    LanguageManager.T("optimizer.msg.restore_need_admin"),
                    inProgress: false,
                    isError: true);
                return;
            }

            SetBusy(true);
            AppendOutput(LanguageManager.T("optimizer.status.restore_creating"));
            ShowActionToast("RESTORE POINT", LanguageManager.T("optimizer.status.restore_creating"), inProgress: true);

            var result = await Task.Run(() =>
                SystemTweakEngine.CreateRestorePoint($"Zantes Tweak {DateTime.Now:yyyy-MM-dd HH:mm:ss}"));

            var localizedResult = LanguageManager.LocalizeLiteral(result.Message);
            AppendOutput(localizedResult);
            ShowActionToast(
                result.Success ? "RESTORE READY" : "RESTORE WARNING",
                localizedResult,
                inProgress: false,
                isError: !result.Success);

            SetBusy(false);
        }

        private async void ApplySelected_Click(object sender, RoutedEventArgs e)
        {
            if (_busy)
                return;

            var selected = GetSelectedTweaks().ToArray();
            if (selected.Length == 0)
            {
                ShowActionToast(
                    LanguageManager.T("common.info").ToUpperInvariant(),
                    LanguageManager.T("optimizer.msg.apply_none"),
                    inProgress: false);
                return;
            }

            if (!SystemTweakEngine.IsAdministrator())
            {
                ShowActionToast(
                    LanguageManager.T("common.warning").ToUpperInvariant(),
                    LanguageManager.T("optimizer.msg.apply_need_admin"),
                    inProgress: false,
                    isError: true);
                return;
            }

            var cautionTweaks = selected.Where(t => t.Risk == TweakRisk.Caution).ToArray();
            if (cautionTweaks.Length > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine(LanguageManager.T("optimizer.msg.caution_header"));
                foreach (var tweak in cautionTweaks.Take(8))
                {
                    sb.Append("- ").Append(LanguageManager.LocalizeLiteral(tweak.Title));
                    if (!string.IsNullOrWhiteSpace(tweak.Warning))
                        sb.Append(" | ").Append(LanguageManager.LocalizeLiteral(tweak.Warning));
                    sb.AppendLine();
                }

                if (cautionTweaks.Length > 8)
                    sb.AppendLine(string.Format(LanguageManager.T("optimizer.msg.caution_and_more"), cautionTweaks.Length - 8));

                sb.AppendLine();
                sb.Append(LanguageManager.T("optimizer.msg.caution_continue"));

                var cautionConfirm = MessageBox.Show(
                    sb.ToString(),
                    $"Zantes Tweak - {LanguageManager.T("common.caution")}",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (cautionConfirm != MessageBoxResult.Yes)
                    return;
            }
            else
            {
                var confirm = MessageBox.Show(
                    string.Format(LanguageManager.T("optimizer.msg.apply_confirm"), selected.Length),
                    "Zantes Tweak",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;
            }

            SetBusy(true);
            ShowActionToast("APPLY ENGINE", string.Format(LanguageManager.T("optimizer.msg.applying"), selected.Length), inProgress: true);

            if (CbAutoRestorePoint.IsChecked == true)
            {
                AppendOutput(LanguageManager.T("optimizer.status.auto_restore_requested"));
                var restore = await Task.Run(() =>
                    SystemTweakEngine.CreateRestorePoint($"Zantes Tweak Auto {DateTime.Now:yyyy-MM-dd HH:mm:ss}"));
                var localizedRestore = LanguageManager.LocalizeLiteral(restore.Message);
                AppendOutput(localizedRestore);
                if (!restore.Success)
                {
                    var proceed = MessageBox.Show(
                        LanguageManager.T("optimizer.msg.apply_restore_failed"),
                        "Zantes Tweak",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (proceed != MessageBoxResult.Yes)
                    {
                        SetBusy(false);
                        return;
                    }
                }
            }

            AppendOutput(string.Format(LanguageManager.T("optimizer.msg.applying"), selected.Length));
            IReadOnlyList<SystemTweakResult> results;
            try
            {
                results = await SystemTweakEngine.ApplyAsync(selected, CancellationToken.None);
            }
            catch (Exception ex)
            {
                AppendOutput(string.Format(LanguageManager.T("optimizer.msg.apply_failed"), ex.Message));
                ShowActionToast("APPLY FAILED", ex.Message, inProgress: false, isError: true);
                SetBusy(false);
                return;
            }

            foreach (var result in results)
            {
                AppendOutput($"[{(result.Success ? "OK" : "FAIL")}] {LanguageManager.LocalizeLiteral(result.Title)}");
                if (!string.IsNullOrWhiteSpace(result.Output))
                    AppendOutput(result.Output);
            }

            int okCount = results.Count(r => r.Success);
            int failCount = results.Count - okCount;
            bool needsRestart = selected.Any(t => t.RequiresRestart);

            var doneText = string.Format(LanguageManager.T("optimizer.msg.apply_done"), okCount, failCount);
            if (needsRestart)
                doneText += $"\n\n{LanguageManager.T("optimizer.msg.restart_recommended")}";

            ShowActionToast(
                failCount == 0 ? "APPLY COMPLETED" : "APPLY COMPLETED WITH WARNINGS",
                doneText,
                inProgress: false,
                isError: failCount > 0);

            SetBusy(false);
        }

        private async void OneClickMaxFps_Click(object sender, RoutedEventArgs e)
        {
            if (_busy)
                return;

            if (!SystemTweakEngine.IsAdministrator())
            {
                ShowActionToast(
                    LanguageManager.T("common.warning").ToUpperInvariant(),
                    LanguageManager.T("optimizer.msg.apply_need_admin"),
                    inProgress: false,
                    isError: true);
                return;
            }

            var plan = SmartOptimizeService.BuildMaxFpsSafePlan();
            var selected = plan.TweakKeys
                .Select(SystemTweakCatalog.Get)
                .Where(t => t != null)
                .Cast<SystemTweakDefinition>()
                .ToArray();

            if (selected.Length == 0)
            {
                selected = OneClickMaxFpsFallbackKeys
                    .Select(SystemTweakCatalog.Get)
                    .Where(t => t != null)
                    .Cast<SystemTweakDefinition>()
                    .ToArray();
            }

            if (selected.Length == 0)
                return;

            foreach (var pair in _toggleMap)
                pair.Key.IsChecked = selected.Any(t => t.Key == pair.Value);
            UpdateSelectionState();

            var confirm = MessageBox.Show(
                string.Format(LanguageManager.T("optimizer.msg.one_click_confirm"), selected.Length),
                "Zantes Tweak",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            SetBusy(true);
            AppendOutput($"[MAX FPS SAFE] {plan.HardwareSummary}");
            AppendOutput($"[MAX FPS SAFE] {plan.Reason}");
            AppendOutput(LanguageManager.T("optimizer.status.auto_restore_requested"));
            ShowActionToast("ONE-CLICK MAX FPS", string.Format(LanguageManager.T("optimizer.msg.applying"), selected.Length), inProgress: true);

            var restore = await Task.Run(() =>
                SystemTweakEngine.CreateRestorePoint($"Zantes Tweak Max FPS {DateTime.Now:yyyy-MM-dd HH:mm:ss}"));
            var localizedRestore = LanguageManager.LocalizeLiteral(restore.Message);
            AppendOutput(localizedRestore);
            if (!restore.Success)
            {
                var proceed = MessageBox.Show(
                    LanguageManager.T("optimizer.msg.apply_restore_failed"),
                    "Zantes Tweak",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (proceed != MessageBoxResult.Yes)
                {
                    SetBusy(false);
                    return;
                }
            }

            AppendOutput(string.Format(LanguageManager.T("optimizer.msg.applying"), selected.Length));

            IReadOnlyList<SystemTweakResult> results;
            try
            {
                results = await SystemTweakEngine.ApplyAsync(selected, CancellationToken.None);
            }
            catch (Exception ex)
            {
                AppendOutput(string.Format(LanguageManager.T("optimizer.msg.apply_failed"), ex.Message));
                ShowActionToast("ONE-CLICK FAILED", ex.Message, inProgress: false, isError: true);
                SetBusy(false);
                return;
            }

            foreach (var result in results)
            {
                AppendOutput($"[{(result.Success ? "OK" : "FAIL")}] {LanguageManager.LocalizeLiteral(result.Title)}");
                if (!string.IsNullOrWhiteSpace(result.Output))
                    AppendOutput(result.Output);
            }

            int okCount = results.Count(r => r.Success);
            int failCount = results.Count - okCount;
            var doneText = string.Format(LanguageManager.T("optimizer.msg.one_click_done"), okCount, failCount);

            ShowActionToast(
                failCount == 0 ? "ONE-CLICK COMPLETED" : "ONE-CLICK COMPLETED WITH WARNINGS",
                doneText,
                inProgress: false,
                isError: failCount > 0);

            SetBusy(false);
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
            => NavigationService?.GoBack();
    }
}
