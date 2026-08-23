using CommunityToolkit.Mvvm.ComponentModel;
using Finvora.Models;
using System.Windows.Media;

namespace Finvora.ViewModels
{
    /// <summary>
    /// Display wrapper for one theme swatch card in Settings. Preview colors are
    /// hardcoded to mirror Resources/Themes/{Name}Theme.xaml exactly, so a swatch
    /// always shows that theme's true look, independent of the currently active theme.
    /// </summary>
    public partial class ThemeOption : ObservableObject
    {
        public AppTheme Theme { get; }
        public string DisplayName { get; }
        public Brush BackgroundPreview { get; }
        public Brush AccentPreview { get; }

        [ObservableProperty] private bool isSelected;

        public ThemeOption(AppTheme theme, string displayName, string backgroundHex, string accentHex)
        {
            Theme = theme;
            DisplayName = displayName;
            BackgroundPreview = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundHex));
            AccentPreview = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentHex));
            BackgroundPreview.Freeze();
            AccentPreview.Freeze();
        }
    }
}  