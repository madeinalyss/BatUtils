using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BatUtils.Controls
{
    public partial class SidebarItem : UserControl
    {
        public SidebarItem()
        {
            InitializeComponent();

            MouseLeftButtonUp += SidebarItem_MouseLeftButtonUp;
            MouseEnter += SidebarItem_MouseEnter;
            MouseLeave += SidebarItem_MouseLeave;

            UpdateAppearance();
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(SidebarItem),
            new PropertyMetadata(string.Empty)
        );

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected),
            typeof(bool),
            typeof(SidebarItem),
            new PropertyMetadata(false, OnIsSelectedChanged)
        );

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public static readonly DependencyProperty PageProperty = DependencyProperty.Register(
            nameof(Page),
            typeof(NavigationPage),
            typeof(SidebarItem),
            new PropertyMetadata(NavigationPage.Clients)
        );

        public NavigationPage Page
        {
            get => (NavigationPage)GetValue(PageProperty);
            set => SetValue(PageProperty, value);
        }

        public event RoutedEventHandler Click;

        private static void OnIsSelectedChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            ((SidebarItem)d).UpdateAppearance();
        }

        private void SidebarItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Click?.Invoke(this, new RoutedEventArgs());
            e.Handled = true;
        }

        private void SidebarItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!IsSelected)
                BackgroundBorder.SetResourceReference(
                    Border.BackgroundProperty,
                    "SidebarHoverBackgroundBrush"
                );
        }

        private void SidebarItem_MouseLeave(object sender, MouseEventArgs e)
        {
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            if (IsSelected)
            {
                SelectionBar.Visibility = Visibility.Visible;
                BackgroundBorder.SetResourceReference(
                    Border.BackgroundProperty,
                    "SidebarSelectedBackgroundBrush"
                );
                TitleText.SetResourceReference(
                    TextBlock.ForegroundProperty,
                    "SidebarSelectedForegroundBrush"
                );
                TitleText.FontWeight = FontWeights.SemiBold;
            }
            else
            {
                SelectionBar.Visibility = Visibility.Collapsed;
                BackgroundBorder.Background = Brushes.Transparent;
                TitleText.SetResourceReference(
                    TextBlock.ForegroundProperty,
                    "MutedForegroundBrush"
                );
                TitleText.FontWeight = FontWeights.Normal;
            }
        }
    }
}
