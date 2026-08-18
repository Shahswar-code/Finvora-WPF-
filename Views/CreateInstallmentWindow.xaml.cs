using System.Windows;
using System.Windows.Input;
using Finvora.ViewModels;

namespace Finvora.Views
{
    public partial class CreateInstallmentWindow : Window
    {
        public CreateInstallmentWindow(CreateInstallmentViewModel viewModel)
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