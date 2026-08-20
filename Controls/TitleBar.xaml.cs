using System;
using System.Windows;
using System.Windows.Controls;

namespace BatUtils.Controls
{
    public partial class TitleBar : UserControl
    {
        public event EventHandler MinimizeClicked;
        public event EventHandler MaximizeClicked;
        public event EventHandler CloseClicked;

        public TitleBar()
        {
            InitializeComponent();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            MinimizeClicked?.Invoke(this, EventArgs.Empty);
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            MaximizeClicked?.Invoke(this, EventArgs.Empty);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseClicked?.Invoke(this, EventArgs.Empty);
        }

        public void SetMaximized(bool maximized)
        {
            MaximizeIcon.Visibility = maximized ? Visibility.Collapsed : Visibility.Visible;
            RestoreIcon.Visibility = maximized ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
