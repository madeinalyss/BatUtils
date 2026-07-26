using BatUtils.Themes;
using Microsoft.Win32;
using System;
using System.Windows;

namespace BatUtils
{
    public static class ThemeManager
    {
        private static ResourceDictionary _currentTheme;
        private static AppTheme _currentMode = AppTheme.Dark;
        private static bool _isListening;

        public static void SetTheme(AppTheme mode)
        {
            _currentMode = mode;

            bool isDark = mode == AppTheme.System ? IsSystemDarkTheme() : mode == AppTheme.Dark;
            ApplyThemeDictionary(isDark);

            if (mode == AppTheme.System)
                StartListening();
            else
                StopListening();
        }

        private static void ApplyThemeDictionary(bool isDark)
        {
            string themePath = isDark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";
            var newTheme = new ResourceDictionary { Source = new Uri(themePath, UriKind.Relative) };

            if (_currentTheme != null)
                Application.Current.Resources.MergedDictionaries.Remove(_currentTheme);

            Application.Current.Resources.MergedDictionaries.Add(newTheme);
            _currentTheme = newTheme;
        }

        private static void StartListening()
        {
            if (_isListening)
                return;

            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            _isListening = true;
        }

        private static void StopListening()
        {
            if (!_isListening)
                return;

            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            _isListening = false;
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General || _currentMode != AppTheme.System)
                return;

            Application.Current.Dispatcher.Invoke(() => ApplyThemeDictionary(IsSystemDarkTheme()));
        }

        private static bool IsSystemDarkTheme()
        {
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string valueName = "AppsUseLightTheme";

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
                {
                    if (key?.GetValue(valueName) is int value)
                        return value == 0; // 0 = dark, 1 = light
                }
            }
            catch
            {
                // Key missing (older Windows) or inaccessible — fall back to dark.
            }

            return true;
        }
    }
}