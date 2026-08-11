using CommunityToolkit.Mvvm.ComponentModel;

namespace Finvora.ViewModels
{
    public partial class SplashViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _statusText = "Starting...";

        [ObservableProperty]
        private double _progressPercent;
    }
}
