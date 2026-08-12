using BatUtils.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BatUtils.Services
{
    public static class ClientStorage
    {
        private const char Separator = ';';

        private const string Header =
            "Enabled;Code;Name;RKServerAddress;RKServerPort;SHServerAddress;SHServerPort";

        private static readonly string DataDirectory =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        private static readonly string ClientsFile =
            Path.Combine(DataDirectory, "clients.csv");

        public static ObservableCollection<Client> Load()
        {
            EnsureFileExists();

            var clients = new ObservableCollection<Client>();

            foreach (string line in File.ReadLines(ClientsFile).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split(Separator);

                if (parts.Length < 7)
                    continue;

                clients.Add(new Client
                {
                    Enabled = bool.TryParse(parts[0], out bool enabled) && enabled,
                    Code = parts[1],
                    Name = parts[2],
                    RKServerAddress = parts[3],
                    RKServerPort = ushort.TryParse(parts[4], out ushort port)
                        ? port
                        : (ushort)0,
                    SHServerAddress = parts[5],
                    SHServerPort = ushort.TryParse(parts[6], out ushort shPort)
                        ? shPort
                        : (ushort)0
                });
            }

            return clients;
        }

        public static void Save(IEnumerable<Client> clients)
        {
            EnsureFileExists();

            using (var writer = new StreamWriter(ClientsFile, false))
            {
                writer.WriteLine(Header);

                foreach (Client client in clients)
                {
                    writer.WriteLine(
                        string.Join(Separator.ToString(),
                            client.Enabled,
                            client.Code,
                            client.Name,
                            client.RKServerAddress,
                            client.RKServerPort,
                            client.SHServerAddress,
                            client.SHServerPort));
                }
            }
        }

        private static void EnsureFileExists()
        {
            Directory.CreateDirectory(DataDirectory);

            if (!File.Exists(ClientsFile))
            {
                File.WriteAllText(
                    ClientsFile,
                    Header + Environment.NewLine);
            }
        }
    }
}