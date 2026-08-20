using System.Windows;
using System.Windows.Controls;
using BatUtils.Services;
using BatUtils.Themes;

namespace BatUtils.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();

            UpdateThemeSelection();
            UpdateLanguageSelection();

            RootFolderTextBox.Text = SettingsStorage.Current.RootFolder;
        }

        private void UpdateThemeSelection()
        {
            switch (SettingsStorage.Current.Theme)
            {
                case "Light":
                    LightThemeRadio.IsChecked = true;
                    break;

                case "System":
                    SystemThemeRadio.IsChecked = true;
                    break;

                default:
                    DarkThemeRadio.IsChecked = true;
                    break;
            }
        }

        private void UpdateLanguageSelection()
        {
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if ((string)item.Tag == SettingsStorage.Current.Language)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void RootFolderTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SettingsStorage.Current.RootFolder = RootFolderTextBox.Text;

            SettingsStorage.Save();
        }

        private void BrowseRootFolder_Click(object sender, RoutedEventArgs e)
        {
            if (FolderPicker.TryPick(
                Window.GetWindow(this),
                Properties.Strings.SettingsRootFolder,
                RootFolderTextBox.Text,
                out string path))
            {
                RootFolderTextBox.Text = path;
            }
        }

        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            AppTheme theme = AppTheme.Dark;

            if (LightThemeRadio.IsChecked == true)
                theme = AppTheme.Light;
            else if (SystemThemeRadio.IsChecked == true)
                theme = AppTheme.System;

            ThemeManager.SetTheme(theme);

            SettingsStorage.Current.Theme = theme.ToString();

            SettingsStorage.Save();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(LanguageComboBox.SelectedItem is ComboBoxItem selected))
            {
                return;
            }

            SettingsStorage.Current.Language = selected.Tag.ToString();

            SettingsStorage.Save();
        }
    }
}
