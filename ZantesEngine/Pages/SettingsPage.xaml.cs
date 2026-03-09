using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ZantesEngine.Services;

namespace ZantesEngine.Pages
{
    public partial class SettingsPage : Page
    {
        private bool _syncing;

        public SettingsPage()
        {
            InitializeComponent();
            LanguageManager.LanguageChanged += HandleLanguageChanged;
            Unloaded += OnUnloaded;
        }

        private MainWindow? Host => Application.Current.MainWindow as MainWindow;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyLanguage();
            RefreshFromHost();
        }

        private void HandleLanguageChanged()
        {
            ApplyLanguage();
            RefreshFromHost();
            Dispatcher.BeginInvoke(new Action(() => LanguageManager.LocalizeTree(this)));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
            => LanguageManager.LanguageChanged -= HandleLanguageChanged;

        private void ApplyLanguage()
        {
            bool tr = LanguageManager.CurrentLanguage == UiLanguage.Turkish;

            TxtHeaderEyebrow.Text = tr ? "AYARLAR" : "SETTINGS";
            TxtHeaderTitle.Text = tr
                ? "Kabugu, hesabi ve guncelleme akislarini buradan yonet."
                : "Control the shell, account, and update flow.";
            TxtHeaderBody.Text = tr
                ? "Dil artik burada. Hesap durumu, Discord Rich Presence, guncelleme kontrolu ve pencere davranisi tek yerde toplandi."
                : "Language now lives here. Account state, Discord Rich Presence, update checks, and shell behavior are all kept in one cleaner place.";

            TxtAccountLabel.Text = tr ? "HESAP" : "ACCOUNT";
            BtnSwitchAccount.Content = tr ? "HESABI DEGISTIR" : "SWITCH ACCOUNT";
            BtnLogout.Content = tr ? "CIKIS YAP" : "LOGOUT";

            TxtPresenceTitle.Text = tr ? "DISCORD RICH PRESENCE" : "DISCORD RICH PRESENCE";
            TxtPresenceBody.Text = tr
                ? "Zantes Tweak etkinligini Discord profilinde goster."
                : "Show your current Zantes Tweak activity on Discord.";
            TxtPinTitle.Text = tr ? "PENCEREYI SABITLE" : "PIN WINDOW";
            TxtPinBody.Text = tr
                ? "Kabugu her zaman ustte tut."
                : "Keep the shell always on top.";
            TxtAutoUpdateTitle.Text = tr ? "OTOMATIK GUNCELLEME KONTROLU" : "AUTO UPDATE CHECK";
            TxtAutoUpdateBody.Text = tr
                ? "Kabuk acildiginda GitHub Releases'i otomatik kontrol et."
                : "Check GitHub Releases automatically when the shell opens.";
            TxtPreferencesLabel.Text = tr ? "KABUK SECENEKLERI" : "SHELL OPTIONS";

            TxtLanguageCardLabel.Text = tr ? "DIL" : "LANGUAGE";
            TxtLanguageHint.Text = tr
                ? "Buradan degistirdiginde kabuk aninda guncellenir."
                : "The shell updates instantly when you switch it here.";
            TxtUpdateCardLabel.Text = tr ? "GUNCELLEME KANALI" : "UPDATE CHANNEL";
            BtnCheckUpdates.Content = tr ? "SIMDI KONTROL ET" : "CHECK NOW";
            BtnOpenRelease.Content = tr ? "RELEASE SAYFASINI AC" : "OPEN RELEASE PAGE";
        }

        private void RefreshFromHost()
        {
            var host = Host;
            if (host == null)
                return;

            _syncing = true;
            SyncLanguageOptions();
            TxtAccountName.Text = host.CurrentAccountName;
            TxtAccountState.Text = host.CurrentSessionState;
            ToggleRichPresence.IsChecked = host.CurrentRichPresenceEnabled;
            ToggleAlwaysOnTop.IsChecked = host.CurrentAlwaysOnTop;
            ToggleAutoUpdate.IsChecked = host.CurrentAutoCheckUpdates;
            TxtLanguageValue.Text = host.CurrentLanguageName;
            TxtVersionValue.Text = host.CurrentVersionText;
            TxtUpdateStatus.Text = host.CurrentUpdateStatusText;

            foreach (var raw in LanguageSelector.Items)
            {
                if (raw is ComboBoxItem item && item.Tag is string tag && tag == host.CurrentLanguageCode)
                {
                    LanguageSelector.SelectedItem = item;
                    break;
                }
            }

            _syncing = false;
        }

        private void SyncLanguageOptions()
        {
            foreach (var raw in LanguageSelector.Items)
            {
                if (raw is not ComboBoxItem item || item.Tag is not string tag)
                    continue;

                item.Content = tag == "en" ? "English" : "Turkish";
            }
        }

        private void ToggleRichPresence_Click(object sender, RoutedEventArgs e)
        {
            if (_syncing || Host == null)
                return;

            Host.SetRichPresenceFromPage(ToggleRichPresence.IsChecked == true);
            RefreshFromHost();
        }

        private void ToggleAlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            if (_syncing || Host == null)
                return;

            Host.SetAlwaysOnTopFromPage(ToggleAlwaysOnTop.IsChecked == true);
            RefreshFromHost();
        }

        private void ToggleAutoUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_syncing || Host == null)
                return;

            Host.SetAutoUpdateChecksFromPage(ToggleAutoUpdate.IsChecked == true);
            RefreshFromHost();
        }

        private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncing || Host == null)
                return;

            if (LanguageSelector.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
                return;

            Host.SetLanguageFromPage(tag);
        }

        private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            if (Host == null)
                return;

            await Host.CheckForUpdatesFromPageAsync();
            RefreshFromHost();
        }

        private void BtnOpenRelease_Click(object sender, RoutedEventArgs e)
        {
            if (Host == null)
                return;

            if (!Host.OpenReleasePageFromPage())
            {
                MessageBox.Show("Release page could not be opened.", "Zantes Tweak", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void BtnSwitchAccount_Click(object sender, RoutedEventArgs e)
        {
            if (Host == null)
                return;

            await Host.SwitchAccountFromPageAsync();
            RefreshFromHost();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (Host == null)
                return;

            Host.LogoutFromPage();
            RefreshFromHost();
        }
    }
}
