using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ZantesEngine.Services;

namespace ZantesEngine
{
    public partial class SplashWindow : Window
    {
        private readonly Random _rnd = new();
        private double _w, _h;
        private bool _isFirstBoot;
        private readonly (string StatusKey, string HintKey, double Progress, int DelayMs)[] _bootSequence =
        {
            ("splash.step.sync.status", "splash.step.sync.hint", 23, 300),
            ("splash.step.engine.status", "splash.step.engine.hint", 51, 330),
            ("splash.step.network.status", "splash.step.network.hint", 78, 300),
            ("splash.step.final.status", "splash.step.final.hint", 100, 260)
        };
        private readonly (string StatusKey, string HintKey, double Progress, int DelayMs)[] _firstBootSequence =
        {
            ("splash.first.provision.status", "splash.first.provision.hint", 18, 520),
            ("splash.first.scan.status", "splash.first.scan.hint", 39, 520),
            ("splash.first.layer.status", "splash.first.layer.hint", 66, 520),
            ("splash.first.ux.status", "splash.first.ux.hint", 88, 460),
            ("splash.step.final.status", "splash.step.final.hint", 100, 360)
        };

        public SplashWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _w = ActualWidth > 0 ? ActualWidth : SystemParameters.PrimaryScreenWidth;
                _h = ActualHeight > 0 ? ActualHeight : SystemParameters.PrimaryScreenHeight;
                var settings = AppSettingsService.Load();
                LanguageManager.SetLanguage(settings.PreferredLanguage.Equals("tr", StringComparison.OrdinalIgnoreCase)
                    ? UiLanguage.Turkish
                    : UiLanguage.English);
                _isFirstBoot = !settings.HasSeenFirstBoot;
                ApplyLanguage();

                if (_isFirstBoot)
                {
                    FirstBootBadge.Visibility = Visibility.Visible;
                    BootModeHint.Text = LanguageManager.T("splash.mode.first");
                }
                else
                {
                    BootModeHint.Text = LanguageManager.T("splash.mode.standard");
                }

                UpdateBootState(LanguageManager.T("splash.prepare.status"), LanguageManager.T("splash.prepare.hint"), 5);
                ParticleCanvas.Visibility = Visibility.Visible;
                SpawnParticles();

                await Task.Delay(80);
                await AnimateIn();
                await RunBootSequence();
                await Task.Delay(180);

                if (_isFirstBoot)
                {
                    settings.HasSeenFirstBoot = true;
                    AppSettingsService.Save(settings);
                }

                if (Application.Current is App app)
                {
                    if (!app.TryOpenMainWindow(this))
                        return;
                }
                else
                {
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    Close();
                }
            }
            catch (Exception ex)
            {
                App.LogException("Splash startup sequence failed.", ex);

                if (Application.Current is App app)
                    app.HandleFatalException("Startup sequence failed after splash screen.", ex, shutdown: true);
                else
                    MessageBox.Show(ex.Message, "Zantes Tweak Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SpawnParticles()
        {
            for (int i = 0; i < 24; i++)
            {
                double size  = _rnd.NextDouble() * 2 + 0.5;
                double x     = _rnd.NextDouble() * _w;
                double y     = _rnd.NextDouble() * _h;
                double delay = _rnd.NextDouble() * 2.5;
                double dur   = _rnd.NextDouble() * 4 + 5;
                double moveY = _rnd.NextDouble() * 52 + 18;
                byte   alpha = (byte)_rnd.Next(45, 130);

                var dot = new Ellipse
                {
                    Width  = size,
                    Height = size,
                    Fill   = new SolidColorBrush(
                                 Color.FromArgb(alpha, 182, 149, 255)),
                    Opacity = 0
                };

                Canvas.SetLeft(dot, x);
                Canvas.SetTop(dot, y);
                ParticleCanvas.Children.Add(dot);

                var fadeLoop = new DoubleAnimationUsingKeyFrames
                {
                    BeginTime      = TimeSpan.FromSeconds(delay),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                fadeLoop.KeyFrames.Add(new EasingDoubleKeyFrame(0,
                    KeyTime.FromTimeSpan(TimeSpan.Zero)));
                fadeLoop.KeyFrames.Add(new EasingDoubleKeyFrame(1,
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(dur * 0.3)),
                    new SineEase { EasingMode = EasingMode.EaseInOut }));
                fadeLoop.KeyFrames.Add(new EasingDoubleKeyFrame(0,
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(dur)),
                    new SineEase { EasingMode = EasingMode.EaseInOut }));
                dot.BeginAnimation(OpacityProperty, fadeLoop);

                var tt = new TranslateTransform();
                dot.RenderTransform = tt;
                tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation
                {
                    From           = 0,
                    To             = -moveY,
                    Duration       = TimeSpan.FromSeconds(dur),
                    BeginTime      = TimeSpan.FromSeconds(delay),
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                });
            }
        }

        private async Task AnimateIn()
        {
            FadeEl(ContentPanel, 0.0, 0.45, 0, 1);
            MoveY(ContentPanel, 0.0, 0.45, 14, 0);
            FadeEl(HeroFrame, 0.08, 0.42, 0, 1);
            MoveY(HeroFrame, 0.08, 0.42, 16, 0);
            FadeEl(BottomLine, 0.1, 0.4, 0, 0.82);
            await Task.Delay(520);
            PulseEl(Dot, 1.0, 0.26, 1.0);
        }

        private async Task AnimateOut()
        {
            FadeEl(this, 0.0, 0.55, 1, 0);
            await Task.Delay(600);
        }

        private async Task RunBootSequence()
        {
            var sequence = _isFirstBoot ? _firstBootSequence : _bootSequence;
            foreach (var step in sequence)
            {
                UpdateBootState(LanguageManager.T(step.StatusKey), LanguageManager.T(step.HintKey), step.Progress);
                AnimateProgress(step.Progress, 0.32);
                await Task.Delay(step.DelayMs);
            }
        }

        private void ApplyLanguage()
        {
            TxtSplashLayerLabel.Text = LanguageManager.T("splash.layer");
            TxtSplashFirstBootLabel.Text = LanguageManager.T("splash.first_boot");
            TxtSplashSubtitle.Text = LanguageManager.T("splash.subtitle");
            TxtSplashDesc.Text = LanguageManager.T("splash.desc");
            TxtSplashBrand.Text = LanguageManager.T("brand.credit");
            TxtSplashDeployLabel.Text = LanguageManager.T("splash.deploy");
            TxtSplashShellLabel.Text = LanguageManager.T("splash.shell.label");
            TxtSplashShellValue.Text = LanguageManager.T("splash.shell.value");
            TxtSplashProfileLabel.Text = LanguageManager.T("splash.profile.label");
            TxtSplashProfileValue.Text = LanguageManager.T("splash.profile.value");
            TxtSplashLoadingModules.Text = $"  {LanguageManager.T("splash.loading_modules")}";
        }

        private void UpdateBootState(string status, string hint, double progress)
        {
            BootStatus.Text = status;
            BootHint.Text = hint;
            BootPercent.Text = $"{Math.Round(progress):F0}%";
        }

        private void AnimateProgress(double to, double durationSec)
        {
            BootProgress.BeginAnimation(ProgressBar.ValueProperty, new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromSeconds(durationSec),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        private void FadeEl(UIElement el, double begin, double dur,
                            double from, double to)
        {
            el.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From           = from,
                To             = to,
                Duration       = TimeSpan.FromSeconds(dur),
                BeginTime      = TimeSpan.FromSeconds(begin),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        private void MoveY(UIElement el, double begin, double dur, double from, double to)
        {
            if (el.RenderTransform is not TranslateTransform tt)
            {
                tt = new TranslateTransform();
                el.RenderTransform = tt;
            }

            tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(dur),
                BeginTime = TimeSpan.FromSeconds(begin),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        private void PulseEl(UIElement el, double from, double to, double dur)
        {
            var p = new DoubleAnimationUsingKeyFrames
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            p.KeyFrames.Add(new EasingDoubleKeyFrame(from,
                KeyTime.FromTimeSpan(TimeSpan.Zero)));
            p.KeyFrames.Add(new EasingDoubleKeyFrame(to,
                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(dur / 2)),
                new SineEase { EasingMode = EasingMode.EaseInOut }));
            p.KeyFrames.Add(new EasingDoubleKeyFrame(from,
                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(dur)),
                new SineEase { EasingMode = EasingMode.EaseInOut }));
            el.BeginAnimation(OpacityProperty, p);
        }
    }
}

