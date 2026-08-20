using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using BatUtils.Models;

namespace BatUtils.Views
{
    public partial class ClientWindow : Window
    {
        private readonly Func<string, bool> _isCodeDuplicate;

        public Client Client { get; private set; }

        public ClientCredentials Credentials { get; private set; }

        public bool RKConnectionChanged { get; private set; }

        public bool SHConnectionChanged { get; private set; }

        public ClientWindow(Func<string, bool> isCodeDuplicate)
        {
            InitializeComponent();

            _isCodeDuplicate = isCodeDuplicate;

            Title = Properties.Strings.ClientWindowAddTitle;

            Credentials = new ClientCredentials();
        }

        public ClientWindow(
            Client client,
            ClientCredentials credentials,
            Func<string, bool> isCodeDuplicate
        )
            : this(isCodeDuplicate)
        {
            Title = Properties.Strings.ClientWindowEditTitle;

            CodeTextBox.Text = client.Code;

            NameTextBox.Text = client.Name;

            EnabledCheckBox.IsChecked = client.Enabled;

            RKAddressTextBox.Text = client.RKServerAddress;

            RKPortTextBox.Text = client.RKServerPort.ToString();

            RKUseHttpsCheckBox.IsChecked = client.RKUseHttps;

            SHAddressTextBox.Text = client.SHServerAddress;

            SHPortTextBox.Text = client.SHServerPort.ToString();

            SHUseHttpsCheckBox.IsChecked = client.SHUseHttps;

            credentials = credentials ?? new ClientCredentials();

            RKLoginTextBox.Text = credentials.RKUserName;

            RKPasswordBox.Password = credentials.RKPassword ?? string.Empty;

            SHLoginTextBox.Text = credentials.SHUserName;

            SHPasswordBox.Password = credentials.SHPassword ?? string.Empty;

            Client = client;

            Credentials = credentials;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ushort rkPort = 0;
            ushort shPort = 0;

            if (
                string.IsNullOrWhiteSpace(CodeTextBox.Text)
                || string.IsNullOrWhiteSpace(NameTextBox.Text)
            )
            {
                MessageBox.Show(
                    this,
                    Properties.Strings.ClientWindowValidationCodeName,
                    Properties.Strings.ClientWindowValidationTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            string code = CodeTextBox.Text.Trim();

            if (code == "." || code == ".." || code.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show(
                    this,
                    Properties.Strings.ClientWindowInvalidCode,
                    Properties.Strings.ClientWindowValidationTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            if (_isCodeDuplicate != null && _isCodeDuplicate(code))
            {
                MessageBox.Show(
                    this,
                    string.Format(Properties.Strings.ClientsDuplicateCode, code),
                    Properties.Strings.ClientWindowValidationTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                CodeTextBox.Focus();
                CodeTextBox.SelectAll();

                return;
            }

            if (
                !string.IsNullOrWhiteSpace(RKPortTextBox.Text)
                && !ushort.TryParse(RKPortTextBox.Text, out rkPort)
            )
            {
                MessageBox.Show(
                    this,
                    Properties.Strings.ClientWindowInvalidRKPort,
                    Properties.Strings.ClientWindowValidationTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            if (
                !string.IsNullOrWhiteSpace(SHPortTextBox.Text)
                && !ushort.TryParse(SHPortTextBox.Text, out shPort)
            )
            {
                MessageBox.Show(
                    this,
                    Properties.Strings.ClientWindowInvalidSHPort,
                    Properties.Strings.ClientWindowValidationTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            var insecureServices = new List<string>();

            if (ShouldWarnForRkHttp(rkPort))
                insecureServices.Add("RK7");

            if (ShouldWarnForShHttp(shPort))
                insecureServices.Add("SH5");

            if (insecureServices.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show(
                    this,
                    string.Format(
                        Properties.Strings.ClientWindowUnencryptedConnectionWarning,
                        string.Join(", ", insecureServices)
                    ),
                    Properties.Strings.ClientWindowUnencryptedConnectionTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result != MessageBoxResult.Yes)
                    return;
            }

            RKConnectionChanged = HasRkConnectionChanged(rkPort);

            SHConnectionChanged = HasShConnectionChanged(shPort);

            if (Client == null)
                Client = new Client();

            Client.Enabled = EnabledCheckBox.IsChecked == true;

            Client.Code = code;
            Client.Name = NameTextBox.Text.Trim();

            Client.RKServerAddress = RKAddressTextBox.Text.Trim();
            Client.RKServerPort = rkPort;
            Client.RKUseHttps = RKUseHttpsCheckBox.IsChecked == true;

            Client.SHServerAddress = SHAddressTextBox.Text.Trim();
            Client.SHServerPort = shPort;
            Client.SHUseHttps = SHUseHttpsCheckBox.IsChecked == true;

            Credentials = new ClientCredentials
            {
                Code = code,
                RKUserName = RKLoginTextBox.Text.Trim(),
                RKPassword = RKPasswordBox.Password,
                SHUserName = SHLoginTextBox.Text.Trim(),
                SHPassword = SHPasswordBox.Password,
            };

            DialogResult = true;
        }

        private bool ShouldWarnForRkHttp(ushort port)
        {
            bool hasCredentials =
                !string.IsNullOrWhiteSpace(RKLoginTextBox.Text)
                || !string.IsNullOrEmpty(RKPasswordBox.Password);

            if (
                RKUseHttpsCheckBox.IsChecked == true
                || !hasCredentials
                || string.IsNullOrWhiteSpace(RKAddressTextBox.Text)
                || port == 0
            )
            {
                return false;
            }

            return Client == null
                || Client.RKUseHttps
                || Client.RKServerPort != port
                || !string.Equals(
                    Client.RKServerAddress,
                    RKAddressTextBox.Text.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
                || !string.Equals(
                    Credentials?.RKUserName,
                    RKLoginTextBox.Text.Trim(),
                    StringComparison.Ordinal
                )
                || !string.Equals(
                    Credentials?.RKPassword,
                    RKPasswordBox.Password,
                    StringComparison.Ordinal
                );
        }

        private bool HasRkConnectionChanged(ushort port)
        {
            if (Client == null)
                return false;

            return Client.RKServerPort != port
                || Client.RKUseHttps != (RKUseHttpsCheckBox.IsChecked == true)
                || !string.Equals(
                    Client.RKServerAddress,
                    RKAddressTextBox.Text.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
                || !string.Equals(
                    Credentials?.RKUserName,
                    RKLoginTextBox.Text.Trim(),
                    StringComparison.Ordinal
                )
                || !string.Equals(
                    Credentials?.RKPassword,
                    RKPasswordBox.Password,
                    StringComparison.Ordinal
                );
        }

        private bool ShouldWarnForShHttp(ushort port)
        {
            bool hasCredentials =
                !string.IsNullOrWhiteSpace(SHLoginTextBox.Text)
                || !string.IsNullOrEmpty(SHPasswordBox.Password);

            if (
                SHUseHttpsCheckBox.IsChecked == true
                || !hasCredentials
                || string.IsNullOrWhiteSpace(SHAddressTextBox.Text)
                || port == 0
            )
            {
                return false;
            }

            return Client == null
                || Client.SHUseHttps
                || Client.SHServerPort != port
                || !string.Equals(
                    Client.SHServerAddress,
                    SHAddressTextBox.Text.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
                || !string.Equals(
                    Credentials?.SHUserName,
                    SHLoginTextBox.Text.Trim(),
                    StringComparison.Ordinal
                )
                || !string.Equals(
                    Credentials?.SHPassword,
                    SHPasswordBox.Password,
                    StringComparison.Ordinal
                );
        }

        private bool HasShConnectionChanged(ushort port)
        {
            if (Client == null)
                return false;

            return Client.SHServerPort != port
                || Client.SHUseHttps != (SHUseHttpsCheckBox.IsChecked == true)
                || !string.Equals(
                    Client.SHServerAddress,
                    SHAddressTextBox.Text.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
                || !string.Equals(
                    Credentials?.SHUserName,
                    SHLoginTextBox.Text.Trim(),
                    StringComparison.Ordinal
                )
                || !string.Equals(
                    Credentials?.SHPassword,
                    SHPasswordBox.Password,
                    StringComparison.Ordinal
                );
        }
    }
}
