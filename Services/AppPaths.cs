using System;
using System.IO;

namespace BatUtils.Services
{
    public static class AppPaths
    {
        public static string DataDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "BAT",
                "BatUtils"
            );

        public static string SettingsFile => Path.Combine(DataDirectory, "settings.xml");

        public static string ClientsFile => Path.Combine(DataDirectory, "clients.csv");

        public static string CredentialsFile => Path.Combine(DataDirectory, "credentials.dat");
    }
}
