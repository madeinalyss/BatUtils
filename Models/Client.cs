using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BatUtils.Models
{
    public class Client : INotifyPropertyChanged
    {
        private bool _enabled = true;
        private string _code;
        private string _name;
        private string _rkServerAddress;
        private ushort _rkServerPort;
        private bool _rkUseHttps;
        private string _shServerAddress;
        private ushort _shServerPort;
        private bool _shUseHttps;
        private string _rkStatus = "—";
        private string _shStatus = "—";
        private string _rkStatusDetails;
        private string _shStatusDetails;

        public bool Enabled
        {
            get => _enabled;
            set => SetField(ref _enabled, value);
        }

        public string Code
        {
            get => _code;
            set => SetField(ref _code, value);
        }

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public string RKServerAddress
        {
            get => _rkServerAddress;
            set
            {
                if (SetField(ref _rkServerAddress, value))
                    OnPropertyChanged(nameof(RKEndpoint));
            }
        }

        public ushort RKServerPort
        {
            get => _rkServerPort;
            set
            {
                if (SetField(ref _rkServerPort, value))
                    OnPropertyChanged(nameof(RKEndpoint));
            }
        }

        public bool RKUseHttps
        {
            get => _rkUseHttps;
            set => SetField(ref _rkUseHttps, value);
        }

        public string SHServerAddress
        {
            get => _shServerAddress;
            set
            {
                if (SetField(ref _shServerAddress, value))
                    OnPropertyChanged(nameof(SHEndpoint));
            }
        }

        public ushort SHServerPort
        {
            get => _shServerPort;
            set
            {
                if (SetField(ref _shServerPort, value))
                    OnPropertyChanged(nameof(SHEndpoint));
            }
        }

        public bool SHUseHttps
        {
            get => _shUseHttps;
            set => SetField(ref _shUseHttps, value);
        }

        public string RKStatus
        {
            get => _rkStatus;
            set => SetField(ref _rkStatus, value);
        }

        public string SHStatus
        {
            get => _shStatus;
            set => SetField(ref _shStatus, value);
        }

        public string RKStatusDetails
        {
            get => _rkStatusDetails;
            set => SetField(ref _rkStatusDetails, value);
        }

        public string SHStatusDetails
        {
            get => _shStatusDetails;
            set => SetField(ref _shStatusDetails, value);
        }

        // Return null for a missing address so XAML displays a dash instead of ":0".
        public string RKEndpoint =>
            string.IsNullOrWhiteSpace(_rkServerAddress)
                ? null
                : $"{_rkServerAddress}:{_rkServerPort}";

        public string SHEndpoint =>
            string.IsNullOrWhiteSpace(_shServerAddress)
                ? null
                : $"{_shServerAddress}:{_shServerPort}";

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Avoid raising change notifications for no-op assignments.
        protected bool SetField<T>(
            ref T field,
            T value,
            [CallerMemberName] string propertyName = null
        )
        {
            if (Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
