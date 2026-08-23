using Finvora.Models;
using System;
using System.Linq;
using System.Windows;

namespace Finvora.Services
{
    /// <summary>
    /// Owns runtime theme switching. Every view binds its colors via DynamicResource
    /// (not StaticResource), so swapping which Themes/*.xaml dictionary is merged into
    /// Application.Resources repaints the whole app instantly -- no restart needed.
    /// </summary>
    public class ThemeService
    {
        private const string AssemblyName = "Finvora";

        private static readonly (AppTheme Theme, string FileName)[] ThemeFiles =
        {
            (AppTheme.DarkNavy,       "DarkNavyTheme.xaml"),
            (AppTheme.Light,          "LightTheme.xaml"),
            (AppTheme.CyberDark,      "CyberDarkTheme.xaml"),
            (AppTheme.MidnightPurple, "MidnightPurpleTheme.xaml"),
            (AppTheme.EmeraldForest,  "EmeraldForestTheme.xaml"),
        };

        public AppTheme Current { get; private set; } = AppTheme.DarkNavy;

        /// <summary>Swaps the active theme dictionary. Safe to call repeatedly at runtime.</summary>
        public void ApplyTheme(AppTheme theme)
        {
            var app = Application.Current;
            if (app == null) return;

            var newDictionary = new ResourceDictionary { Source = BuildPackUri(theme) };
            var merged = app.Resources.MergedDictionaries;

            // Find whichever theme dictionary is currently loaded (matched by file name)
            // and replace just that one -- Typography.xaml / ViewTemplates.xaml are untouched.
            var existingIndex = -1;
            for (var i = 0; i < merged.Count; i++)
            {
                var source = merged[i].Source?.OriginalString ?? "";
                if (ThemeFiles.Any(t => source.EndsWith(t.FileName, StringComparison.OrdinalIgnoreCase)))
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                merged.RemoveAt(existingIndex);
                merged.Insert(existingIndex, newDictionary);
            }
            else
            {
                merged.Insert(0, newDictionary);
            }

            Current = theme;
        }

        private static Uri BuildPackUri(AppTheme theme)
        {
            var fileName = ThemeFiles.First(t => t.Theme == theme).FileName;
            return new Uri($"/{AssemblyName};component/Resources/Themes/{fileName}", UriKind.Relative);
        }
    }
}  