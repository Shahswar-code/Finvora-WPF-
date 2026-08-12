using CommunityToolkit.Mvvm.ComponentModel;

namespace Finvora.ViewModels
{
    public partial class ComingSoonViewModel : ObservableObject
    {
        public string Title { get; }
        public string Message => $"{Title} is coming in a later build phase — this screen is a placeholder for now.";

        public ComingSoonViewModel(string title)
        {
            Title = title;
        }
    }
} 