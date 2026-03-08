using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ZantesEngine.Services;

namespace ZantesEngine.Pages
{
    public partial class NetworkPage : Page
    {
        private sealed class NetProbe
        {
            public bool Success { get; init; }
            public double LatencyMs { get; init; }
        }

        private readonly DispatcherTimer _metricsTimer = new() { Interval = TimeSpan.FromSeconds(3) };
        private readonly Queue<double> _latencyHistory = new();
        private readonly Queue<bool> _lossHistory = new();
        private readonly Dictionary<CheckBox, string> _routeToggleMap = new();
        private DateTime _lastSpikeApplyUtc = DateTime.MinValue;
        private bool _spikeLatched;
        private bool _busy;
        private double _currentLossPercent;
        private NetworkAutoTuneResult? _wizardResult;

        public NetworkPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_routeToggleMap.Count == 0)
                BindRouteToggles();

            _metricsTimer.Tick += MetricsTimer_Tick;
            _metricsTimer.Start();

            LanguageManager.LanguageChanged += OnLanguageChanged;
            OnLanguageChanged();
            UpdateDriverVendorLabel();
            AppendOutput(LanguageManager.T("network.log.ready"));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _metricsTimer.Stop();
            _metricsTimer.Tick -= MetricsTimer_Tick;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
            => Dispatcher.BeginInvoke(new Action(() =>
            {
                TxtModule.Text = LanguageManager.T("network.module");
                TxtTitle.Text = LanguageManager.T("network.title");
                TxtRouteLabel.Text = LanguageManager.T("network.route_stack");
                TxtSpikeGuardLabel.Text = LanguageManager.T("network.spike_guard");
                TxtSpikeThresholdLabel.Text = LanguageManager.T("network.spike_threshold");
                CbSpikeGuard.Content = LanguageManager.T("network.guard_enable");
                CbCreateRestoreOnSpike.Content = LanguageManager.T("network.guard_restore");
                BtnApplyRouteProfile.Content = LanguageManager.T("network.btn.apply_route");
                TxtRouteProfileLabel.Text = LanguageManager.T("network.route_profile");
                TxtRouteProfileName.Text = LanguageManager.T("network.route_profile_name");
                TxtDriverPresetLabel.Text = LanguageManager.T("network.driver_preset");
                BtnApplyDriverPreset.Content = LanguageManager.T("network.btn.apply_driver");
                TxtDriverHint.Text = LanguageManager.T("network.driver_hint");
                TxtSpikeState.Text = LanguageManager.T("network.guard_armed");
                TxtRouteScore.Text = string.Format(LanguageManager.T("network.readiness"), (int)Math.Round(RouteScoreBar.Value));
                UpdateDriverVendorLabel();
                UpdateThresholdLabel();
                TxtWizardLabel.Text = LanguageManager.T("network.wizard.title");
                TxtWizardDesc.Text = LanguageManager.T("network.wizard.desc");
                BtnRunWizard.Content = LanguageManager.T("network.wizard.run");
                BtnApplyWizard.Content = LanguageManager.T("network.wizard.apply");
                CbApplyRecommendedDns.Content = LanguageManager.T("network.wizard.apply_dns");
                if (_wizardResult == null)
                {
                    TxtWizardStatus.Text = LanguageManager.T("network.wizard.status_idle");
                    TxtWizardBestDns.Text = LanguageManager.T("network.wizard.best_dns_empty");
                    TxtWizardMtu.Text = LanguageManager.T("network.wizard.mtu_empty");
                    TxtWizardProfile.Text = LanguageManager.T("network.wizard.profile_empty");
                }
                Dispatcher.BeginInvoke(new Action(() => LanguageManager.LocalizeTree(this)));
            }));

        private void BindRouteToggles()
        {
            _routeToggleMap[CbTcpAutotune] = "tcp_autotune";
            _routeToggleMap[CbNetworkRss] = "network_rss";
            _routeToggleMap[CbTcpHeuristics] = "tcp_heuristics_disabled";
            _routeToggleMap[CbDisableNetworkThrottle] = "disable_network_throttling";
        }

        private async void MetricsTimer_Tick(object? sender, EventArgs e)
        {
            var probe = await ProbeAsync();
            UpdateMetrics(probe);

            if (CbSpikeGuard.IsChecked == true)
                await EvaluateSpikeGuardAsync(probe);
        }

        private async Task<NetProbe> ProbeAsync()
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync("1.1.1.1", 1200);
                if (reply.Status == IPStatus.Success)
                {
                    return new NetProbe
                    {
                        Success = true,
                        LatencyMs = reply.RoundtripTime
                    };
                }
            }
            catch
            {
                // ignored
            }

            return new NetProbe
            {
                Success = false
            };
        }

        private void UpdateMetrics(NetProbe probe)
        {
            _lossHistory.Enqueue(probe.Success);
            while (_lossHistory.Count > 40)
                _lossHistory.Dequeue();

            if (probe.Success)
            {
                _latencyHistory.Enqueue(probe.LatencyMs);
                while (_latencyHistory.Count > 20)
                    _latencyHistory.Dequeue();
            }

            _currentLossPercent = _lossHistory.Count == 0
                ? 0
                : 100d * _lossHistory.Count(v => !v) / _lossHistory.Count;

            double jitter = 0;
            if (_latencyHistory.Count >= 2)
            {
                var arr = _latencyHistory.ToArray();
                double sum = 0;
                for (int i = 1; i < arr.Length; i++)
                    sum += Math.Abs(arr[i] - arr[i - 1]);
                jitter = sum / (arr.Length - 1);
            }

            if (probe.Success)
                TxtPingValue.Text = $"{probe.LatencyMs:F0} ms";
            else
                TxtPingValue.Text = "timeout";

            TxtJitterValue.Text = $"{jitter:F1} ms";
            TxtLossValue.Text = $"{_currentLossPercent:F1}%";

            int score = ComputeReadinessScore(probe.Success ? probe.LatencyMs : 220, jitter, _currentLossPercent);
            RouteScoreBar.Value = score;
            TxtRouteScore.Text = string.Format(LanguageManager.T("network.readiness"), score);
        }

        private static int ComputeReadinessScore(double ping, double jitter, double lossPercent)
        {
            double score = 100;
            score -= Math.Min(50, ping / 2.8);
            score -= Math.Min(25, jitter * 1.7);
            score -= Math.Min(45, lossPercent * 5.4);
            return (int)Math.Clamp(Math.Round(score), 1, 100);
        }

        private async Task EvaluateSpikeGuardAsync(NetProbe probe)
        {
            double threshold = SldSpikeThreshold.Value;
            bool spike = !probe.Success || probe.LatencyMs > threshold || _currentLossPercent >= 8;
            bool recovered = probe.Success && probe.LatencyMs < threshold * 0.8 && _currentLossPercent < 3;

            if (recovered)
                _spikeLatched = false;

            if (!spike || _spikeLatched)
                return;

            _spikeLatched = true;
            TxtSpikeState.Text = string.Format(
                LanguageManager.T("network.guard_triggered"),
                probe.Success ? probe.LatencyMs.ToString("F0") : "timeout",
                _currentLossPercent.ToString("F1"));
            AppendOutput(TxtSpikeState.Text);

            if (DateTime.UtcNow - _lastSpikeApplyUtc < TimeSpan.FromMinutes(2))
            {
                AppendOutput(LanguageManager.T("network.guard_cooldown"));
                return;
            }

            if (!SystemTweakEngine.IsAdministrator())
            {
                AppendOutput(LanguageManager.T("network.guard_need_admin"));
                return;
            }

            _lastSpikeApplyUtc = DateTime.UtcNow;
            await ApplyTweaksAsync(
                new[] { "tcp_autotune", "network_rss", "tcp_heuristics_disabled", "disable_network_throttling", "dns_flush" },
                CbCreateRestoreOnSpike.IsChecked == true,
                LanguageManager.T("network.guard_apply_name"));
        }

        private async void BtnApplyRouteProfile_Click(object sender, RoutedEventArgs e)
        {
            var keys = _routeToggleMap
                .Where(p => p.Key.IsChecked == true)
                .Select(p => p.Value)
                .ToArray();

            if (keys.Length == 0)
            {
                AppendOutput(LanguageManager.T("network.route_none"));
                return;
            }

            await ApplyTweaksAsync(keys, createRestore: true, LanguageManager.T("network.route_profile_name"));
        }

        private async void BtnApplyDriverPreset_Click(object sender, RoutedEventArgs e)
        {
            if (_busy)
                return;

            if (!SystemTweakEngine.IsAdministrator())
            {
                AppendOutput(LanguageManager.T("network.guard_need_admin"));
                return;
            }

            SetBusy(true);
            AppendOutput(LanguageManager.T("network.driver_apply_start"));
            var result = await DriverPresetService.ApplyBestPresetAsync(createRestorePoint: true, CancellationToken.None);
            SetBusy(false);

            if (!result.Success)
            {
                AppendOutput($"{LanguageManager.T("network.driver_apply_fail")} {result.Message}");
                return;
            }

            AppendOutput($"{LanguageManager.T("network.driver_apply_done")} {result.Message}");
            UpdateDriverVendorLabel();
        }

        private async void BtnRunWizard_Click(object sender, RoutedEventArgs e)
        {
            if (_busy)
                return;

            SetBusy(true);
            TxtWizardStatus.Text = LanguageManager.T("network.wizard.status_running");
            AppendOutput(LanguageManager.T("network.wizard.status_running"));

            try
            {
                var result = await NetworkAutoTuneService.RunAsync(CancellationToken.None);
                _wizardResult = result;

                TxtWizardStatus.Text = LanguageManager.T("network.wizard.status_ready");
                TxtWizardBestDns.Text = string.Format(
                    LanguageManager.T("network.wizard.best_dns_fmt"),
                    result.BestDns.Label,
                    result.BestDns.Primary,
                    result.BestDns.AverageLatencyMs);
                TxtWizardMtu.Text = string.Format(
                    LanguageManager.T("network.wizard.mtu_fmt"),
                    result.RecommendedMtuPayload,
                    result.RecommendedMtuPayload + 28);
                TxtWizardProfile.Text = string.Format(
                    LanguageManager.T("network.wizard.profile_fmt"),
                    GetProfileLabel(result.Profile),
                    LanguageManager.LocalizeLiteral(result.Reason));

                ApplyProfileDefaults(result.Profile);
                AppendOutput(TxtWizardBestDns.Text);
                AppendOutput(TxtWizardMtu.Text);
                AppendOutput(TxtWizardProfile.Text);
            }
            catch (Exception ex)
            {
                _wizardResult = null;
                TxtWizardStatus.Text = LanguageManager.T("network.wizard.status_failed");
                AppendOutput($"{LanguageManager.T("network.wizard.status_failed")} {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void BtnApplyWizard_Click(object sender, RoutedEventArgs e)
        {
            if (_busy)
                return;

            if (_wizardResult == null)
            {
                AppendOutput(LanguageManager.T("network.wizard.need_run"));
                return;
            }

            if (!SystemTweakEngine.IsAdministrator())
            {
                AppendOutput(LanguageManager.T("network.wizard.need_admin"));
                return;
            }

            var keys = BuildWizardTweakKeys(_wizardResult.Profile).ToList();
            var tweaks = keys
                .Select(SystemTweakCatalog.Get)
                .Where(t => t != null)
                .Cast<SystemTweakDefinition>()
                .ToList();

            tweaks.Add(BuildMtuDefinition(_wizardResult.RecommendedMtuPayload));
            if (CbApplyRecommendedDns.IsChecked == true)
                tweaks.Add(BuildDnsDefinition(_wizardResult.BestDns));

            AppendOutput(LanguageManager.T("network.wizard.apply_start"));
            await ApplyDefinitionsAsync(
                tweaks,
                createRestore: true,
                $"{LanguageManager.T("network.wizard.profile_name")} ({GetProfileLabel(_wizardResult.Profile)})");
        }

        private async Task ApplyTweaksAsync(IEnumerable<string> tweakKeys, bool createRestore, string profileName)
        {
            if (_busy)
                return;

            if (!SystemTweakEngine.IsAdministrator())
            {
                AppendOutput(LanguageManager.T("network.guard_need_admin"));
                return;
            }

            var tweaks = tweakKeys
                .Select(SystemTweakCatalog.Get)
                .Where(t => t != null)
                .Cast<SystemTweakDefinition>()
                .ToArray();

            if (tweaks.Length == 0)
                return;

            await ApplyDefinitionsAsync(tweaks, createRestore, profileName);
        }

        private async Task ApplyDefinitionsAsync(IEnumerable<SystemTweakDefinition> tweaks, bool createRestore, string profileName)
        {
            if (_busy)
                return;

            SetBusy(true);
            var tweakArray = tweaks.ToArray();
            AppendOutput(string.Format(LanguageManager.T("network.apply_start"), profileName, tweakArray.Length));

            if (createRestore)
            {
                var restore = await Task.Run(() =>
                    SystemTweakEngine.CreateRestorePoint($"Zantes Tweak Network {DateTime.Now:yyyy-MM-dd HH:mm:ss}"));
                AppendOutput(LanguageManager.LocalizeLiteral(restore.Message));
            }

            IReadOnlyList<SystemTweakResult> results;
            try
            {
                results = await SystemTweakEngine.ApplyAsync(tweakArray, CancellationToken.None);
            }
            catch (Exception ex)
            {
                AppendOutput(ex.Message);
                SetBusy(false);
                return;
            }

            int ok = results.Count(r => r.Success);
            int fail = results.Count - ok;
            AppendOutput(string.Format(LanguageManager.T("network.apply_done"), ok, fail));
            SetBusy(false);
        }

        private void UpdateDriverVendorLabel()
        {
            var detect = DriverPresetService.DetectPrimaryVendor();
            TxtDetectedVendor.Text = string.Format(LanguageManager.T("network.driver_detected"), detect.Label);
        }

        private void SetBusy(bool value)
        {
            _busy = value;
            BtnApplyRouteProfile.IsEnabled = !value;
            BtnApplyDriverPreset.IsEnabled = !value;
            BtnRunWizard.IsEnabled = !value;
            BtnApplyWizard.IsEnabled = !value;
            CbSpikeGuard.IsEnabled = !value;
            CbApplyRecommendedDns.IsEnabled = !value;
        }

        private void AppendOutput(string message)
        {
            OutputBox.Text += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
            OutputBox.ScrollToEnd();
        }

        private void SldSpikeThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => UpdateThresholdLabel();

        private void UpdateThresholdLabel()
            => TxtSpikeThresholdValue.Text = $"{SldSpikeThreshold.Value:F0} ms";

        private static SystemTweakDefinition BuildMtuDefinition(int mtuPayload)
        {
            int mtu = Math.Clamp(mtuPayload + 28, 1280, 1500);
            string command =
                $"powershell -NoProfile -Command \"$mtu={mtu}; Get-NetIPInterface -AddressFamily IPv4 | Where-Object {{ $_.ConnectionState -eq 'Connected' -and $_.InterfaceAlias -notlike '*Loopback*' }} | ForEach-Object {{ Set-NetIPInterface -InterfaceIndex $_.InterfaceIndex -NlMtuBytes $mtu -ErrorAction SilentlyContinue }}\"";

            return new SystemTweakDefinition
            {
                Key = "dynamic_set_mtu",
                Title = "Set Recommended MTU",
                Category = "Network",
                Description = $"Applies recommended MTU {mtu} bytes to active IPv4 adapters.",
                Risk = TweakRisk.Caution,
                Warning = "Some VPN/PPPoE links may need a different MTU.",
                Commands = new[] { command }
            };
        }

        private static SystemTweakDefinition BuildDnsDefinition(DnsProbeSample dns)
        {
            string command =
                $"powershell -NoProfile -Command \"$servers='{dns.Primary}','{dns.Secondary}'; Get-NetAdapter | Where-Object {{ $_.Status -eq 'Up' -and $_.HardwareInterface -eq $true }} | ForEach-Object {{ Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ServerAddresses $servers -ErrorAction SilentlyContinue }}\"";

            return new SystemTweakDefinition
            {
                Key = "dynamic_set_dns",
                Title = "Apply Recommended DNS",
                Category = "Network",
                Description = $"Sets active adapters DNS to {dns.Label} ({dns.Primary}, {dns.Secondary}).",
                Commands = new[] { command }
            };
        }

        private IEnumerable<string> BuildWizardTweakKeys(NetworkTuneProfile profile)
        {
            var keys = new List<string>
            {
                "tcp_autotune",
                "network_rss",
                "tcp_heuristics_disabled",
                "disable_delivery_opt",
                "dns_flush",
                "disable_qos_reserved_bandwidth"
            };

            if (profile != NetworkTuneProfile.Safe)
                keys.Add("disable_nagle_algorithm");

            if (profile == NetworkTuneProfile.Balanced || profile == NetworkTuneProfile.Aggressive)
            {
                keys.Add("tcp_ecn_disabled");
                keys.Add("tcp_timestamps_disabled");
            }

            if (profile == NetworkTuneProfile.Aggressive)
            {
                keys.Add("disable_network_throttling");
                keys.Add("tcp_rsc_disabled");
                keys.Add("tcp_chimney_disabled");
            }

            return keys.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private void ApplyProfileDefaults(NetworkTuneProfile profile)
        {
            CbTcpAutotune.IsChecked = true;
            CbNetworkRss.IsChecked = true;
            CbTcpHeuristics.IsChecked = true;
            CbDisableNetworkThrottle.IsChecked = profile == NetworkTuneProfile.Aggressive;
        }

        private static string GetProfileLabel(NetworkTuneProfile profile)
            => profile switch
            {
                NetworkTuneProfile.Aggressive => LanguageManager.T("network.wizard.profile.aggressive"),
                NetworkTuneProfile.Balanced => LanguageManager.T("network.wizard.profile.balanced"),
                _ => LanguageManager.T("network.wizard.profile.safe")
            };

        private void GoBack_Click(object sender, RoutedEventArgs e)
            => NavigationService?.GoBack();
    }
}
