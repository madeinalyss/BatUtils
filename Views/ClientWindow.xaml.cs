using System;
using System.Windows;
using BatUtils.Models;

namespace BatUtils.Views
{
    public partial class ClientWindow : Window
    {
        public Client Client { get; private set; }

        public ClientWindow()
        {
            InitializeComponent();

            Title = Properties.Strings.ClientWindowAddTitle;
        }

        public ClientWindow(Client client)
            : this()
        {
            Title = Properties.Strings.ClientWindowEditTitle;

            CodeTextBox.Text = client.Code;
            NameTextBox.Text = client.Name;

            EnabledCheckBox.IsChecked = client.Enabled;

            RKAddressTextBox.Text = client.RKServerAddress;
            RKPortTextBox.Text = client.RKServerPort.ToString();

            SHAddressTextBox.Text = client.SHServerAddress;
            SHPortTextBox.Text = client.SHServerPort.ToString();

            Client = client;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ushort rkPort = 0;
            ushort shPort = 0;

            if (string.IsNullOrWhiteSpace(CodeTextBox.Text) ||
                string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show(this, Properties.Strings.ClientWindowValidationCodeName, Properties.Strings.ClientWindowValidationTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(RKPortTextBox.Text) &&
                !ushort.TryParse(RKPortTextBox.Text, out rkPort))
            {
                MessageBox.Show(this, Properties.Strings.ClientWindowInvalidRKPort, Properties.Strings.ClientWindowValidationTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(SHPortTextBox.Text) &&
                !ushort.TryParse(SHPortTextBox.Text, out shPort))
            {
                MessageBox.Show(this, Properties.Strings.ClientWindowInvalidSHPort, Properties.Strings.ClientWindowValidationTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Client == null)
                Client = new Client();

            Client.Enabled = EnabledCheckBox.IsChecked == true;

            Client.Code = CodeTextBox.Text.Trim();
            Client.Name = NameTextBox.Text.Trim();

            Client.RKServerAddress = RKAddressTextBox.Text.Trim();
            Client.RKServerPort = rkPort;

            Client.SHServerAddress = SHAddressTextBox.Text.Trim();
            Client.SHServerPort = shPort;

            DialogResult = true;
        }
    }
}