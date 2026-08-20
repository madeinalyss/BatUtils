using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BatUtils.Models;

namespace BatUtils.Services
{
    public static class ClientStorage
    {
        private const char Separator = ';';

        private const int FieldCount = 9;
        private const int LegacyFieldCount = 7;

        private const string Header =
            "Enabled;Code;Name;RKServerAddress;RKServerPort;SHServerAddress;SHServerPort;RKUseHttps;SHUseHttps";

        private static readonly Encoding FileEncoding = new UTF8Encoding(true);

        private static readonly object SyncRoot = new object();

        public static ObservableCollection<Client> Load()
        {
            EnsureFileExists();

            var clients = new ObservableCollection<Client>();

            foreach (string line in File.ReadLines(AppPaths.ClientsFile, FileEncoding).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (!TryParseLine(line, out string[] fields))
                {
                    continue;
                }

                if (fields.Length != FieldCount && fields.Length != LegacyFieldCount)
                    continue;

                clients.Add(CreateClient(fields));
            }

            return clients;
        }

        public static void Save(IEnumerable<Client> clients)
        {
            if (clients == null)
                throw new ArgumentNullException(nameof(clients));

            lock (SyncRoot)
            {
                Directory.CreateDirectory(AppPaths.DataDirectory);

                string tempFile = Path.Combine(
                    AppPaths.DataDirectory,
                    "clients." + Guid.NewGuid().ToString("N") + ".tmp"
                );

                try
                {
                    WriteFile(tempFile, clients);

                    ReplaceFile(tempFile, AppPaths.ClientsFile);
                }
                finally
                {
                    TryDeleteFile(tempFile);
                }
            }
        }

        private static Client CreateClient(string[] fields)
        {
            return new Client
            {
                Enabled = !bool.TryParse(fields[0], out bool enabled) || enabled,

                Code = fields[1],

                Name = fields[2],

                RKServerAddress = fields[3],

                RKServerPort = ParsePort(fields[4]),

                SHServerAddress = fields[5],

                SHServerPort = ParsePort(fields[6]),

                // Preserve the protocols used before these fields were added.
                RKUseHttps = fields.Length == LegacyFieldCount || ParseBoolean(fields[7]),

                SHUseHttps = fields.Length != LegacyFieldCount && ParseBoolean(fields[8]),
            };
        }

        private static string BuildLine(Client client)
        {
            return string.Join(
                Separator.ToString(),
                client.Enabled.ToString(),
                EscapeField(client.Code),
                EscapeField(client.Name),
                EscapeField(client.RKServerAddress),
                client.RKServerPort.ToString(CultureInfo.InvariantCulture),
                EscapeField(client.SHServerAddress),
                client.SHServerPort.ToString(CultureInfo.InvariantCulture),
                client.RKUseHttps.ToString(),
                client.SHUseHttps.ToString()
            );
        }

        private static ushort ParsePort(string value)
        {
            return ushort.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out ushort port
            )
                ? port
                : (ushort)0;
        }

        private static bool ParseBoolean(string value)
        {
            return bool.TryParse(value, out bool result) && result;
        }

        private static string EscapeField(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            value = value.Replace("\r", " ").Replace("\n", " ");

            bool requiresQuotes = value.IndexOf(Separator) >= 0 || value.IndexOf('"') >= 0;

            if (!requiresQuotes)
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static bool TryParseLine(string line, out string[] fields)
        {
            var result = new List<string>();

            var field = new StringBuilder();

            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;

                        continue;
                    }

                    inQuotes = !inQuotes;

                    continue;
                }

                if (c == Separator && !inQuotes)
                {
                    result.Add(field.ToString());

                    field.Clear();

                    continue;
                }

                field.Append(c);
            }

            if (inQuotes)
            {
                fields = null;
                return false;
            }

            result.Add(field.ToString());

            fields = result.ToArray();

            return true;
        }

        private static void WriteFile(string path, IEnumerable<Client> clients)
        {
            using (
                var stream = new FileStream(
                    path,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None
                )
            )
            using (var writer = new StreamWriter(stream, FileEncoding))
            {
                writer.WriteLine(Header);

                foreach (Client client in clients)
                {
                    writer.WriteLine(BuildLine(client));
                }

                writer.Flush();

                stream.Flush(true);
            }
        }

        private static void EnsureFileExists()
        {
            Directory.CreateDirectory(AppPaths.DataDirectory);

            if (File.Exists(AppPaths.ClientsFile))
            {
                return;
            }

            try
            {
                using (
                    var stream = new FileStream(
                        AppPaths.ClientsFile,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None
                    )
                )
                using (var writer = new StreamWriter(stream, FileEncoding))
                {
                    writer.WriteLine(Header);
                }
            }
            catch (IOException)
            {
                // Another instance may have created the file after the existence check.
                if (!File.Exists(AppPaths.ClientsFile))
                    throw;
            }
        }

        private static void ReplaceFile(string sourceFile, string targetFile)
        {
            if (File.Exists(targetFile))
            {
                File.Replace(sourceFile, targetFile, null);
            }
            else
            {
                File.Move(sourceFile, targetFile);
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // A cleanup failure must not hide the original save exception.
            }
        }
    }
}
