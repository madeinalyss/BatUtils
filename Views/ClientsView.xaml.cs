using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using BatUtils.Models;
using BatUtils.Services;

namespace BatUtils.Views
{
    public class SoftwareInstalledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string clientCode = value as string;
            string software = parameter as string;

            if (string.IsNullOrWhiteSpace(clientCode) || string.IsNullOrWhiteSpace(software))
            {
                return "×";
            }

            string rootFolder = SettingsStorage.Current.RootFolder;

            if (string.IsNullOrWhiteSpace(rootFolder))
                return "×";

            if (string.Equals(software, "RK", StringComparison.OrdinalIgnoreCase))
            {
                return UcsManager.IsRk7Installed(rootFolder, clientCode) ? "✓" : "×";
            }

            if (string.Equals(software, "SH", StringComparison.OrdinalIgnoreCase))
            {
                return UcsManager.IsSh5Installed(rootFolder, clientCode) ? "✓" : "×";
            }

            return "×";
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            throw new NotSupportedException();
        }
    }

    public partial class ClientsView : UserControl
    {
        public ObservableCollection<Client> Clients { get; } = new ObservableCollection<Client>();

        private readonly ICollectionView _clientsView;

        private bool _showInactiveClients;
        private string _filterText = string.Empty;

        public ClientsView()
        {
            InitializeComponent();

            _clientsView = CollectionViewSource.GetDefaultView(Clients);

            _clientsView.Filter = ClientFilter;

            ClientsGrid.ItemsSource = _clientsView;

            ReloadClients();
        }

        private void ReloadClients()
        {
            Clients.Clear();

            foreach (Client client in ClientStorage.Load())
            {
                Clients.Add(client);
            }

            _clientsView.Refresh();
        }

        private void SaveClients()
        {
            ClientStorage.Save(Clients);
        }

        private bool ClientFilter(object obj)
        {
            Client client = obj as Client;

            if (client == null)
                return false;

            if (!_showInactiveClients && !client.Enabled)
                return false;

            if (string.IsNullOrWhiteSpace(_filterText))
                return true;

            string query = _filterText.Trim();

            return (
                    !string.IsNullOrEmpty(client.Code)
                    && client.Code.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                )
                || (
                    !string.IsNullOrEmpty(client.Name)
                    && client.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                );
        }

        private Client SelectedClient
        {
            get { return ClientsGrid.SelectedItem as Client; }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _filterText = SearchTextBox.Text ?? string.Empty;

            _clientsView.Refresh();
        }

        private void ClientsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditClient();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ClientWindow window = new ClientWindow(code => IsClientCodeDuplicate(code))
            {
                Owner = Window.GetWindow(this),
            };

            if (window.ShowDialog() != true)
                return;

            bool credentialsSaved = false;
            bool clientAdded = false;

            try
            {
                CredentialStorage.SetClient(null, window.Credentials);

                credentialsSaved = true;

                Clients.Add(window.Client);
                clientAdded = true;

                SaveClients();
            }
            catch (Exception ex)
            {
                if (clientAdded)
                    Clients.Remove(window.Client);

                Exception rollbackError = null;

                if (credentialsSaved)
                {
                    try
                    {
                        CredentialStorage.RemoveClient(window.Client.Code);
                    }
                    catch (Exception rollbackException)
                    {
                        rollbackError = rollbackException;
                    }
                }

                ShowPersistenceError(ex, rollbackError);

                return;
            }

            _clientsView.Refresh();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditClient();
        }

        private void EditClient()
        {
            Client client = SelectedClient;

            if (client == null)
                return;

            string originalCode = client.Code;

            Client originalClient = CloneClient(client);

            bool hadOriginalCredentials = CredentialStorage.TryGetClient(
                originalCode,
                out ClientCredentials originalCredentials
            );

            ClientCredentials displayedCredentials = hadOriginalCredentials
                ? originalCredentials
                : new ClientCredentials { Code = originalCode };

            ClientWindow window = new ClientWindow(
                client,
                displayedCredentials,
                code => IsClientCodeDuplicate(code, client)
            )
            {
                Owner = Window.GetWindow(this),
            };

            if (window.ShowDialog() != true)
                return;

            bool credentialsSaved = false;

            try
            {
                CredentialStorage.SetClient(originalCode, window.Credentials);

                credentialsSaved = true;

                if (window.RKConnectionChanged)
                {
                    client.RKStatus = "—";
                    client.RKStatusDetails = null;
                }

                if (window.SHConnectionChanged)
                {
                    client.SHStatus = "—";
                    client.SHStatusDetails = null;
                }

                SaveClients();
            }
            catch (Exception ex)
            {
                RestoreClient(originalClient, client);

                Exception rollbackError = null;

                if (credentialsSaved)
                {
                    try
                    {
                        if (hadOriginalCredentials)
                        {
                            CredentialStorage.SetClient(
                                window.Credentials.Code,
                                originalCredentials
                            );
                        }
                        else
                        {
                            CredentialStorage.RemoveClient(window.Credentials.Code);
                        }
                    }
                    catch (Exception rollbackException)
                    {
                        rollbackError = rollbackException;
                    }
                }

                ShowPersistenceError(ex, rollbackError);

                _clientsView.Refresh();
                return;
            }

            _clientsView.Refresh();

            if (!_showInactiveClients && !client.Enabled)
            {
                ClientsGrid.SelectedItem = null;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Client client = SelectedClient;

            if (client == null)
                return;

            MessageBoxResult result = MessageBox.Show(
                Window.GetWindow(this),
                string.Format(Properties.Strings.ClientsDeleteQuestion, client.Name),
                Properties.Strings.ClientsDeleteTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes)
                return;

            int clientIndex = Clients.IndexOf(client);

            bool hadCredentials = CredentialStorage.TryGetClient(
                client.Code,
                out ClientCredentials credentials
            );

            bool credentialsRemoved = false;
            bool clientRemoved = false;

            try
            {
                if (hadCredentials)
                {
                    CredentialStorage.RemoveClient(client.Code);

                    credentialsRemoved = true;
                }

                Clients.Remove(client);
                clientRemoved = true;

                SaveClients();
            }
            catch (Exception ex)
            {
                if (clientRemoved)
                    Clients.Insert(clientIndex, client);

                Exception rollbackError = null;

                if (credentialsRemoved)
                {
                    try
                    {
                        CredentialStorage.SetClient(null, credentials);
                    }
                    catch (Exception rollbackException)
                    {
                        rollbackError = rollbackException;
                    }
                }

                ShowPersistenceError(ex, rollbackError);

                _clientsView.Refresh();
                return;
            }

            _clientsView.Refresh();
        }

        private static Client CloneClient(Client client)
        {
            return new Client
            {
                Enabled = client.Enabled,
                Code = client.Code,
                Name = client.Name,
                RKServerAddress = client.RKServerAddress,
                RKServerPort = client.RKServerPort,
                RKUseHttps = client.RKUseHttps,
                SHServerAddress = client.SHServerAddress,
                SHServerPort = client.SHServerPort,
                SHUseHttps = client.SHUseHttps,
                RKStatus = client.RKStatus,
                SHStatus = client.SHStatus,
                RKStatusDetails = client.RKStatusDetails,
                SHStatusDetails = client.SHStatusDetails,
            };
        }

        private static void RestoreClient(Client source, Client target)
        {
            target.Enabled = source.Enabled;
            target.Code = source.Code;
            target.Name = source.Name;
            target.RKServerAddress = source.RKServerAddress;
            target.RKServerPort = source.RKServerPort;
            target.RKUseHttps = source.RKUseHttps;
            target.SHServerAddress = source.SHServerAddress;
            target.SHServerPort = source.SHServerPort;
            target.SHUseHttps = source.SHUseHttps;
            target.RKStatus = source.RKStatus;
            target.SHStatus = source.SHStatus;
            target.RKStatusDetails = source.RKStatusDetails;
            target.SHStatusDetails = source.SHStatusDetails;
        }

        private void ShowPersistenceError(Exception saveError, Exception rollbackError)
        {
            string message =
                rollbackError == null
                    ? string.Format(Properties.Strings.ClientsPersistenceError, saveError.Message)
                    : string.Format(
                        Properties.Strings.ClientsPersistenceRollbackError,
                        saveError.Message,
                        rollbackError.Message
                    );

            MessageBox.Show(
                Window.GetWindow(this),
                message,
                Properties.Strings.ClientsPersistenceErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        private bool IsClientCodeDuplicate(string code, Client exceptClient = null)
        {
            foreach (Client client in Clients)
            {
                if (ReferenceEquals(client, exceptClient))
                    continue;

                if (string.Equals(client.Code, code, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private async void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            Client client = SelectedClient;

            if (client == null)
                return;

            ClientCredentials credentials = CredentialStorage.GetClient(client.Code);

            client.RKStatus = Properties.Strings.ClientStatusChecking;

            client.SHStatus = Properties.Strings.ClientStatusChecking;
            client.RKStatusDetails = null;
            client.SHStatusDetails = null;

            CheckButton.IsEnabled = false;

            try
            {
                Task<ServerStatusResult> rkCheck = ServerStatusChecker.CheckRk7Async(
                    client,
                    credentials
                );

                Task<ServerStatusResult> shCheck = ServerStatusChecker.CheckSh5Async(
                    client,
                    credentials
                );

                await Task.WhenAll(rkCheck, shCheck);

                client.RKStatus = rkCheck.Result.Status;
                client.RKStatusDetails = rkCheck.Result.Details;

                client.SHStatus = shCheck.Result.Status;
                client.SHStatusDetails = shCheck.Result.Details;
            }
            finally
            {
                CheckButton.IsEnabled = true;
            }
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            MoreContextMenu.PlacementTarget = MoreButton;
            MoreContextMenu.Placement = PlacementMode.Custom;

            MoreContextMenu.CustomPopupPlacementCallback = (popupSize, targetSize, offset) =>
            {
                return new[]
                {
                    new CustomPopupPlacement
                    {
                        Point = new Point(
                            targetSize.Width - popupSize.Width,
                            targetSize.Height + 3
                        ),

                        PrimaryAxis = PopupPrimaryAxis.Vertical,
                    },
                };
            };

            MoreContextMenu.IsOpen = true;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ReloadClients();
        }

        private void ShowInactiveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Client selectedClient = SelectedClient;

            _showInactiveClients = ShowInactiveMenuItem.IsChecked;

            _clientsView.Refresh();

            if (!_showInactiveClients && selectedClient != null && !selectedClient.Enabled)
            {
                ClientsGrid.SelectedItem = null;
            }
        }

        private void OpenRKButton_Click(object sender, RoutedEventArgs e)
        {
            Client client = SelectedClient;

            if (client == null)
                return;

            string executablePath = UcsManager.GetRk7Executable(
                SettingsStorage.Current.RootFolder,
                client.Code
            );

            LaunchExecutable(
                executablePath,
                Properties.Strings.ClientRK7Name,
                Properties.Strings.ClientsRK7NotFoundMessage
            );
        }

        private void OpenSHButton_Click(object sender, RoutedEventArgs e)
        {
            Client client = SelectedClient;

            if (client == null)
                return;

            string executablePath = UcsManager.GetSh5Executable(
                SettingsStorage.Current.RootFolder,
                client.Code
            );

            LaunchExecutable(
                executablePath,
                Properties.Strings.ClientSH5Name,
                Properties.Strings.ClientsSH5NotFoundMessage
            );
        }

        private void LaunchExecutable(
            string executablePath,
            string applicationName,
            string notFoundMessage
        )
        {
            if (!File.Exists(executablePath))
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    string.Format(notFoundMessage, executablePath),
                    Properties.Strings.ClientsExecutableNotFoundTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            try
            {
                Process.Start(
                    new ProcessStartInfo { FileName = executablePath, UseShellExecute = true }
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    string.Format(
                        Properties.Strings.ClientsLaunchErrorMessage,
                        applicationName,
                        ex.Message
                    ),
                    Properties.Strings.ClientsLaunchErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void ExploreButton_Click(object sender, RoutedEventArgs e)
        {
            Client client = SelectedClient;

            if (client == null)
                return;

            string clientFolder = UcsManager.GetClientFolder(
                SettingsStorage.Current.RootFolder,
                client.Code
            );

            if (!Directory.Exists(clientFolder))
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    string.Format(Properties.Strings.ClientsFolderNotFoundMessage, clientFolder),
                    Properties.Strings.ClientsFolderNotFoundTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            try
            {
                Process.Start(
                    new ProcessStartInfo { FileName = clientFolder, UseShellExecute = true }
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    string.Format(Properties.Strings.ClientsOpenFolderErrorMessage, ex.Message),
                    Properties.Strings.ClientsOpenFolderErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}
