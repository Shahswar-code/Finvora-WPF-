using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Finvora.ViewModels;

namespace Finvora.Views
{
    public partial class PinDialogWindow : Window
    {
        /// <summary>true = PIN action succeeded, false/null = cancelled.</summary>
        public bool? Result { get; private set; }

        public PinDialogWindow(PinDialogViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            viewModel.RequestClose += success =>
            {
                Result = success;
                Close();
            };
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        private void CurrentPinBox_PasswordChanged(object sender, RoutedEventArgs e) =>
            ((PinDialogViewModel)DataContext).CurrentPin = ((PasswordBox)sender).Password;

        private void NewPinBox_PasswordChanged(object sender, RoutedEventArgs e) =>
            ((PinDialogViewModel)DataContext).NewPin = ((PasswordBox)sender).Password;

        private void ConfirmPinBox_PasswordChanged(object sender, RoutedEventArgs e) =>
            ((PinDialogViewModel)DataContext).ConfirmPin = ((PasswordBox)sender).Password;

        private void VerifyPinBox_PasswordChanged(object sender, RoutedEventArgs e) =>
            ((PinDialogViewModel)DataContext).VerifyPin = ((PasswordBox)sender).Password;
    }
}  