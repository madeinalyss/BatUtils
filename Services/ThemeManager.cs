using System;
using System.Windows;
using BatUtils.Themes;
using Microsoft.Win32;

namespace BatUtils.Services
{
    public static class ThemeManager
    {
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

            var dictionaries = Application.Current.Resources.MergedDictionaries;

            for (int i = dictionaries.Count - 1; i >= 0; i--)
            {
                string source = dictionaries[i].Source?.OriginalString ?? string.Empty;

                if (
                    source.IndexOf("DarkTheme.xaml", StringComparison.OrdinalIgnoreCase) >= 0
                    || source.IndexOf("LightTheme.xaml", StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    dictionaries.RemoveAt(i);
                }
            }

            dictionaries.Add(
                new ResourceDictionary { Source = new Uri(themePath, UriKind.Relative) }
            );
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
            if (_currentMode != AppTheme.System)
                return;

            if (e.Category != UserPreferenceCategory.General)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() => ApplyThemeDictionary(IsSystemDarkTheme()));
        }

        private static bool IsSystemDarkTheme()
        {
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

            const string valueName = "AppsUseLightTheme";

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath))
                {
                    if (key?.GetValue(valueName) is int value)
                    {
                        return value == 0;
                    }
                }
            }
            catch
            {
                // Older Windows or inaccessible registry.
            }

            return true;
        }
    }
}
