using BatUtils.Services;
using BatUtils.Themes;
using System;
using System.Windows;

namespace BatUtils
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LocalizationManager.SetLanguage(
                global::BatUtils.Properties.Settings.Default.Language);

            if (!Enum.TryParse(global::BatUtils.Properties.Settings.Default.Theme, out AppTheme theme))
                theme = AppTheme.Dark;

            ThemeManager.SetTheme(theme);
        }
    }
}