using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ZantesEngine.Services;

namespace ZantesEngine.Pages
{
    public partial class Dashboard : Page
    {
        private bool _applyBusy;
        private readonly DoubleAnimation _spinnerAnimation = new()
        {
            From = 0,
            To = 360,
            Duration = TimeSpan.FromSeconds(1.1),
            RepeatBehavior = RepeatBehavior.Forever
        };

        public Dashboard()
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
            TxtHeroEyebrow.Text = tr ? "TEK DOKUNUS" : "ONE TAP";
            TxtHeroTitle.Text = tr ? "FPS optimize, tek komut." : "FPS optimize, one command.";
            TxtHeroBody.Text = tr
                ? "Once geri yukleme noktasi olusturur, sonra sadece kanit temelli FPS ayarlarini uygular."
                : "Creates a restore point first, then applies only evidence-backed FPS tweaks.";
            TxtHeroButton.Text = tr ? "CALISTIR" : "RUN";
            StatusText.Text = tr
                ? "Geri yukleme noktasi olusturup FPS-guvenli optimize uygulamaya hazir."
                : "Ready to create restore point and apply FPS-safe optimize.";
            OverlayTitle.Text = tr ? "FPS-guvenli optimize uygulaniyor..." : "Applying FPS-safe optimize...";
            OverlayStatus.Text = tr
                ? "Geri yukleme noktasi olusturuluyor, sonra karisik sonuc veren ayarlar atlanarak sadece FPS-guvenli set uygulanıyor."
                : "Creating restore point, then applying only the FPS-safe set while skipping mixed-result tweaks.";
            Dispatcher.BeginInvoke(new Action(() => LanguageManager.LocalizeTree(this)));
        }

        private async void ApplySelectedNow_Click(object sender, RoutedEventArgs e)
        {
            if (_applyBusy)
                return;

            if (!SystemTweakEngine.IsAdministrator())
            {
                bool tr = LanguageManager.CurrentLanguage == UiLanguage.Turkish;
                OverlayTitle.Text = tr ? "Yonetici yetkisi gerekli." : "Administrator privileges required.";
                OverlayStatus.Text = tr
                    ? "Bu akisi calistirmak icin uygulamayi yonetici olarak yeniden ac."
                    : "Re-open the app as administrator to run this flow.";
                StatusText.Text = OverlayStatus.Text;
                return;
            }

            SetBusy(true);
            try
            {
                var result = await OneTapOptimizeService.RunAsync(CancellationToken.None);
                OverlayTitle.Text = result.Success ? "Optimize complete." : "Optimize finished with warnings.";
                OverlayStatus.Text =
                    $"Restore: {result.RestoreMessage}{Environment.NewLine}" +
                    $"Mode: FPS-safe only{Environment.NewLine}" +
                    $"GPU preference: {result.UpdatedGpuPreferenceCount}{Environment.NewLine}" +
                    $"Process policy: {result.UpdatedProcessPolicyCount}{Environment.NewLine}" +
                    $"Success: {result.AppliedSuccessCount}  Failed: {result.AppliedFailCount}";

                StatusText.Text = result.Success
                    ? "FPS-safe optimize completed successfully."
                    : "FPS-safe optimize completed with warnings. Check the result summary.";

                await Task.Delay(1500);
                StatusText.Text =
                    $"{result.Message} " +
                    $"Success: {result.AppliedSuccessCount}  Failed: {result.AppliedFailCount}";
            }
            catch (Exception ex)
            {
                OverlayTitle.Text = "Optimize failed.";
                OverlayStatus.Text = ex.Message;
                StatusText.Text = "Optimize failed.";
                await Task.Delay(1200);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            _applyBusy = busy;
            BtnHeroApply.IsEnabled = !busy;

            if (busy)
            {
                StatusText.Text = "Creating restore point and applying the FPS-safe stack...";
                BtnScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.96, TimeSpan.FromMilliseconds(180)));
                BtnScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.96, TimeSpan.FromMilliseconds(180)));
                OverlayTitle.Text = "Applying FPS-safe optimize...";
                OverlayStatus.Text = "Creating restore point, then applying only evidence-backed FPS tweaks.";
                ProgressOverlay.Visibility = Visibility.Visible;
                ProgressOverlay.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(180)));
                SpinnerRotate.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, _spinnerAnimation);
            }
            else
            {
                BtnScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(180)));
                BtnScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(180)));
                SpinnerRotate.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, null);

                var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(220));
                fade.Completed += (_, _) => ProgressOverlay.Visibility = Visibility.Collapsed;
                ProgressOverlay.BeginAnimation(OpacityProperty, fade);
            }
        }
    }
}
