using System.IO;

namespace BatUtils.Services
{
    public static class UcsManager
    {
        private const string ClientsDirectory = "CLIENTS";
        private const string DistributionDirectory = "DIST";

        private const string Rk7Directory = "Rk7Manager";
        private const string Sh5Directory = "Sh5Client";

        private const string Rk7Executable = "rk7man.exe";
        private const string Sh5Executable = "Sh.exe";


        // =========================================================
        // ROOT
        // =========================================================

        public static string GetClientsFolder(string rootFolder)
        {
            return Path.Combine(
                rootFolder,
                ClientsDirectory);
        }

        public static string GetDistributionFolder(string rootFolder)
        {
            return Path.Combine(
                rootFolder,
                DistributionDirectory);
        }


        // =========================================================
        // CLIENT
        // =========================================================

        public static string GetClientFolder(
            string rootFolder,
            string clientCode)
        {
            return Path.Combine(
                GetClientsFolder(rootFolder),
                clientCode);
        }


        // =========================================================
        // RK7
        // =========================================================

        public static string GetRk7Folder(
            string rootFolder,
            string clientCode)
        {
            return Path.Combine(
                GetClientFolder(rootFolder, clientCode),
                Rk7Directory);
        }


        public static string GetRk7Executable(
            string rootFolder,
            string clientCode)
        {
            return Path.Combine(
                GetRk7Folder(rootFolder, clientCode),
                Rk7Executable);
        }


        public static bool IsRk7Installed(
            string rootFolder,
            string clientCode)
        {
            return File.Exists(
                GetRk7Executable(rootFolder, clientCode));
        }


        // =========================================================
        // SH5
        // =========================================================

        public static string GetSh5Folder(
            string rootFolder,
            string clientCode)
        {
            return Path.Combine(
                GetClientFolder(rootFolder, clientCode),
                Sh5Directory);
        }


        public static string GetSh5Executable(
            string rootFolder,
            string clientCode)
        {
            return Path.Combine(
                GetSh5Folder(rootFolder, clientCode),
                Sh5Executable);
        }


        public static bool IsSh5Installed(
            string rootFolder,
            string clientCode)
        {
            return File.Exists(
                GetSh5Executable(rootFolder, clientCode));
        }
    }
}