using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
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

            Loaded += SplashWindow_Loaded;
        } 

        private void SplashWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fadeIn);
        }

        /// <summary>
        /// Plays the full splash "loading" sequence: a smooth left-to-right fill of the
        /// progress bar across <paramref name="duration"/>, paired with three status
        /// messages shown one after another ("Initializing...", "Getting ready...", "Ready!").
        /// Completes once both the bar and the message sequence have finished.
        /// </summary>
        public async Task PlayLoadingSequenceAsync(TimeSpan duration)
        {
            // Bar fill animates smoothly across the *entire* duration.
            var fillCompleted = new TaskCompletionSource();
            var fillAnimation = new DoubleAnimation(0, 1, duration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            fillAnimation.Completed += (_, _) => fillCompleted.SetResult();
            ProgressFillScale.BeginAnimation(ScaleTransform.ScaleXProperty, fillAnimation);

            // Status text cycles through 3 phases, each roughly a third of the duration.
            var phase = TimeSpan.FromTicks(duration.Ticks / 3);

            ViewModel.StatusText = "Initializing...";
            await Task.Delay(phase);

            ViewModel.StatusText = "Getting ready...";
            await Task.Delay(phase);

            ViewModel.StatusText = "Ready!";

            // Wait out whatever's left, and make sure the bar animation has actually finished.
            await Task.WhenAll(Task.Delay(phase), fillCompleted.Task);
        }

        /// <summary>
        /// Fades the splash screen out and completes once the animation finishes.
        /// Call this instead of Close() directly so the exit is smooth, not abrupt.
        /// </summary>
        public Task FadeOutAsync()
        {
            var tcs = new TaskCompletionSource();

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (_, _) => tcs.SetResult();

            BeginAnimation(OpacityProperty, fadeOut);

            return tcs.Task;
        }
    }
} 