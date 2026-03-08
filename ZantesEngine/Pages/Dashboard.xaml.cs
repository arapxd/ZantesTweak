using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using ZantesEngine.Services;

namespace ZantesEngine.Pages
{
    public partial class Dashboard : Page
    {
        private PerformanceCounter? _cpu;
        private PerformanceCounter? _disk;
        private CancellationTokenSource _cts = new();
        private CheckBox[] _tweakBoxes = Array.Empty<CheckBox>();
        private bool _applyBusy;

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buf);

        [StructLayout(LayoutKind.Sequential)]
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

        public Dashboard()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_cts.IsCancellationRequested)
                _cts = new CancellationTokenSource();

            _tweakBoxes = new[]
            {
                TweakKernel,
                TweakTcp,
                TweakMmcss,
                TweakRam,
                TweakDriver,
                TweakDebloat
            };

            LoadSystemInfo();
            InitPerformanceCounters();
            UpdateSelectionSummary();
            LanguageManager.LanguageChanged += OnLanguageChanged;
            OnLanguageChanged();
            StartLoop();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            _cpu?.Dispose();
            _disk?.Dispose();
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
            => Dispatcher.BeginInvoke(new Action(() =>
            {
                LanguageManager.LocalizeTree(this);
                BtnHeroApply.Content = LanguageManager.T("dashboard.hero_apply");
                BtnAdvancedFlow.Content = LanguageManager.T("dashboard.advanced_flow");
                BtnQuickBoostOpen.Content = LanguageManager.T("dashboard.quick_boost");
                UpdateSelectionSummary();
            }));

        private void LoadSystemInfo()
        {
            Task.Run(() =>
            {
                try
                {
                    using var cpuSearch = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
                    foreach (var o in cpuSearch.Get())
                    {
                        Dispatcher.Invoke(() => LiveCpu.Text = o["Name"]?.ToString()?.Trim() ?? "-");
                        break;
                    }
                }
                catch { }
            });

            Task.Run(() =>
            {
                try
                {
                    using var gpuSearch = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
                    foreach (var o in gpuSearch.Get())
                    {
                        Dispatcher.Invoke(() => LiveGpu.Text = o["Name"]?.ToString()?.Trim() ?? "-");
                        break;
                    }
                }
                catch { }
            });

            LiveOs.Text = $"Windows {Environment.OSVersion.Version.Major} {(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")}";
        }

        private void InitPerformanceCounters()
        {
            try
            {
                _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                _disk = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total", true);
                _cpu.NextValue();
                _disk.NextValue();
            }
            catch { }
        }

        private void StartLoop()
        {
            var token = _cts.Token;

            Task.Run(async () =>
            {
                using Ping ping = new();
                Stopwatch fpsTimer = new();
                fpsTimer.Start();

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        float cpu = _cpu?.NextValue() ?? 0f;

                        var mem = new MEMORYSTATUSEX
                        {
                            dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
                        };

                        GlobalMemoryStatusEx(ref mem);

                        float ramPct = mem.dwMemoryLoad;
                        double usedGb = (mem.ullTotalPhys - mem.ullAvailPhys) / (1024.0 * 1024 * 1024);
                        double totalGb = mem.ullTotalPhys / (1024.0 * 1024 * 1024);

                        long pingVal = -1;
                        try
                        {
                            var reply = await ping.SendPingAsync("8.8.8.8", 1000);
                            if (reply.Status == IPStatus.Success)
                                pingVal = reply.RoundtripTime;
                        }
                        catch { }

                        double frameTime = fpsTimer.Elapsed.TotalMilliseconds;
                        fpsTimer.Restart();

                        Dispatcher.Invoke(() =>
                        {
                            CpuVal.Text = $"{cpu:F1}%";
                            CpuStatus.Text = cpu < 50
                                ? LanguageManager.T("dashboard.cpu.normal")
                                : cpu < 80
                                    ? LanguageManager.T("dashboard.cpu.high")
                                    : LanguageManager.T("dashboard.cpu.critical");

                            RamVal.Text = $"{ramPct:F1}%";
                            RamDetail.Text = $"{usedGb:F1} GB / {totalGb:F1} GB";

                            PingVal.Text = pingVal >= 0 ? $"{LanguageManager.T("dashboard.ping")} {pingVal} ms" : $"{LanguageManager.T("dashboard.ping")} -";
                            FpsVal.Text = $"{LanguageManager.T("dashboard.frame")} {frameTime:F0} ms";
                        });
                    }
                    catch { }

                    await Task.Delay(2000, token);
                }
            }, token);
        }

        private void UpdateSelectionSummary()
        {
            int total = _tweakBoxes.Count(cb => cb.IsChecked == true);
            int engine = _tweakBoxes.Count(cb => cb.IsChecked == true && (cb.Tag?.ToString() ?? string.Empty) == "Engine");
            int system = _tweakBoxes.Count(cb => cb.IsChecked == true && (cb.Tag?.ToString() ?? string.Empty) == "System");
            int connection = _tweakBoxes.Count(cb => cb.IsChecked == true && (cb.Tag?.ToString() ?? string.Empty) == "Connection");
            int graphics = _tweakBoxes.Count(cb => cb.IsChecked == true && (cb.Tag?.ToString() ?? string.Empty) == "Graphics");

            SelectedTweaksValue.Text = total.ToString();
            SelectedCountText.Text = string.Format(LanguageManager.T("dashboard.selected_fmt"), total, _tweakBoxes.Length);
            EfficiencyText.Text = $"{(total / (double)_tweakBoxes.Length) * 100:F1}%";

            CountEngine.Text = engine.ToString();
            CountSystem.Text = system.ToString();
            CountConnection.Text = connection.ToString();
            CountGraphics.Text = graphics.ToString();

            BtnSelectAll.Content = total == _tweakBoxes.Length
                ? LanguageManager.T("dashboard.unselect_all")
                : LanguageManager.T("dashboard.select_all");
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            bool setAll = _tweakBoxes.Any(cb => cb.IsChecked != true);
            foreach (var box in _tweakBoxes)
                box.IsChecked = setAll;

            UpdateSelectionSummary();
        }

        private void Tweak_Checked(object sender, RoutedEventArgs e) => UpdateSelectionSummary();

        private void Card_SelectToggle(object sender, MouseButtonEventArgs e)
        {
            if (FindAncestor<CheckBox>(e.OriginalSource as DependencyObject) != null)
                return;

            if (sender is not FrameworkElement element || element.Tag is not string targetName)
                return;

            if (FindName(targetName) is CheckBox box)
            {
                box.IsChecked = box.IsChecked != true;
                e.Handled = true;
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            int total = _tweakBoxes.Count(cb => cb.IsChecked == true);
            if (total == 0)
            {
                MessageBox.Show(LanguageManager.T("dashboard.msg.none"), "Zantes Tweak", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var go = MessageBox.Show(
                string.Format(LanguageManager.T("dashboard.msg.redirect"), total),
                "Zantes Tweak",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (go == MessageBoxResult.Yes)
                NavigationService?.Navigate(new OptimizerPage());
        }

        private async void ApplySelectedNow_Click(object sender, RoutedEventArgs e)
        {
            if (_applyBusy)
                return;

            int total = _tweakBoxes.Count(cb => cb.IsChecked == true);
            if (total == 0)
            {
                MessageBox.Show(LanguageManager.T("dashboard.msg.none"), "Zantes Tweak", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!SystemTweakEngine.IsAdministrator())
            {
                MessageBox.Show(LanguageManager.T("dashboard.msg.need_admin"), "Zantes Tweak", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedIds = GetSelectedDashboardSelections();
            var tweaks = SmartOptimizeService.BuildDashboardApplyTweaks(selectedIds);
            if (tweaks.Count == 0)
            {
                MessageBox.Show(LanguageManager.T("dashboard.msg.no_plan"), "Zantes Tweak", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SetApplyBusy(true);
            try
            {
                var results = await SystemTweakEngine.ApplyAsync(tweaks, CancellationToken.None);
                int ok = results.Count(r => r.Success);
                int fail = results.Count - ok;
                MessageBox.Show(
                    string.Format(LanguageManager.T("dashboard.msg.applied"), ok, fail),
                    "Zantes Tweak",
                    MessageBoxButton.OK,
                    fail == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Zantes Tweak", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SetApplyBusy(false);
            }
        }

        private void Card_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border b)
                AnimateCardState(b, isHover: true);
        }

        private void Card_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border b)
                AnimateCardState(b, isHover: false);
        }

        private void Optimizer_Open(object sender, RoutedEventArgs e)
            => NavigationService?.Navigate(new OptimizerPage());

        private void Tuner_Open(object sender, RoutedEventArgs e)
            => NavigationService?.Navigate(new GameTunerPage());

        private void QuickBoost_Open(object sender, RoutedEventArgs e)
            => NavigationService?.Navigate(new QuickBoostPage());

        private string[] GetSelectedDashboardSelections()
        {
            var selected = new System.Collections.Generic.List<string>();
            if (TweakKernel.IsChecked == true)
                selected.Add("kernel");
            if (TweakTcp.IsChecked == true)
                selected.Add("tcp");
            if (TweakMmcss.IsChecked == true)
                selected.Add("mmcss");
            if (TweakRam.IsChecked == true)
                selected.Add("ram");
            if (TweakDriver.IsChecked == true)
                selected.Add("driver");
            if (TweakDebloat.IsChecked == true)
                selected.Add("debloat");

            return selected.ToArray();
        }

        private void SetApplyBusy(bool busy)
        {
            _applyBusy = busy;
            BtnHeroApply.IsEnabled = !busy;
            BtnAdvancedFlow.IsEnabled = !busy;
            BtnSelectAll.IsEnabled = !busy;
            BtnHeroApply.Content = busy
                ? LanguageManager.T("dashboard.applying")
                : LanguageManager.T("dashboard.hero_apply");
        }

        private static void AnimateCardState(Border card, bool isHover)
        {
            if (card.RenderTransform is not TransformGroup group || group.Children.Count < 2)
            {
                group = new TransformGroup();
                group.Children.Add(new ScaleTransform(1, 1));
                group.Children.Add(new TranslateTransform(0, 0));
                card.RenderTransform = group;
                card.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            var scale = (ScaleTransform)group.Children[0];
            var move = (TranslateTransform)group.Children[1];

            if (card.BorderBrush is not SolidColorBrush borderBrush || borderBrush.IsFrozen)
            {
                borderBrush = new SolidColorBrush(Color.FromRgb(58, 44, 94));
                card.BorderBrush = borderBrush;
            }

            if (card.Effect is not DropShadowEffect glow)
            {
                glow = new DropShadowEffect
                {
                    Color = Color.FromRgb(108, 43, 255),
                    BlurRadius = 0,
                    ShadowDepth = 0,
                    Opacity = 0
                };
                card.Effect = glow;
            }

            var duration = TimeSpan.FromMilliseconds(170);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(isHover ? 1.018 : 1.0, duration) { EasingFunction = ease });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(isHover ? 1.018 : 1.0, duration) { EasingFunction = ease });
            move.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(isHover ? -2.0 : 0.0, duration) { EasingFunction = ease });

            borderBrush.BeginAnimation(
                SolidColorBrush.ColorProperty,
                new ColorAnimation(isHover ? Color.FromRgb(108, 43, 255) : Color.FromRgb(58, 44, 94), duration) { EasingFunction = ease });

            glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, new DoubleAnimation(isHover ? 18 : 0, duration) { EasingFunction = ease });
            glow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(isHover ? 0.58 : 0.0, duration) { EasingFunction = ease });
        }

        private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
        {
            while (source != null)
            {
                if (source is T match)
                    return match;

                source = VisualTreeHelper.GetParent(source);
            }

            return null;
        }
    }
}

