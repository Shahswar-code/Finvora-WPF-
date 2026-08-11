using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Finvora.Services.Startup
{
    /// <summary>
    /// Snapshot of startup progress reported to the splash screen after each task.
    /// </summary>
    public readonly record struct StartupProgress(string StatusText, int CompletedCount, int TotalCount)
    {
        public double PercentComplete => TotalCount == 0 ? 100 : (double)CompletedCount / TotalCount * 100;
    }

    /// <summary>
    /// Executes the application's startup tasks in order, on a background thread,
    /// reporting progress back to the UI thread via IProgress&lt;T&gt;.
    /// </summary>
    public class StartupOrchestrator
    {
        private readonly IReadOnlyList<IStartupTask> _tasks;

        public StartupOrchestrator(IReadOnlyList<IStartupTask> tasks)
        {
            _tasks = tasks;
        }

        public async Task RunAsync(IProgress<StartupProgress> progress)
        {
            for (int i = 0; i < _tasks.Count; i++)
            {
                var task = _tasks[i];
                progress.Report(new StartupProgress(task.StatusText, i, _tasks.Count));

                await Task.Run(task.ExecuteAsync).ConfigureAwait(false);
            }

            progress.Report(new StartupProgress("Ready", _tasks.Count, _tasks.Count));
        }
    }
}
