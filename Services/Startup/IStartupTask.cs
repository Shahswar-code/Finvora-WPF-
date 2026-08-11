using System.Threading.Tasks;

namespace Finvora.Services.Startup
{
    /// <summary>
    /// A single unit of real application startup work.
    /// Each task reports what it is doing so the splash screen can show genuine
    /// progress instead of a fake timed animation.
    ///
    /// Later phases add more implementations here (e.g. loading settings,
    /// verifying the database, checking for a due backup) — the splash screen
    /// and App.xaml.cs never need to change when a new task is added.
    /// </summary>
    public interface IStartupTask
    {
        /// <summary>Short label shown on the splash screen while this task runs.</summary>
        string StatusText { get; }

        Task ExecuteAsync();
    }
}
