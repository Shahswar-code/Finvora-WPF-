using System;
using System.Windows;
using Finvora.Services.Startup;
using Finvora.Services.Startup.Tasks;
using Finvora.Views;

namespace Finvora
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var splash = new SplashWindow();
            splash.Show();

            try
            {
                var orchestrator = new StartupOrchestrator(new IStartupTask[]
                {
                    new EnsureAppDataFolderTask(),
                    // Future phases add real startup work here, e.g.:
                    // new LoadSettingsTask(),
                    // new EnsureDatabaseTask(),
                    // new CheckDueBackupTask(),
                });

                var progress = new Progress<StartupProgress>(p =>
                {
                    splash.ViewModel.StatusText = p.StatusText;
                    splash.ViewModel.ProgressPercent = p.PercentComplete;
                });

                await orchestrator.RunAsync(progress);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"FINVORA couldn't start correctly.\n\n{ex.Message}",
                    "Startup error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                splash.Close();
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();

            splash.Close();
        }
    }
}
