using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ZantesEngine.Services;

namespace ZantesEngine.Pages
{
    public partial class BenchmarkPage : Page
    {
        private const int CaptureDurationSeconds = 45;
        private static readonly string BaselinePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZantesEngine",
            "benchmark_baseline.json");

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

        private sealed class BenchmarkSnapshot
        {
            public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
            public double AvgCpu { get; set; }
            public double AvgRam { get; set; }
            public double AvgDisk { get; set; }
            public double AvgNetworkMbps { get; set; }
            public double AvgFps { get; set; }
            public double Low1Fps { get; set; }
        }

        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
        private readonly List<double> _cpuSamples = new();
        private readonly List<double> _ramSamples = new();
        private readonly List<double> _diskSamples = new();
        private readonly List<double> _networkSamples = new();
        private int _captureTick;

        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _diskCounter;
        private NetworkInterface? _activeAdapter;
        private long _prevRxBytes;
        private long _prevTxBytes;

        private BenchmarkSnapshot? _baseline;
        private BenchmarkSnapshot? _current;

        public BenchmarkPage()
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
            InitializeTelemetry();
            _baseline = LoadBaseline();
            RenderCards();
            TxtCaptureStatus.Text = LanguageManager.T("bench.status.idle");
            AppendLog(LanguageManager.T("bench.log.ready"));
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
            TxtModule.Text = LanguageManager.T("bench.module");
            TxtTitle.Text = LanguageManager.T("bench.title");
            TxtSubtitle.Text = LanguageManager.T("bench.subtitle");
            TxtCaptureHeader.Text = LanguageManager.T("bench.capture.header");
            TxtCaptureNote.Text = LanguageManager.T("bench.capture.note");
            TxtAvgFpsLabel.Text = LanguageManager.T("bench.avg_fps");
            TxtLow1Label.Text = LanguageManager.T("bench.low1");
            BtnStartCapture.Content = LanguageManager.T("bench.btn.start");
            BtnSaveBaseline.Content = LanguageManager.T("bench.btn.save");
            BtnCompare.Content = LanguageManager.T("bench.btn.compare");
            BtnClearBaseline.Content = LanguageManager.T("bench.btn.clear");
            TxtInsightHeader.Text = LanguageManager.T("bench.insight.header");
            TxtInsightScoreLabel.Text = LanguageManager.T("bench.insight.score");
            TxtInsightConsistencyLabel.Text = LanguageManager.T("bench.insight.consistency");
            TxtInsightPressureLabel.Text = LanguageManager.T("bench.insight.pressure");
            TxtInsightVerdictLabel.Text = LanguageManager.T("bench.insight.verdict");
            TxtBaselineHeader.Text = LanguageManager.T("bench.card.baseline");
            TxtCurrentHeader.Text = LanguageManager.T("bench.card.current");
            TxtDeltaHeader.Text = LanguageManager.T("bench.card.delta");
        }

        private void InitializeTelemetry()
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

            _activeAdapter = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderByDescending(n => n.Speed)
                .FirstOrDefault();

            if (_activeAdapter != null)
            {
                var stats = _activeAdapter.GetIPv4Statistics();
                _prevRxBytes = stats.BytesReceived;
                _prevTxBytes = stats.BytesSent;
            }
        }

        private void StartCapture_Click(object sender, RoutedEventArgs e)
        {
            if (_timer.IsEnabled)
                return;

            _cpuSamples.Clear();
            _ramSamples.Clear();
            _diskSamples.Clear();
            _networkSamples.Clear();
            _captureTick = 0;

            CaptureProgress.Maximum = CaptureDurationSeconds;
            CaptureProgress.Value = 0;
            TxtCaptureStatus.Text = string.Format(LanguageManager.T("bench.status.capturing"), CaptureDurationSeconds);
            BtnStartCapture.IsEnabled = false;
            _timer.Start();
            AppendLog(LanguageManager.T("bench.log.started"));
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _captureTick++;
            CaptureProgress.Value = _captureTick;

            _cpuSamples.Add(ReadPercentCounter(_cpuCounter));
            _ramSamples.Add(ReadRamLoadPercent());
            _diskSamples.Add(ReadPercentCounter(_diskCounter));
            _networkSamples.Add(ReadNetworkMbps());

            int remain = Math.Max(0, CaptureDurationSeconds - _captureTick);
            TxtCaptureStatus.Text = string.Format(LanguageManager.T("bench.status.capturing"), remain);

            if (_captureTick < CaptureDurationSeconds)
                return;

            _timer.Stop();
            BtnStartCapture.IsEnabled = true;
            _current = BuildSnapshot();
            TxtCaptureStatus.Text = LanguageManager.T("bench.status.done");
            AppendLog(LanguageManager.T("bench.log.completed"));
            RenderCards();
        }

        private void SaveBaseline_Click(object sender, RoutedEventArgs e)
        {
            if (_current == null)
            {
                TxtCaptureStatus.Text = LanguageManager.T("bench.status.no_current");
                return;
            }

            _baseline = _current;
            SaveBaseline(_baseline);
            TxtCaptureStatus.Text = LanguageManager.T("bench.status.saved");
            AppendLog(LanguageManager.T("bench.log.saved"));
            RenderCards();
        }

        private void Compare_Click(object sender, RoutedEventArgs e)
        {
            if (_baseline == null)
            {
                TxtCaptureStatus.Text = LanguageManager.T("bench.status.no_baseline");
                return;
            }

            if (_current == null)
            {
                TxtCaptureStatus.Text = LanguageManager.T("bench.status.no_current");
                return;
            }

            TxtDeltaBlock.Text = FormatDelta(_baseline, _current);
            TxtCaptureStatus.Text = LanguageManager.T("bench.status.compared");
            AppendLog(LanguageManager.T("bench.log.compared"));
        }

        private void ClearBaseline_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(BaselinePath))
                    File.Delete(BaselinePath);
            }
            catch
            {
                // ignored
            }

            _baseline = null;
            TxtCaptureStatus.Text = LanguageManager.T("bench.status.cleared");
            AppendLog(LanguageManager.T("bench.log.cleared"));
            RenderCards();
        }

        private BenchmarkSnapshot BuildSnapshot()
        {
            double avgFps = ParseOrZero(TbAvgFps.Text);
            double low1 = ParseOrZero(TbLow1.Text);

            return new BenchmarkSnapshot
            {
                TimestampUtc = DateTime.UtcNow,
                AvgCpu = Average(_cpuSamples),
                AvgRam = Average(_ramSamples),
                AvgDisk = Average(_diskSamples),
                AvgNetworkMbps = Average(_networkSamples),
                AvgFps = avgFps,
                Low1Fps = low1
            };
        }

        private void RenderCards()
        {
            TxtBaselineBlock.Text = _baseline == null
                ? LanguageManager.T("bench.card.no_baseline")
                : FormatSnapshot(_baseline);

            TxtCurrentBlock.Text = _current == null
                ? LanguageManager.T("bench.card.no_current")
                : FormatSnapshot(_current);

            if (_baseline == null || _current == null)
                TxtDeltaBlock.Text = LanguageManager.T("bench.card.no_delta");

            UpdateInsightCard();
        }

        private string FormatSnapshot(BenchmarkSnapshot s)
        {
            string timeLabel = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Zaman" : "Time";
            string cpuLabel = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "CPU Ort" : "CPU Avg";
            string ramLabel = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "RAM Ort" : "RAM Avg";
            string diskLabel = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Disk Ort" : "Disk Avg";
            string netLabel = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Ag Ort" : "Net Avg";
            string fpsLabel = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "FPS Ort" : "FPS Avg";
            string lowLabel = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "%1 Low" : "1% Low";
            string consistencyLabel = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Kare Tutarliligi" : "Frame Consistency";

            return string.Join(Environment.NewLine, new[]
            {
                $"{timeLabel}: {s.TimestampUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}",
                $"{cpuLabel}: {s.AvgCpu:F1}%",
                $"{ramLabel}: {s.AvgRam:F1}%",
                $"{diskLabel}: {s.AvgDisk:F1}%",
                $"{netLabel}: {s.AvgNetworkMbps:F2} Mbps",
                $"{fpsLabel}: {s.AvgFps:F1}",
                $"{lowLabel}: {s.Low1Fps:F1}",
                $"{consistencyLabel}: {ComputeConsistency(s):F0}%"
            });
        }

        private string FormatDelta(BenchmarkSnapshot baseline, BenchmarkSnapshot current)
        {
            string F(string label, double baseVal, double curVal, string unit)
            {
                double delta = curVal - baseVal;
                string sign = delta >= 0 ? "+" : string.Empty;
                return $"{label}: {baseVal:F1}{unit} -> {curVal:F1}{unit} ({sign}{delta:F1}{unit})";
            }

            string fpsLabel = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "FPS Ort" : "FPS Avg";
            string lowLabel = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "%1 Low" : "1% Low";
            string cpuLabel = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "CPU Ort" : "CPU Avg";
            string ramLabel = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "RAM Ort" : "RAM Avg";
            string diskLabel = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Disk Ort" : "Disk Avg";
            string netLabel = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Ag Ort" : "Net Avg";
            string consistencyLabel = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Kare Tutarliligi" : "Frame Consistency";

            return string.Join(Environment.NewLine, new[]
            {
                F(cpuLabel, baseline.AvgCpu, current.AvgCpu, "%"),
                F(ramLabel, baseline.AvgRam, current.AvgRam, "%"),
                F(diskLabel, baseline.AvgDisk, current.AvgDisk, "%"),
                F(netLabel, baseline.AvgNetworkMbps, current.AvgNetworkMbps, " Mbps"),
                F(fpsLabel, baseline.AvgFps, current.AvgFps, ""),
                F(lowLabel, baseline.Low1Fps, current.Low1Fps, ""),
                F(consistencyLabel, ComputeConsistency(baseline), ComputeConsistency(current), "%")
            });
        }

        private void UpdateInsightCard()
        {
            BenchmarkSnapshot? snapshot = _current ?? _baseline;
            if (snapshot == null)
            {
                TxtInsightScoreValue.Text = "--";
                TxtInsightConsistencyValue.Text = "--";
                TxtInsightPressureValue.Text = "--";
                TxtInsightVerdictValue.Text = LanguageManager.T("bench.insight.empty");
                return;
            }

            double score = ComputeScore(snapshot);
            double consistency = ComputeConsistency(snapshot);
            double pressure = ComputePressure(snapshot);

            TxtInsightScoreValue.Text = $"{score:F0}";
            TxtInsightConsistencyValue.Text = $"{consistency:F0}%";
            TxtInsightPressureValue.Text = $"{pressure:F0}%";
            TxtInsightVerdictValue.Text = BuildVerdict(snapshot, _baseline, _current);
        }

        private static double ComputeConsistency(BenchmarkSnapshot snapshot)
        {
            if (snapshot.AvgFps <= 0)
                return 0;

            return Math.Clamp(snapshot.Low1Fps / snapshot.AvgFps * 100d, 0, 100);
        }

        private static double ComputePressure(BenchmarkSnapshot snapshot)
            => Math.Clamp((snapshot.AvgCpu * 0.45) + (snapshot.AvgRam * 0.35) + (snapshot.AvgDisk * 0.20), 0, 100);

        private static double ComputeScore(BenchmarkSnapshot snapshot)
        {
            double fpsScore = snapshot.AvgFps <= 0 ? 20 : Math.Clamp(snapshot.AvgFps / 240d * 100d, 0, 100);
            double consistency = ComputeConsistency(snapshot);
            double efficiency = 100d - ComputePressure(snapshot);
            return Math.Clamp((fpsScore * 0.40) + (consistency * 0.35) + (efficiency * 0.25), 0, 100);
        }

        private string BuildVerdict(BenchmarkSnapshot snapshot, BenchmarkSnapshot? baseline, BenchmarkSnapshot? current)
        {
            if (current == null)
                return LanguageManager.T("bench.insight.empty");

            if (baseline == null)
            {
                double score = ComputeScore(snapshot);
                if (score >= 75)
                    return LanguageManager.T("bench.insight.good");
                if (score >= 55)
                    return LanguageManager.T("bench.insight.mid");
                return LanguageManager.T("bench.insight.low");
            }

            double fpsDelta = current.AvgFps - baseline.AvgFps;
            double lowDelta = current.Low1Fps - baseline.Low1Fps;
            double pressureDelta = ComputePressure(current) - ComputePressure(baseline);

            if (fpsDelta >= 3 && lowDelta >= 2 && pressureDelta <= 2)
                return LanguageManager.T("bench.insight.up");
            if (fpsDelta <= -3 || lowDelta <= -2)
                return LanguageManager.T("bench.insight.down");
            return LanguageManager.T("bench.insight.flat");
        }

        private static BenchmarkSnapshot? LoadBaseline()
        {
            try
            {
                if (!File.Exists(BaselinePath))
                    return null;

                string json = File.ReadAllText(BaselinePath);
                return JsonSerializer.Deserialize<BenchmarkSnapshot>(json);
            }
            catch
            {
                return null;
            }
        }

        private static void SaveBaseline(BenchmarkSnapshot snapshot)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(BaselinePath)!);
                string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(BaselinePath, json);
            }
            catch
            {
                // ignored
            }
        }

        private void AppendLog(string message)
        {
            BenchmarkLog.Text += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
            BenchmarkLog.ScrollToEnd();
        }

        private static double ParseOrZero(string text)
            => double.TryParse(text?.Trim(), out var v) ? Math.Max(0, v) : 0;

        private static double Average(List<double> values)
            => values.Count == 0 ? 0 : values.Average();

        private static double ReadPercentCounter(PerformanceCounter? counter)
        {
            if (counter == null)
                return 0;

            try
            {
                return Math.Clamp(counter.NextValue(), 0, 100);
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

        private void GoBack_Click(object sender, RoutedEventArgs e)
            => NavigationService?.GoBack();
    }
}
