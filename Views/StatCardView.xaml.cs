using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Finvora.Views
{
    public partial class StatCardView : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(StatCardView));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(object), typeof(StatCardView));

        public static readonly DependencyProperty BadgeProperty =
            DependencyProperty.Register(nameof(Badge), typeof(string), typeof(StatCardView));

        public static readonly DependencyProperty AccentBrushProperty =
            DependencyProperty.Register(nameof(AccentBrush), typeof(Brush), typeof(StatCardView),
                new PropertyMetadata(Brushes.SteelBlue));

        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
        public object Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
        public string Badge { get => (string)GetValue(BadgeProperty); set => SetValue(BadgeProperty, value); }
        public Brush AccentBrush { get => (Brush)GetValue(AccentBrushProperty); set => SetValue(AccentBrushProperty, value); }

        public StatCardView()
        {
            InitializeComponent();
        }
    }
}  