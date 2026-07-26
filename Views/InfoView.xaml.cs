using System;
using System.Reflection;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BatUtils.Views
{
    public partial class InfoView : UserControl
    {
        public InfoView()
        {
            InitializeComponent();
            PopulateSystemInfo();
        }

        private void PopulateSystemInfo()
        {
            OsValueText.Text = GetOsDisplayName();
            ArchValueText.Text = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            DotNetValueText.Text = GetDotNetVersion();
            AppVersionValueText.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private static string GetOsDisplayName()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    string productName = key?.GetValue("ProductName") as string;
                    string displayVersion = key?.GetValue("DisplayVersion") as string
                        ?? key?.GetValue("ReleaseId") as string;

                    if (!string.IsNullOrEmpty(productName))
                    {
                        return string.IsNullOrEmpty(displayVersion)
                            ? productName
                            : $"{productName} ({displayVersion})";
                    }
                }
            }
            catch
            {
                // Registry unavailable — fall back below.
            }

            return Environment.OSVersion.VersionString;
        }

        private static string GetDotNetVersion()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                {
                    if (key?.GetValue("Release") is int release)
                        return $".NET Framework {ReleaseToVersion(release)}";
                }
            }
            catch
            {
                // Fall back below.
            }

            return Environment.Version.ToString();
        }

        private static string ReleaseToVersion(int release)
        {
            if (release >= 528040) return "4.8";
            if (release >= 461808) return "4.7.2";
            if (release >= 461308) return "4.7.1";
            if (release >= 460798) return "4.7";
            return release.ToString();
        }
    }
}