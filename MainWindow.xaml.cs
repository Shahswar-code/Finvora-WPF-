using System;
using System.Windows;
using System.Windows.Input;

namespace Finvora
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Size relative to the actual screen so the window always fits and
            // starts centered, however big or small the user's display is.
            var workArea = SystemParameters.WorkArea;
            Width = Math.Min(1360, workArea.Width * 0.9);
            Height = Math.Min(800, workArea.Height * 0.9);
            Left = workArea.Left + (workArea.Width - Width) / 2;
            Top = workArea.Top + (workArea.Height - Height) / 2;

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fadeIn);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximizeRestore();
            }
            else
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaximizeRestore_Click(object sender, RoutedEventArgs e) => ToggleMaximizeRestore();

        private void ToggleMaximizeRestore()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                var border = SystemParameters.WindowResizeBorderThickness;
                RootGrid.Margin = new Thickness(border.Left, border.Top, border.Right, border.Bottom);
                MaximizeButton.Content = "\uE923";
            }
            else
            {
                RootGrid.Margin = new Thickness(0);
                MaximizeButton.Content = "\uE922";
            }
        }
    }
}  