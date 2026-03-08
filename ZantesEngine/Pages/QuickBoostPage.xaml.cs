using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZantesEngine.Services;

namespace ZantesEngine.Pages
{
    public partial class QuickBoostPage : Page
    {
        private readonly Dictionary<CheckBox, string> _moduleMap = new();
        private readonly Dictionary<string, SmartModulePlan> _planMap = new(StringComparer.OrdinalIgnoreCase);
        private SmartOptimizePlan? _plan;
        private bool _busy;

        public QuickBoostPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_moduleMap.Count == 0)
                BindModuleMap();

            BuildSmartPlan();
            LanguageManager.LanguageChanged += OnLanguageChanged;
            OnLanguageChanged();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
            => LanguageManager.LanguageChanged -= OnLanguageChanged;

        private void BindModuleMap()
        {
            _moduleMap[CbPower] = "power";
            _moduleMap[CbGaming] = "gaming";
            _moduleMap[CbNetwork] = "network";
            _moduleMap[CbGpu] = "gpu";
            _moduleMap[CbServices] = "services";
            _moduleMap[CbMaintenance] = "maintenance";
        }

        private void BuildSmartPlan()
        {
            _plan = SmartOptimizeService.BuildPlan();
            _planMap.Clear();
            foreach (var module in _plan.Modules)
                _planMap[module.Id] = module;

            TxtHardwareSummary.Text = _plan.HardwareSummary;

            foreach (var pair in _moduleMap)
            {
                if (_planMap.TryGetValue(pair.Value, out var module))
                    pair.Key.IsChecked = module.DefaultEnabled;
            }

            RefreshReasonTexts();
            UpdateSelectedCount();
        }

        private void OnLanguageChanged()
            => Dispatcher.BeginInvoke(new Action(() =>
            {
                TxtModule.Text = LanguageManager.T("quick.module");
                TxtTitle.Text = LanguageManager.T("quick.title");
                TxtSubTitle.Text = LanguageManager.T("quick.subtitle");
                BtnSelectAll.Content = LanguageManager.T("quick.btn.select_all");
                TxtDeployLabel.Text = LanguageManager.T("quick.deploy.label");
                TxtDeployTitle.Text = LanguageManager.T("quick.deploy.title");
                TxtHardwareLabel.Text = LanguageManager.T("quick.hardware.label");
                CbCreateRestorePoint.Content = LanguageManager.T("quick.restore");
                BtnApplySmart.Content = LanguageManager.T("quick.btn.apply");
                BtnOpenAdvanced.Content = LanguageManager.T("quick.btn.advanced");
                TxtHint.Text = LanguageManager.T("quick.hint");

                TxtCardPower.Text = LanguageManager.T("quick.card.power");
                TxtCardPowerDesc.Text = LanguageManager.T("quick.card.power.desc");
                TxtCardGaming.Text = LanguageManager.T("quick.card.gaming");
                TxtCardGamingDesc.Text = LanguageManager.T("quick.card.gaming.desc");
                TxtCardNetwork.Text = LanguageManager.T("quick.card.network");
                TxtCardNetworkDesc.Text = LanguageManager.T("quick.card.network.desc");
                TxtCardGpu.Text = LanguageManager.T("quick.card.gpu");
                TxtCardGpuDesc.Text = LanguageManager.T("quick.card.gpu.desc");
                TxtCardServices.Text = LanguageManager.T("quick.card.services");
                TxtCardServicesDesc.Text = LanguageManager.T("quick.card.services.desc");
                TxtCardMaintenance.Text = LanguageManager.T("quick.card.maintenance");
                TxtCardMaintenanceDesc.Text = LanguageManager.T("quick.card.maintenance.desc");

                Dispatcher.BeginInvoke(new Action(() => LanguageManager.LocalizeTree(this)));
                RefreshReasonTexts();
                UpdateSelectedCount();
            }));

        private void RefreshReasonTexts()
        {
            TxtReasonPower.Text = ResolveReason("power");
            TxtReasonGaming.Text = ResolveReason("gaming");
            TxtReasonNetwork.Text = ResolveReason("network");
            TxtReasonGpu.Text = ResolveReason("gpu");
            TxtReasonServices.Text = ResolveReason("services");
            TxtReasonMaintenance.Text = ResolveReason("maintenance");
        }

        private string ResolveReason(string moduleId)
        {
            if (!_planMap.TryGetValue(moduleId, out var module))
                return "-";

            string text = LanguageManager.T(module.ReasonKey);
            if (module.ReasonArgs == null || module.ReasonArgs.Count == 0)
                return text;

            try
            {
                return string.Format(text, module.ReasonArgs.ToArray());
            }
            catch
            {
                return text;
            }
        }

        private void ModuleCheck_Changed(object sender, RoutedEventArgs e)
            => UpdateSelectedCount();

        private void ModuleCard_Click(object sender, MouseButtonEventArgs e)
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

        private void UpdateSelectedCount()
        {
            int selected = _moduleMap.Keys.Count(cb => cb.IsChecked == true);
            TxtSelectedModules.Text = string.Format(LanguageManager.T("quick.selected"), selected, _moduleMap.Count);
        }

        private void SetBusy(bool value)
        {
            _busy = value;
            BtnApplySmart.IsEnabled = !value;
            BtnOpenAdvanced.IsEnabled = !value;
            BtnSelectAll.IsEnabled = !value;
        }

        private void AppendOutput(string message)
        {
            OutputBox.Text += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
            OutputBox.ScrollToEnd();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            bool setAll = _moduleMap.Keys.Any(cb => cb.IsChecked != true);
            foreach (var cb in _moduleMap.Keys)
                cb.IsChecked = setAll;

            UpdateSelectedCount();
        }

        private async void ApplySmart_Click(object sender, RoutedEventArgs e)
        {
            if (_busy)
                return;

            if (!SystemTweakEngine.IsAdministrator())
            {
                AppendOutput(LanguageManager.T("quick.need_admin"));
                return;
            }

            var selectedModules = _moduleMap
                .Where(p => p.Key.IsChecked == true && _planMap.ContainsKey(p.Value))
                .Select(p => _planMap[p.Value])
                .ToArray();

            if (selectedModules.Length == 0)
            {
                AppendOutput(LanguageManager.T("quick.none"));
                return;
            }

            SetBusy(true);
            AppendOutput(LanguageManager.T("quick.apply_start"));

            if (CbCreateRestorePoint.IsChecked == true)
            {
                var restore = await Task.Run(() =>
                    SystemTweakEngine.CreateRestorePoint($"Zantes Tweak Smart Optimize {DateTime.Now:yyyy-MM-dd HH:mm:ss}"));
                AppendOutput(LanguageManager.LocalizeLiteral(restore.Message));
            }

            IReadOnlyList<SystemTweakResult> results;
            try
            {
                results = await SmartOptimizeService.ApplyModulesAsync(selectedModules, CancellationToken.None);
            }
            catch (Exception ex)
            {
                AppendOutput(ex.Message);
                SetBusy(false);
                return;
            }

            int ok = results.Count(r => r.Success);
            int fail = results.Count - ok;
            AppendOutput(string.Format(LanguageManager.T("quick.apply_done"), ok, fail));
            SetBusy(false);
        }

        private void OpenAdvanced_Click(object sender, RoutedEventArgs e)
            => NavigationService?.Navigate(new OptimizerPage());

        private void GoBack_Click(object sender, RoutedEventArgs e)
            => NavigationService?.GoBack();

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
