using System.Windows;
using Finvora.ViewModels;

namespace Finvora.Views
{
    public partial class SplashWindow : Window
    {
        public SplashViewModel ViewModel { get; }

        public SplashWindow()
        {
            InitializeComponent();

            ViewModel = new SplashViewModel();
            DataContext = ViewModel;
        }
    }
}
