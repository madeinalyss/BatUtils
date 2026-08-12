using BatUtils.Themes;
using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;

namespace BatUtils.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();

            UpdateThemeSelection();
            UpdateLanguageSelection();

            RootFolderTextBox.Text = Properties.Settings.Default.RootFolder;
        }

        private void UpdateThemeSelection()
        {
            switch (Properties.Settings.Default.Theme)
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
                if ((string)item.Tag == Properties.Settings.Default.Language)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void RootFolderTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.RootFolder = RootFolderTextBox.Text;
            Properties.Settings.Default.Save();
        }

        private void BrowseRootFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = Properties.Strings.SettingsRootFolder,
                SelectedPath = RootFolderTextBox.Text,
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true)
            {
                RootFolderTextBox.Text = dialog.SelectedPath;
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

            Properties.Settings.Default.Theme = theme.ToString();
            Properties.Settings.Default.Save();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(LanguageComboBox.SelectedItem is ComboBoxItem selected))
                return;

            Properties.Settings.Default.Language = selected.Tag.ToString();
            Properties.Settings.Default.Save();
        }
    }
}