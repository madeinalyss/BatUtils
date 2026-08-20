using System;
using System.Windows;
using BatUtils.Services;

namespace BatUtils.Views
{
    public enum LoginWindowMode
    {
        Create,
        Unlock,
    }

    public partial class LoginWindow : Window
    {
        private readonly LoginWindowMode _mode;

        private readonly Func<string, CredentialUnlockResult> _unlock;

        public string Password { get; private set; }

        public LoginWindow(LoginWindowMode mode, Func<string, CredentialUnlockResult> unlock = null)
        {
            InitializeComponent();

            _mode = mode;
            _unlock = unlock;

            ConfigureMode();
        }

        private void ConfigureMode()
        {
            if (_mode == LoginWindowMode.Create)
            {
                TitleText.Text = Properties.Strings.LoginCreateTitle;

                DescriptionText.Text = Properties.Strings.LoginCreateDescription;

                ConfirmPasswordPanel.Visibility = Visibility.Visible;

                ContinueButton.Content = Properties.Strings.LoginCreateAction;
            }
            else
            {
                TitleText.Text = Properties.Strings.LoginUnlockTitle;

                DescriptionText.Text = Properties.Strings.LoginUnlockDescription;

                ConfirmPasswordPanel.Visibility = Visibility.Collapsed;

                ContinueButton.Content = Properties.Strings.LoginUnlockAction;
            }
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(password))
            {
                ShowError(Properties.Strings.LoginPasswordRequired);

                PasswordBox.Focus();
                return;
            }

            if (_mode == LoginWindowMode.Create)
            {
                if (password != ConfirmPasswordBox.Password)
                {
                    ShowError(Properties.Strings.LoginPasswordsDoNotMatch);

                    ConfirmPasswordBox.Focus();
                    ConfirmPasswordBox.SelectAll();

                    return;
                }
            }

            if (_mode == LoginWindowMode.Unlock)
            {
                CredentialUnlockResult result;

                try
                {
                    result = _unlock?.Invoke(password);
                }
                catch (Exception ex)
                {
                    result = CredentialUnlockResult.Failed(
                        CredentialUnlockFailure.Unavailable,
                        ex.Message
                    );
                }

                if (result == null || !result.Succeeded)
                {
                    ShowUnlockError(result);

                    PasswordBox.SelectAll();
                    PasswordBox.Focus();

                    return;
                }
            }

            Password = password;

            DialogResult = true;
        }

        private void ShowUnlockError(CredentialUnlockResult result)
        {
            if (result == null || result.Failure == CredentialUnlockFailure.Unavailable)
            {
                ShowError(
                    string.Format(
                        Properties.Strings.LoginCredentialStoreUnavailable,
                        result?.Details ?? Properties.Strings.LoginUnknownError
                    )
                );

                return;
            }

            if (result.Failure == CredentialUnlockFailure.InvalidStore)
            {
                ShowError(Properties.Strings.LoginCredentialStoreInvalid);

                return;
            }

            ShowError(Properties.Strings.LoginInvalidPasswordOrCorrupted);
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;

            ErrorText.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorText.Visibility = Visibility.Collapsed;
        }
    }
}
