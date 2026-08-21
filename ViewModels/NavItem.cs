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

        /// <summary>Unread-count badge shown next to the label. Only the
        /// Notifications item sets this above zero today, but it's generic so
        /// any future nav item can use it the same way.</summary>
        [ObservableProperty]
        private int badgeCount;

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