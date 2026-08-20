using System;
using System.Windows;
using System.Windows.Controls;

namespace BatUtils.Controls
{
    public partial class Sidebar : UserControl
    {
        public event EventHandler<NavigationPage> NavigationRequested;

        private readonly SidebarItem[] _items;

        public Sidebar()
        {
            InitializeComponent();

            _items = new[] { ClientsItem, ToolsItem, SettingsItem, InfoItem };

            foreach (SidebarItem item in _items)
                item.Click += SidebarItem_Click;
        }

        private void SidebarItem_Click(object sender, RoutedEventArgs e)
        {
            SidebarItem selected = (SidebarItem)sender;

            foreach (SidebarItem item in _items)
                item.IsSelected = item == selected;

            NavigationRequested?.Invoke(this, selected.Page);
        }
    }
}
