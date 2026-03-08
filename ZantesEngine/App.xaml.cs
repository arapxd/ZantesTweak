using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ZantesEngine
{
    public partial class App : Application
    {
        private bool _fatalDialogShown;

        protected override void OnStartup(StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            RegisterGlobalExceptionHandlers();

            base.OnStartup(e);

            try
            {
                var splash = new SplashWindow();
                MainWindow = splash;
                splash.Show();
            }
            catch (Exception ex)
            {
                HandleFatalException("Splash window could not be opened.", ex, shutdown: true);
            }
        }

        internal bool TryOpenMainWindow(Window? splashWindow)
        {
            try
            {
                var mainWindow = new MainWindow();
                MainWindow = mainWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();

                if (splashWindow != null && splashWindow.IsVisible)
                    splashWindow.Close();

                return true;
            }
            catch (Exception ex)
            {
                HandleFatalException("Main window could not be opened.", ex, shutdown: true);
                return false;
            }
        }

        internal void HandleFatalException(string context, Exception ex, bool shutdown)
        {
            LogException(context, ex);
            ShowFatalDialog(context, ex);

            if (shutdown)
                Shutdown(-1);
        }

        internal static void LogException(string context, Exception ex)
        {
            try
            {
                string logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ZantesEngine",
                    "logs");
                Directory.CreateDirectory(logDirectory);

                string logPath = Path.Combine(logDirectory, "startup.log");
                var sb = new StringBuilder();
                sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}");
                sb.AppendLine(ex.ToString());
                sb.AppendLine(new string('-', 80));
                File.AppendAllText(logPath, sb.ToString());
            }
            catch
            {
                // Logging must never take the app down.
            }
        }

        private void RegisterGlobalExceptionHandlers()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleFatalException("Unhandled UI exception.", e.Exception, shutdown: true);
            e.Handled = true;
        }

        private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                HandleFatalException("Unhandled domain exception.", ex, shutdown: true);
            else
                LogException("Unhandled domain exception.", new Exception("Unknown non-Exception object received."));
        }

        private void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException("Unobserved task exception.", e.Exception);
            e.SetObserved();
        }

        private void ShowFatalDialog(string context, Exception ex)
        {
            if (_fatalDialogShown)
                return;

            _fatalDialogShown = true;

            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ZantesEngine",
                "logs",
                "startup.log");

            string message =
                $"{context}{Environment.NewLine}{Environment.NewLine}" +
                $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                $"A diagnostic log was written to:{Environment.NewLine}{logPath}";

            void Show() => MessageBox.Show(
                message,
                "Zantes Tweak Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            if (Dispatcher.CheckAccess())
                Show();
            else
                Dispatcher.Invoke(Show);
        }
    }
}
