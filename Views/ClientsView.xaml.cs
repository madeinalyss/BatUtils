using BatUtils.Models;
using BatUtils.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace BatUtils.Views
{
    public class SoftwareInstalledConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            string clientCode = value as string;
            string software = parameter as string;

            if (string.IsNullOrWhiteSpace(clientCode) ||
                string.IsNullOrWhiteSpace(software))
            {
                return "×";
            }

            string rootFolder =
                Properties.Settings.Default.RootFolder;

            if (string.IsNullOrWhiteSpace(rootFolder))
                return "×";

            if (string.Equals(
                    software,
                    "RK",
                    StringComparison.OrdinalIgnoreCase))
            {
                return UcsManager.IsRk7Installed(
                    rootFolder,
                    clientCode)
                    ? "✓"
                    : "×";
            }

            if (string.Equals(
                    software,
                    "SH",
                    StringComparison.OrdinalIgnoreCase))
            {
                return UcsManager.IsSh5Installed(
                    rootFolder,
                    clientCode)
                    ? "✓"
                    : "×";
            }

            return "×";
        }


        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


    public partial class ClientsView : UserControl
    {
        public ObservableCollection<Client> Clients { get; }
            = new ObservableCollection<Client>();

        private ICollectionView _clientsView;

        private bool _showInactiveClients;

        private string _filterText = string.Empty;
        private DispatcherTimer _filterTimer;

        public ClientsView()
        {
            InitializeComponent();

            _clientsView =
                CollectionViewSource.GetDefaultView(Clients);

            _clientsView.Filter = ClientFilter;

            ClientsGrid.ItemsSource = _clientsView;

            // debounce timer for search
            _filterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _filterTimer.Tick += FilterTimer_Tick;

            ReloadClients();
        }


        // =========================================================
        // CLIENTS
        // =========================================================

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

            // hide inactive unless requested
            if (!_showInactiveClients && !client.Enabled)
                return false;

            if (string.IsNullOrWhiteSpace(_filterText))
                return true;

            string q = _filterText.Trim();

            return (client.Code != null && client.Code.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                || (client.Name != null && client.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
        }


        private Client SelectedClient
        {
            get
            {
                return ClientsGrid.SelectedItem as Client;
            }
        }

        private void FilterTimer_Tick(object sender, EventArgs e)
        {
            _filterTimer.Stop();
            _clientsView.Refresh();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _filterTimer.Stop();
            _filterText = SearchTextBox.Text ?? string.Empty;
            _filterTimer.Start();
        }

        private void ShowInactiveCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _showInactiveClients = true;
            _clientsView.Refresh();
        }

        private void ShowInactiveCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _showInactiveClients = false;
            _clientsView.Refresh();
        }


        // =========================================================
        // CLIENT MANAGEMENT
        // =========================================================

        private void ClientsGrid_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            EditClient();
        }

        private void AddButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ClientWindow window = new ClientWindow();

            window.Owner = Window.GetWindow(this);

            if (window.ShowDialog() != true)
                return;

            Clients.Add(window.Client);

            SaveClients();

            _clientsView.Refresh();
        }

        private void EditClient()
        {
            Client client = SelectedClient;

            if (client == null)
                return;

            ClientWindow window =
                new ClientWindow(client);

            window.Owner = Window.GetWindow(this);

            if (window.ShowDialog() != true)
                return;

            SaveClients();

            _clientsView.Refresh();

            if (!_showInactiveClients &&
                !client.Enabled)
            {
                ClientsGrid.SelectedItem = null;
            }
        }

        private void EditButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            EditClient();
        }

        private void DeleteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Client client = SelectedClient;

            if (client == null)
                return;

            if (MessageBox.Show(
                    string.Format(
                        Properties.Strings.ClientsDeleteQuestion,
                        client.Name),
                    Properties.Strings.ClientsDeleteTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question)
                != MessageBoxResult.Yes)
            {
                return;
            }

            Clients.Remove(client);

            SaveClients();

            _clientsView.Refresh();
        }


        // =========================================================
        // CHECK / MORE
        // =========================================================

        private void CheckButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (SelectedClient == null)
                return;

            // TODO:
            // Check RK7 and SH5 server availability.
        }


        private void MoreButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MoreContextMenu.PlacementTarget = MoreButton;
            MoreContextMenu.Placement = PlacementMode.Custom;

            MoreContextMenu.CustomPopupPlacementCallback =
                (popupSize, targetSize, offset) =>
                {
                    return new[]
                    {
                        new CustomPopupPlacement
                        {
                            Point = new Point(
                                targetSize.Width - popupSize.Width,
                                targetSize.Height + 3),

                            PrimaryAxis =
                                PopupPrimaryAxis.Vertical
                        }
                    };
                };

            MoreContextMenu.IsOpen = true;
        }


        private void RefreshButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ReloadClients();
        }


        private void ShowInactiveMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            _showInactiveClients =
                ShowInactiveMenuItem.IsChecked;

            _clientsView.Refresh();

            /*
             * If the currently selected client became hidden,
             * clear the selection.
             */
            if (!_showInactiveClients &&
                SelectedClient != null &&
                !SelectedClient.Enabled)
            {
                ClientsGrid.SelectedItem = null;
            }
        }


        // =========================================================
        // RK7
        // =========================================================

        private void OpenRKButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (SelectedClient == null)
                return;

            string rootFolder =
                Properties.Settings.Default.RootFolder;

            string executablePath =
                UcsManager.GetRk7Executable(
                    rootFolder,
                    SelectedClient.Code);

            if (!File.Exists(executablePath))
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    string.Format(Properties.Strings.ClientsRK7NotFoundMessage, executablePath),
                    Properties.Strings.ClientsExecutableNotFoundTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    string.Format(Properties.Strings.ClientsLaunchErrorMessage, Properties.Strings.ClientRK7Name, ex.Message),
                    Properties.Strings.ClientsLaunchErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

        }


        // =========================================================
        // SH5
        // =========================================================

        private void OpenSHButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (SelectedClient == null)
                return;

            string rootFolder =
                Properties.Settings.Default.RootFolder;

            string executablePath =
                UcsManager.GetSh5Executable(
                    rootFolder,
                    SelectedClient.Code);

            if (!File.Exists(executablePath))
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    string.Format(Properties.Strings.ClientsSH5NotFoundMessage, executablePath),
                    Properties.Strings.ClientsExecutableNotFoundTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    string.Format(Properties.Strings.ClientsLaunchErrorMessage, Properties.Strings.ClientSH5Name, ex.Message),
                    Properties.Strings.ClientsLaunchErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

        }


        // =========================================================
        // EXPLORE
        // =========================================================

        private void ExploreButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (SelectedClient == null)
                return;

            string rootFolder =
                Properties.Settings.Default.RootFolder;

            string clientFolder =
                UcsManager.GetClientFolder(
                    rootFolder,
                    SelectedClient.Code);

            if (!Directory.Exists(clientFolder))
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    string.Format(Properties.Strings.ClientsFolderNotFoundMessage, clientFolder),
                    Properties.Strings.ClientsFolderNotFoundTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = clientFolder,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    string.Format(Properties.Strings.ClientsOpenFolderErrorMessage, ex.Message),
                    Properties.Strings.ClientsOpenFolderErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

        }
    }
}