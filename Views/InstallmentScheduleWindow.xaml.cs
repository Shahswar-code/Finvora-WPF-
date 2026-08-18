using System.Windows;
using System.Windows.Input;
using Finvora.ViewModels;

namespace Finvora.Views
{
    public partial class InstallmentScheduleWindow : Window
    {
        public InstallmentScheduleWindow(InstallmentScheduleViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            viewModel.RequestClose += Close;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
} 