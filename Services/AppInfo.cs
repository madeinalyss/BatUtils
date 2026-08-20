using System;
using System.Reflection;
using Microsoft.Win32;

namespace BatUtils.Services
{
    public static class AppInfo
    {
        public static string Version
        {
            get
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                AssemblyInformationalVersionAttribute attribute =
                    (AssemblyInformationalVersionAttribute)
                        assembly.GetCustomAttributes(
                            typeof(AssemblyInformationalVersionAttribute),
                            false
                        )[0];

                return attribute.InformationalVersion;
            }
        }

        public static string OperatingSystem => GetOsDisplayName();

        public static string Architecture => Environment.Is64BitOperatingSystem ? "x64" : "x86";

        public static string RuntimeVersion => GetDotNetVersion();

        private static string GetOsDisplayName()
        {
            try
            {
                using (
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion"
                    )
                )
                {
                    string productName = key?.GetValue("ProductName") as string;

                    string displayVersion =
                        key?.GetValue("DisplayVersion") as string
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
                // Fall back below.
            }

            return Environment.OSVersion.VersionString;
        }

        private static string GetDotNetVersion()
        {
            try
            {
                using (
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"
                    )
                )
                {
                    if (key?.GetValue("Release") is int release)
                    {
                        return ".NET Framework " + ReleaseToVersion(release);
                    }
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
            if (release >= 528040)
                return "4.8";

            if (release >= 461808)
                return "4.7.2";

            if (release >= 461308)
                return "4.7.1";

            if (release >= 460798)
                return "4.7";

            return release.ToString();
        }
    }
}
