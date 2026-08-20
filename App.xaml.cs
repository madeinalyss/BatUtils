using System;
using System.Windows;
using BatUtils.Services;
using BatUtils.Themes;
using BatUtils.Views;

namespace BatUtils
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Keep WPF alive while the login window closes before MainWindow exists.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            LocalizationManager.SetLanguage(SettingsStorage.Current.Language);

            if (!Enum.TryParse(SettingsStorage.Current.Theme, out AppTheme theme))
            {
                theme = AppTheme.Dark;
            }

            ThemeManager.SetTheme(theme);

            bool loggedIn;

            if (CredentialStorage.Exists)
            {
                loggedIn = ShowUnlockWindow();
            }
            else
            {
                loggedIn = ShowCreateWindow();
            }

            if (!loggedIn)
            {
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();

            MainWindow = mainWindow;

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            mainWindow.Show();
        }

        private static bool ShowUnlockWindow()
        {
            var window = new LoginWindow(
                LoginWindowMode.Unlock,
                password => CredentialStorage.Unlock(password)
            );

            return window.ShowDialog() == true;
        }

        private static bool ShowCreateWindow()
        {
            var window = new LoginWindow(LoginWindowMode.Create);

            if (window.ShowDialog() != true)
                return false;

            try
            {
                CredentialStorage.Create(window.Password);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(
                        BatUtils.Properties.Strings.LoginCredentialStoreCreateError,
                        ex.Message
                    ),
                    BatUtils.Properties.Strings.LoginCredentialStoreCreateErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                return false;
            }

            return true;
        }
    }
}
