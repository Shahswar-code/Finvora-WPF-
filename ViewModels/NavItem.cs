using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Finvora.ViewModels
{
    /// <summary>
    /// One button in the sidebar. Clicking it asks _pageViewModelFactory to build
    /// whatever ViewModel that screen needs; the Shell never needs to know which.
    /// </summary>
    public partial class NavItem : ObservableObject
    {
        public string Label { get; }
        public string Glyph { get; }

        [ObservableProperty]
        private bool isSelected;

        private readonly Func<object> _pageViewModelFactory;

        public NavItem(string label, string glyph, Func<object> pageViewModelFactory)
        {
            Label = label;
            Glyph = glyph;
            _pageViewModelFactory = pageViewModelFactory;
        }

        public object CreatePageViewModel() => _pageViewModelFactory();
    }
}  