using System;
using System.Diagnostics;
using System.Threading.Tasks;
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
        private static readonly TimeSpan MinimumSplashDuration = TimeSpan.FromSeconds(5);

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var splash = new SplashWindow();
            splash.Show();

            // Starts immediately and runs for MinimumSplashDuration on its own timeline.
            // This is what guarantees the splash stays visible for at least that long.
            var loadingSequenceTask = splash.PlayLoadingSequenceAsync(MinimumSplashDuration);

            try
            {
                var orchestrator = new StartupOrchestrator(new IStartupTask[]
{
                           new EnsureAppDataFolderTask(),
                           new EnsureDatabaseTask(),
                    // Future phases add real startup work here, e.g.:
                    // new LoadSettingsTask(),
                    // new EnsureDatabaseTask(),
                    // new CheckDueBackupTask(),
                });

                // The splash UI is now driven entirely by PlayLoadingSequenceAsync above,
                // so this callback isn't wired to the UI anymore — it's just a hook you can
                // use for logging/diagnostics as real tasks get added in later phases.
                var progress = new Progress<StartupProgress>(p =>
                    Debug.WriteLine($"[Startup] {p.StatusText} ({p.PercentComplete:0}%)"));

                await orchestrator.RunAsync(progress);
                await loadingSequenceTask;
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

            await splash.FadeOutAsync();
            splash.Close();

            mainWindow.Show();
        }
    }
}  