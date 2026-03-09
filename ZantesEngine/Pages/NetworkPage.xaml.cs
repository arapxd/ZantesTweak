using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using ZantesEngine.Services;

namespace ZantesEngine.Pages
{
    public partial class NetworkPage : Page
    {
        private bool _applyBusy;
        private bool _uiReady;
        private readonly DoubleAnimation _spinnerAnimation = new()
        {
            From = 0,
            To = 360,
            Duration = TimeSpan.FromSeconds(1.1),
            RepeatBehavior = RepeatBehavior.Forever
        };

        public NetworkPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            LanguageManager.LanguageChanged += OnLanguageChanged;
            Unloaded += OnUnloaded;
            ApplyLanguage();
            SelectDetectedVendor();
            UpdateSelectionSummary();
            _uiReady = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MainScrollViewer?.ScrollToTop();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void OnLanguageChanged()
        {
            ApplyLanguage();
            Dispatcher.BeginInvoke(new Action(() => LanguageManager.LocalizeTree(this)));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        private void ApplyLanguage()
        {
            bool tr = LanguageManager.CurrentLanguage == UiLanguage.Turkish;

            TxtEyebrow.Text = tr ? "AG + ARACLAR" : "NET + TOOLS";
            TxtTitle.Text = tr
                ? "Ag araclari ve surucu presetleri."
                : "Network tools and driver presets.";
            TxtSubtitle.Text = tr
                ? "Kartlari sec, sonra secili araci ya da surucu presetini uygula."
                : "Pick the cards you want, then apply the stack or a driver preset.";

            TxtStackPreviewLabel.Text = tr ? "YIGIN ONIZLEME" : "STACK PREVIEW";
            TxtToolsSectionLabel.Text = tr ? "AG YIGINI" : "NETWORK STACK";
            TxtDriverSectionLabel.Text = tr ? "SURUCU PRESETI" : "DRIVER PRESET";
            TxtDriverTitle.Text = tr ? "GPU ailesini kendin sec." : "Pick the GPU family yourself.";
            TxtDriverBody.Text = tr
                ? "NVIDIA, AMD veya Intel secip sadece o preset'i uygula."
                : "Choose NVIDIA, AMD, or Intel and apply only that preset.";
            TxtDriverPreviewLabel.Text = tr ? "SURUCU HEDEFI" : "DRIVER TARGET";
            TxtDriverPreviewHint.Text = tr ? "Manuel uretici preset'i hazir." : "Manual vendor preset is ready.";
            TxtFooterTitle.Text = tr ? "Sadece istedigini uygula." : "Apply only what you actually want.";
            TxtApplyToolsButton.Text = tr ? "SECILI ARACLARI UYGULA" : "APPLY SELECTED TOOLS";
            TxtApplyDriverButton.Text = tr ? "SURUCU PRESETINI UYGULA" : "APPLY DRIVER PRESET";

            TxtCardTcpOptimizerTitle.Text = "TCP OPTIMIZER";
            TxtCardTcpOptimizerDesc.Text = tr ? "TCP auto-tuning, RSS ve ag kisitlama ayarlarini uygular." : "Applies TCP auto-tuning, RSS, and throttle fixes.";
            TxtCardFlushDnsTitle.Text = tr ? "DNS TEMIZLE" : "FLUSH DNS";
            TxtCardFlushDnsDesc.Text = tr ? "Eski DNS kayitlarini aninda temizler." : "Clears cached DNS entries instantly.";
            TxtCardWinsockTitle.Text = tr ? "WINSOCK SIFIRLA" : "WINSOCK RESET";
            TxtCardWinsockDesc.Text = tr ? "Karismis soket katalogunu sifirlar." : "Resets a messy socket catalog.";
            TxtCardHeuristicsTitle.Text = tr ? "TCP HEURISTICS" : "TCP HEURISTICS";
            TxtCardHeuristicsDesc.Text = tr ? "TCP heuristics geri alma davranisini kapatir." : "Stops TCP heuristics from undoing tuning.";
            TxtCardChimneyTitle.Text = tr ? "TCP CHIMNEY" : "TCP CHIMNEY";
            TxtCardChimneyDesc.Text = tr ? "Eski chimney offload yolunu kapatir." : "Disables legacy chimney offload.";
            TxtCardTimestampsTitle.Text = tr ? "TCP TIMESTAMPS" : "TCP TIMESTAMPS";
            TxtCardTimestampsDesc.Text = tr ? "Timestamp yukunu kapatir." : "Disables timestamp overhead.";
            TxtCardSuperfetchTitle.Text = "SUPERFETCH";
            TxtCardSuperfetchDesc.Text = tr ? "SysMain arka plan yukunu keser." : "Cuts SysMain background disk churn.";
            TxtCardDynamicTickTitle.Text = tr ? "DYNAMIC TICK" : "DYNAMIC TICK";
            TxtCardDynamicTickDesc.Text = tr ? "Daha stabil timer icin dynamic tick'i kapatir." : "Disables dynamic tick for steadier timers.";
            TxtCardTaskOffloadTitle.Text = tr ? "TASK OFFLOADING" : "TASK OFFLOADING";
            TxtCardTaskOffloadDesc.Text = tr ? "Eski task offload yolunu kapatir." : "Turns off legacy task offload.";
            TxtCardNduTitle.Text = tr ? "NDU KAPAT" : "DISABLE NDU";
            TxtCardNduDesc.Text = tr ? "Veri kullanimi izleme yukunu kapatir." : "Turns off data usage monitoring overhead.";
            TxtCardDiagTitle.Text = tr ? "DIAG TRACK" : "DIAG TRACK";
            TxtCardDiagDesc.Text = tr ? "Arka plan telemetri servislerini durdurur." : "Stops background telemetry services.";

            TxtVendorNvidiaTitle.Text = "NVIDIA";
            TxtVendorNvidiaDesc.Text = tr ? "Game Mode, telemetri ve shader cache." : "Game Mode, telemetry, and shader cache.";
            TxtVendorAmdTitle.Text = "AMD";
            TxtVendorAmdDesc.Text = tr ? "Radeon icin dengeli preset." : "Balanced preset for Radeon systems.";
            TxtVendorIntelTitle.Text = "INTEL";
            TxtVendorIntelDesc.Text = tr ? "Hafif scheduler ve grafik preset'i." : "Lean scheduler and graphics preset.";

            if (!_applyBusy)
            {
                StatusText.Text = tr
                    ? "Geri yukleme noktasi ile secili araclari uygulamaya hazir."
                    : "Ready to create a restore point and apply the selected tools.";
                OverlayTitle.Text = tr ? "Arac yigini uygulaniyor..." : "Applying tool stack...";
                OverlayStatus.Text = tr
                    ? "Once geri yukleme noktasi, sonra secili servis ve ag araclari."
                    : "Restore point first, then selected services and network tools.";
            }
        }

        private void SelectDetectedVendor()
        {
            var detected = DriverPresetService.DetectPrimaryVendor();
            switch (detected.Vendor)
            {
                case GpuVendor.Nvidia:
                    VendorNvidia.IsChecked = true;
                    break;
                case GpuVendor.Amd:
                    VendorAmd.IsChecked = true;
                    break;
                case GpuVendor.Intel:
                    VendorIntel.IsChecked = true;
                    break;
                default:
                    VendorNvidia.IsChecked = true;
                    break;
            }

            TxtDriverPreviewValue.Text = detected.Vendor switch
            {
                GpuVendor.Nvidia => "NVIDIA",
                GpuVendor.Amd => "AMD",
                GpuVendor.Intel => "INTEL",
                _ => "NVIDIA"
            };
        }

        private void Vendor_Checked(object sender, RoutedEventArgs e)
        {
            if (!_uiReady || TxtDriverPreviewValue == null)
                return;

            TxtDriverPreviewValue.Text = GetSelectedVendor() switch
            {
                GpuVendor.Nvidia => "NVIDIA",
                GpuVendor.Amd => "AMD",
                GpuVendor.Intel => "INTEL",
                _ => "NVIDIA"
            };
        }

        private void ToolTile_Changed(object sender, RoutedEventArgs e)
        {
            if (!_uiReady || TxtSelectedCount == null || BtnApplyTools == null)
                return;

            UpdateSelectionSummary();
        }

        private void UpdateSelectionSummary()
        {
            int count = GetSelectedKeys().Count;
            bool tr = LanguageManager.CurrentLanguage == UiLanguage.Turkish;

            TxtSelectedCount.Text = count == 1 ? (tr ? "1 secili" : "1 selected") : (tr ? $"{count} secili" : $"{count} selected");
            TxtPreviewHint.Text = count == 0
                ? (tr ? "Baslamak icin en az bir kart sec." : "Pick at least one tool card to start.")
                : (tr ? "Once geri yukleme, sonra secili araclar." : "Restore point first, then selected tools.");
            BtnApplyTools.IsEnabled = !_applyBusy && count > 0;
        }

        private IReadOnlyList<string> GetSelectedKeys()
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var toggle in new ToggleButton[]
                     {
                         ToolTcpOptimizer,
                         ToolFlushDns,
                         ToolWinsockReset,
                         ToolTcpHeuristics,
                         ToolTcpChimney,
                         ToolTcpTimestamps,
                         ToolSuperfetch,
                         ToolDynamicTick,
                         ToolTaskOffloading,
                         ToolDisableNdu,
                         ToolDiagTrack
                     })
            {
                if (toggle.IsChecked != true || toggle.Tag is not string raw)
                    continue;

                foreach (string part in raw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    keys.Add(part);
            }

            return keys.ToList();
        }

        private GpuVendor GetSelectedVendor()
        {
            if (VendorAmd.IsChecked == true)
                return GpuVendor.Amd;
            if (VendorIntel.IsChecked == true)
                return GpuVendor.Intel;
            return GpuVendor.Nvidia;
        }

        private async void ApplyTools_Click(object sender, RoutedEventArgs e)
        {
            if (_applyBusy)
                return;

            if (!SystemTweakEngine.IsAdministrator())
            {
                OverlayTitle.Text = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Yonetici yetkisi gerekli." : "Administrator privileges required.";
                OverlayStatus.Text = LanguageManager.CurrentLanguage == UiLanguage.Turkish
                    ? "Bu akisi calistirmak icin uygulamayi yonetici olarak yeniden ac."
                    : "Re-open the app as administrator to run this flow.";
                StatusText.Text = OverlayStatus.Text;
                return;
            }

            var selectedKeys = GetSelectedKeys();
            if (selectedKeys.Count == 0)
                return;

            var tweaks = selectedKeys
                .Select(SystemTweakCatalog.Get)
                .Where(t => t != null)
                .Cast<SystemTweakDefinition>()
                .ToList();

            SetBusy(true, driverMode: false);
            try
            {
                var restore = SystemTweakEngine.CreateRestorePoint($"Zantes Tweak Net + Tools {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                if (!restore.Success)
                    throw new InvalidOperationException((LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Geri yukleme noktasi olusturulamadi: " : "Restore point failed: ") + restore.Message);

                var results = await SystemTweakEngine.ApplyAsync(tweaks, CancellationToken.None);
                int successCount = results.Count(r => r.Success);
                int failCount = results.Count - successCount;

                OverlayTitle.Text = failCount == 0
                    ? (LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Arac yigini tamamlandi." : "Tool stack complete.")
                    : (LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Arac yigini uyarilarla bitti." : "Tool stack finished with warnings.");
                OverlayStatus.Text = $"{(LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Geri yukleme" : "Restore")}: {restore.Message}{Environment.NewLine}{(LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Basarili" : "Success")}: {successCount}  {(LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Hatali" : "Failed")}: {failCount}";
                StatusText.Text = failCount == 0
                    ? (LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Secili araclar basariyla uygulandi." : "Selected tools applied successfully.")
                    : (LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Secili araclar uyarilarla bitti. Ozeti kontrol et." : "Selected tools finished with warnings. Review the summary.");

                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                OverlayTitle.Text = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Arac yigini basarisiz." : "Tool stack failed.";
                OverlayStatus.Text = ex.Message;
                StatusText.Text = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Arac yigini basarisiz." : "Tool stack failed.";
                await Task.Delay(900);
            }
            finally
            {
                SetBusy(false, driverMode: false);
            }
        }

        private async void ApplyDriverPreset_Click(object sender, RoutedEventArgs e)
        {
            if (_applyBusy)
                return;

            if (!SystemTweakEngine.IsAdministrator())
            {
                OverlayTitle.Text = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Yonetici yetkisi gerekli." : "Administrator privileges required.";
                OverlayStatus.Text = LanguageManager.CurrentLanguage == UiLanguage.Turkish
                    ? "Bu akisi calistirmak icin uygulamayi yonetici olarak yeniden ac."
                    : "Re-open the app as administrator to run this flow.";
                StatusText.Text = OverlayStatus.Text;
                return;
            }

            SetBusy(true, driverMode: true);
            try
            {
                var vendor = GetSelectedVendor();
                var result = await DriverPresetService.ApplyPresetAsync(vendor, true, CancellationToken.None);

                OverlayTitle.Text = result.Success
                    ? (LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Surucu preset'i tamamlandi." : "Driver preset complete.")
                    : (LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Surucu preset'i uyarilarla bitti." : "Driver preset finished with warnings.");
                OverlayStatus.Text = $"{result.VendorLabel}{Environment.NewLine}{result.Message}";
                StatusText.Text = result.Success
                    ? (LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Surucu preset'i basariyla uygulandi." : "Driver preset applied successfully.")
                    : (LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Surucu preset'i uyarilarla bitti." : "Driver preset finished with warnings.");

                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                OverlayTitle.Text = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Surucu preset'i basarisiz." : "Driver preset failed.";
                OverlayStatus.Text = ex.Message;
                StatusText.Text = LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Surucu preset'i basarisiz." : "Driver preset failed.";
                await Task.Delay(900);
            }
            finally
            {
                SetBusy(false, driverMode: true);
            }
        }

        private void SetBusy(bool busy, bool driverMode)
        {
            _applyBusy = busy;
            UpdateSelectionSummary();
            BtnApplyDriver.IsEnabled = !busy;

            if (busy)
            {
                StatusText.Text = driverMode
                    ? (LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Geri yukleme noktasi olusturuluyor ve surucu preset'i uygulaniyor..." : "Creating restore point and applying driver preset...")
                    : (LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Geri yukleme noktasi olusturuluyor ve secili araclar uygulaniyor..." : "Creating restore point and applying selected tools...");
                OverlayTitle.Text = driverMode
                    ? (LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Surucu preset'i uygulaniyor..." : "Applying driver preset...")
                    : (LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Arac yigini uygulaniyor..." : "Applying tool stack...");
                OverlayStatus.Text = driverMode
                    ? (LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Once geri yukleme noktasi, sonra secilen GPU preset'i." : "Restore point first, then the selected GPU preset.")
                    : (LanguageManager.CurrentLanguage == UiLanguage.Turkish ? "Once geri yukleme noktasi, sonra secili servis ve ag araclari." : "Restore point first, then selected services and network tools.");
                ProgressOverlay.Visibility = Visibility.Visible;
                ProgressOverlay.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(180)));
                SpinnerRotate.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, _spinnerAnimation);
            }
            else
            {
                SpinnerRotate.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, null);
                var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(220));
                fade.Completed += (_, _) => ProgressOverlay.Visibility = Visibility.Collapsed;
                ProgressOverlay.BeginAnimation(OpacityProperty, fade);
            }
        }
    }
}
