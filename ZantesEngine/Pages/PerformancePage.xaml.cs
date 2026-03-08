using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Threading;
using ZantesEngine.Services;

namespace ZantesEngine.Pages
{
    public partial class PerformancePage : Page
    {
        private const int MaxSamples = 70;

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

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer);

        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
        private readonly Queue<double> _cpuSamples = new();
        private readonly Queue<double> _ramSamples = new();
        private readonly Queue<double> _diskSamples = new();
        private readonly Queue<double> _networkSamples = new();

        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _diskCounter;
        private NetworkInterface? _activeAdapter;
        private long _prevRxBytes;
        private long _prevTxBytes;
        private double _peakCpu;
        private double _peakRam;
        private double _peakDisk;
        private bool _customBusy;

        public PerformancePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            _timer.Tick += Timer_Tick;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LanguageManager.LanguageChanged += ApplyLanguage;
            ApplyLanguage();
            InitializeSystemInfo();
            InitializeCounters();
            _timer.Start();
            RefreshTelemetry();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            LanguageManager.LanguageChanged -= ApplyLanguage;
            _timer.Stop();
            _cpuCounter?.Dispose();
            _diskCounter?.Dispose();
        }

        private void ApplyLanguage()
        {
            TxtModule.Text = LanguageManager.T("perf.module");
            TxtTitle.Text = LanguageManager.T("perf.title");
            TxtSubtitle.Text = LanguageManager.T("perf.subtitle");

            TxtCpuLabel.Text = LanguageManager.T("perf.cpu");
            TxtRamLabel.Text = LanguageManager.T("perf.ram");
            TxtDiskLabel.Text = LanguageManager.T("perf.disk");
            TxtNetworkLabel.Text = LanguageManager.T("perf.network");
            TxtCpuPeakLabel.Text = LanguageManager.T("perf.cpu_load");
            TxtRamPeakLabel.Text = LanguageManager.T("perf.ram_load");
            TxtDiskPeakLabel.Text = LanguageManager.T("perf.disk_load");
            TxtHeartbeatLabel.Text = LanguageManager.T("perf.heartbeat");

            TxtInfoTitle.Text = LanguageManager.T("perf.info.title");
            TxtLiveStatus.Text = LanguageManager.T("perf.live");
            TxtScoreLabel.Text = LanguageManager.T("perf.score");
            TxtReadoutTitle.Text = LanguageManager.T("perf.readout.title");
            TxtCpuReadoutLabel.Text = LanguageManager.T("perf.cpu");
            TxtRamReadoutLabel.Text = LanguageManager.T("perf.ram");
            TxtDiskReadoutLabel.Text = LanguageManager.T("perf.disk");
            TxtNetworkReadoutLabel.Text = LanguageManager.T("perf.network");
            TxtGuidanceLabel.Text = LanguageManager.T("perf.guidance.label");

            TxtCpuInfoLabel.Text = LanguageManager.T("perf.cpu_model");
            TxtGpuInfoLabel.Text = LanguageManager.T("perf.gpu_model");
            TxtMemInfoLabel.Text = LanguageManager.T("perf.total_memory");
            TxtProcLabel.Text = LanguageManager.T("perf.running_processes");
            TxtAdapterLabel.Text = LanguageManager.T("perf.active_adapter");
            TxtCustomTitle.Text = LanguageManager.T("perf.custom.title");
            TxtCustomSubtitle.Text = LanguageManager.T("perf.custom.subtitle");
            CbCustomPower.Content = LanguageManager.T("perf.custom.power");
            CbCustomLatency.Content = LanguageManager.T("perf.custom.latency");
            CbCustomNetwork.Content = LanguageManager.T("perf.custom.network");
            CbCustomBackground.Content = LanguageManager.T("perf.custom.background");
            CbCustomVisual.Content = LanguageManager.T("perf.custom.visual");
            CbCustomRestore.Content = LanguageManager.T("quick.restore");
            BtnApplyCustom.Content = LanguageManager.T("perf.custom.apply");
            if (!_customBusy)
                TxtCustomStatus.Text = LanguageManager.T("perf.custom.status.ready");

            TxtCpuPeak.Text = FormatPeak(0);
            TxtRamPeak.Text = FormatPeak(0);
            TxtDiskPeak.Text = FormatPeak(0);
            TxtHeartbeat.Text = "--:--:--";
            TxtPerfScore.Text = "100";
            TxtPerfState.Text = LanguageManager.T("perf.state.stable");
            TxtCpuReadoutValue.Text = LanguageManager.T("perf.readout.balanced");
            TxtRamReadoutValue.Text = LanguageManager.T("perf.readout.balanced");
            TxtDiskReadoutValue.Text = LanguageManager.T("perf.readout.balanced");
            TxtNetworkReadoutValue.Text = LanguageManager.T("perf.readout.quiet");
            TxtGuidanceValue.Text = LanguageManager.T("perf.guidance.ok");
        }

        private void InitializeSystemInfo()
        {
            TxtCpuInfoValue.Text = QuerySingle("SELECT Name FROM Win32_Processor", "Name");
            TxtGpuInfoValue.Text = QuerySingle("SELECT Name FROM Win32_VideoController", "Name");

            string memRaw = QuerySingle("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem", "TotalPhysicalMemory");
            if (ulong.TryParse(memRaw, out ulong totalBytes) && totalBytes > 0)
            {
                double gb = totalBytes / 1024d / 1024d / 1024d;
                TxtMemInfoValue.Text = $"{gb:F1} GB";
            }
            else
            {
                TxtMemInfoValue.Text = "-";
            }

            _activeAdapter = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderByDescending(n => n.Speed)
                .FirstOrDefault();

            TxtAdapterValue.Text = _activeAdapter?.Name ?? "-";

            if (_activeAdapter != null)
            {
                var stats = _activeAdapter.GetIPv4Statistics();
                _prevRxBytes = stats.BytesReceived;
                _prevTxBytes = stats.BytesSent;
            }
        }

        private void InitializeCounters()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                _cpuCounter.NextValue();
            }
            catch
            {
                _cpuCounter = null;
            }

            try
            {
                _diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total", true);
                _diskCounter.NextValue();
            }
            catch
            {
                _diskCounter = null;
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
            => RefreshTelemetry();

        private void RefreshTelemetry()
        {
            double cpu = ReadPercentCounter(_cpuCounter);
            double disk = ReadPercentCounter(_diskCounter);
            double ram = ReadRamLoadPercent();
            double networkMbps = ReadNetworkMbps();

            CpuValueText.Text = $"{cpu:F1}%";
            RamValueText.Text = $"{ram:F1}%";
            DiskValueText.Text = $"{disk:F1}%";
            NetworkValueText.Text = $"{networkMbps:F2} Mbps";
            TxtProcValue.Text = Process.GetProcesses().Length.ToString();
            TxtHeartbeat.Text = DateTime.Now.ToString("HH:mm:ss");

            CpuUsageBar.Value = cpu;
            RamUsageBar.Value = ram;
            DiskUsageBar.Value = disk;
            NetworkUsageBar.Value = Math.Clamp(networkMbps * 8, 0, 100);

            _peakCpu = Math.Max(_peakCpu, cpu);
            _peakRam = Math.Max(_peakRam, ram);
            _peakDisk = Math.Max(_peakDisk, disk);
            TxtCpuPeak.Text = FormatPeak(_peakCpu);
            TxtRamPeak.Text = FormatPeak(_peakRam);
            TxtDiskPeak.Text = FormatPeak(_peakDisk);

            PushSample(_cpuSamples, cpu);
            PushSample(_ramSamples, ram);
            PushSample(_diskSamples, disk);
            PushSample(_networkSamples, networkMbps);

            DrawLine(_cpuSamples, CpuGraphLine, CpuGraphCanvas, 100);
            DrawLine(_ramSamples, RamGraphLine, RamGraphCanvas, 100);
            DrawLine(_diskSamples, DiskGraphLine, DiskGraphCanvas, 100);
            DrawLine(_networkSamples, NetworkGraphLine, NetworkGraphCanvas, Math.Max(10, _networkSamples.DefaultIfEmpty(0).Max() * 1.2));

            UpdatePerformanceScore(cpu, ram, disk);
            UpdateReadout(cpu, ram, disk, networkMbps);
        }

        private void UpdatePerformanceScore(double cpu, double ram, double disk)
        {
            double weightedLoad = (cpu * 0.50) + (ram * 0.30) + (disk * 0.20);
            int score = (int)Math.Round(Math.Clamp(100 - weightedLoad, 0, 100));
            TxtPerfScore.Text = score.ToString();

            if (score >= 75)
            {
                TxtPerfState.Text = LanguageManager.T("perf.state.stable");
                TxtPerfState.Foreground = System.Windows.Media.Brushes.LightGreen;
                return;
            }

            if (score >= 50)
            {
                TxtPerfState.Text = LanguageManager.T("perf.state.load");
                TxtPerfState.Foreground = System.Windows.Media.Brushes.Khaki;
                return;
            }

            TxtPerfState.Text = LanguageManager.T("perf.state.high");
            TxtPerfState.Foreground = System.Windows.Media.Brushes.OrangeRed;
        }

        private void UpdateReadout(double cpu, double ram, double disk, double networkMbps)
        {
            TxtCpuReadoutValue.Text = DescribePercentLoad(cpu);
            TxtRamReadoutValue.Text = DescribePercentLoad(ram);
            TxtDiskReadoutValue.Text = DescribePercentLoad(disk);
            TxtNetworkReadoutValue.Text = DescribeNetworkLoad(networkMbps);
            TxtGuidanceValue.Text = BuildGuidance(cpu, ram, disk, networkMbps);
        }

        private async void ApplyCustom_Click(object sender, RoutedEventArgs e)
        {
            if (_customBusy)
                return;

            if (!SystemTweakEngine.IsAdministrator())
            {
                TxtCustomStatus.Text = LanguageManager.T("perf.custom.status.need_admin");
                return;
            }

            var tweaks = BuildCustomTweaks();
            if (tweaks.Count == 0)
            {
                TxtCustomStatus.Text = LanguageManager.T("perf.custom.status.none");
                return;
            }

            SetCustomBusy(true);
            try
            {
                if (CbCustomRestore.IsChecked == true)
                {
                    var restore = await Task.Run(() =>
                        SystemTweakEngine.CreateRestorePoint($"Zantes Tweak Custom Performance {DateTime.Now:yyyy-MM-dd HH:mm:ss}"));
                    TxtCustomStatus.Text = LanguageManager.LocalizeLiteral(restore.Message);
                }

                var results = await SystemTweakEngine.ApplyAsync(tweaks, CancellationToken.None);
                int ok = results.Count(r => r.Success);
                int fail = results.Count - ok;
                TxtCustomStatus.Text = string.Format(LanguageManager.T("perf.custom.status.done"), ok, fail);
            }
            catch (Exception ex)
            {
                TxtCustomStatus.Text = ex.Message;
            }
            finally
            {
                SetCustomBusy(false);
            }
        }

        private IReadOnlyList<SystemTweakDefinition> BuildCustomTweaks()
        {
            var keys = new List<string>();

            if (CbCustomPower.IsChecked == true)
            {
                keys.AddRange(new[]
                {
                    "power_high_performance",
                    "cpu_maximum_state_100",
                    "cpu_core_parking_off",
                    "disable_power_throttling",
                    "disable_startup_delay",
                    "usb_selective_suspend_off"
                });
            }

            if (CbCustomLatency.IsChecked == true)
            {
                keys.AddRange(new[]
                {
                    "enable_game_mode",
                    "disable_game_dvr",
                    "disable_xbox_gamebar",
                    "disable_mouse_accel",
                    "disable_sticky_keys_shortcut",
                    "mmcss_system_responsiveness",
                    "mmcss_games_task_profile",
                    "hw_scheduling",
                    "disable_network_throttling"
                });
            }

            if (CbCustomNetwork.IsChecked == true)
            {
                keys.AddRange(new[]
                {
                    "tcp_autotune",
                    "network_rss",
                    "tcp_ecn_disabled",
                    "tcp_heuristics_disabled",
                    "tcp_chimney_disabled",
                    "tcp_rsc_disabled",
                    "disable_nagle_algorithm",
                    "disable_qos_reserved_bandwidth",
                    "disable_delivery_opt",
                    "dns_flush"
                });
            }

            if (CbCustomBackground.IsChecked == true)
            {
                keys.AddRange(new[]
                {
                    "disable_telemetry",
                    "disable_diagtrack_service",
                    "disable_windows_error_reporting",
                    "disable_background_apps",
                    "disable_windows_tips",
                    "disable_feedback_notifications",
                    "disable_activity_history",
                    "disable_clipboard_history",
                    "disable_search_web_results",
                    "disable_phone_service",
                    "disable_consumer_features",
                    "disable_storage_sense",
                    "disable_onedrive_startup",
                    "disable_teams_autostart"
                });
            }

            if (CbCustomVisual.IsChecked == true)
            {
                keys.AddRange(new[]
                {
                    "visualfx_performance",
                    "disable_menu_show_delay",
                    "disable_window_animations",
                    "disable_transparency",
                    "disable_start_recommendations",
                    "disable_windows_spotlight",
                    "disable_widgets",
                    "disable_copilot"
                });
            }

            return keys
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(SystemTweakCatalog.Get)
                .Where(t => t != null)
                .Cast<SystemTweakDefinition>()
                .ToArray();
        }

        private void SetCustomBusy(bool busy)
        {
            _customBusy = busy;
            BtnApplyCustom.IsEnabled = !busy;
            CbCustomPower.IsEnabled = !busy;
            CbCustomLatency.IsEnabled = !busy;
            CbCustomNetwork.IsEnabled = !busy;
            CbCustomBackground.IsEnabled = !busy;
            CbCustomVisual.IsEnabled = !busy;
            CbCustomRestore.IsEnabled = !busy;
            if (busy)
                TxtCustomStatus.Text = LanguageManager.T("perf.custom.status.applying");
        }

        private string FormatPeak(double value)
            => string.Format(LanguageManager.T("perf.peak_fmt"), value);

        private string DescribePercentLoad(double value)
        {
            if (value < 45)
                return LanguageManager.T("perf.readout.balanced");
            if (value < 75)
                return LanguageManager.T("perf.readout.busy");
            return LanguageManager.T("perf.readout.high");
        }

        private string DescribeNetworkLoad(double value)
        {
            if (value < 2)
                return LanguageManager.T("perf.readout.quiet");
            if (value < 20)
                return LanguageManager.T("perf.readout.active");
            return LanguageManager.T("perf.readout.spike");
        }

        private string BuildGuidance(double cpu, double ram, double disk, double networkMbps)
        {
            double max = Math.Max(Math.Max(cpu, ram), disk);
            if (max < 55 && networkMbps < 10)
                return LanguageManager.T("perf.guidance.ok");
            if (cpu >= ram && cpu >= disk && cpu >= 75)
                return LanguageManager.T("perf.guidance.cpu");
            if (ram >= cpu && ram >= disk && ram >= 75)
                return LanguageManager.T("perf.guidance.ram");
            if (disk >= cpu && disk >= ram && disk >= 70)
                return LanguageManager.T("perf.guidance.disk");
            if (networkMbps >= 20)
                return LanguageManager.T("perf.guidance.network");
            return LanguageManager.T("perf.guidance.tune");
        }

        private static double ReadPercentCounter(PerformanceCounter? counter)
        {
            if (counter == null)
                return 0;

            try
            {
                double value = counter.NextValue();
                return Math.Clamp(value, 0, 100);
            }
            catch
            {
                return 0;
            }
        }

        private static double ReadRamLoadPercent()
        {
            try
            {
                var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
                return GlobalMemoryStatusEx(ref mem) ? mem.dwMemoryLoad : 0;
            }
            catch
            {
                return 0;
            }
        }

        private double ReadNetworkMbps()
        {
            if (_activeAdapter == null)
                return 0;

            try
            {
                var stats = _activeAdapter.GetIPv4Statistics();
                long totalNow = stats.BytesReceived + stats.BytesSent;
                long totalPrev = _prevRxBytes + _prevTxBytes;

                _prevRxBytes = stats.BytesReceived;
                _prevTxBytes = stats.BytesSent;

                long diff = Math.Max(0, totalNow - totalPrev);
                return diff * 8d / 1_000_000d;
            }
            catch
            {
                return 0;
            }
        }

        private static void PushSample(Queue<double> samples, double value)
        {
            samples.Enqueue(value);
            while (samples.Count > MaxSamples)
                samples.Dequeue();
        }

        private static void DrawLine(Queue<double> samples, Polyline line, Canvas canvas, double maxValue)
        {
            double width = Math.Max(20, canvas.ActualWidth);
            double height = Math.Max(20, canvas.ActualHeight);
            if (samples.Count == 0)
            {
                line.Points.Clear();
                return;
            }

            var values = samples.ToArray();
            double stepX = values.Length <= 1 ? width : width / (values.Length - 1);
            line.Points.Clear();

            for (int i = 0; i < values.Length; i++)
            {
                double normalized = maxValue <= 0 ? 0 : Math.Clamp(values[i] / maxValue, 0, 1);
                double x = i * stepX;
                double y = height - normalized * height;
                line.Points.Add(new Point(x, y));
            }
        }

        private static string QuerySingle(string query, string field)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(query);
                foreach (ManagementObject obj in searcher.Get())
                {
                    string value = obj[field]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(value))
                        return value.Trim();
                }
            }
            catch
            {
                // ignored
            }

            return "-";
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
            => NavigationService?.GoBack();
    }
}
