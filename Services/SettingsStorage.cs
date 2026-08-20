using System.IO;
using System.Xml.Serialization;
using BatUtils.Models;

namespace BatUtils.Services
{
    public static class SettingsStorage
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(AppSettings));

        private static AppSettings _current;

        public static AppSettings Current
        {
            get
            {
                if (_current == null)
                    _current = Load();

                return _current;
            }
        }

        private static AppSettings Load()
        {
            if (!File.Exists(AppPaths.SettingsFile))
                return new AppSettings();

            try
            {
                using (var stream = File.OpenRead(AppPaths.SettingsFile))
                {
                    return (AppSettings)Serializer.Deserialize(stream);
                }
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save()
        {
            Directory.CreateDirectory(AppPaths.DataDirectory);

            string tempFile = AppPaths.SettingsFile + ".tmp";

            try
            {
                using (var stream = File.Create(tempFile))
                {
                    Serializer.Serialize(stream, Current);
                }

                ReplaceFile(tempFile, AppPaths.SettingsFile);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        private static void ReplaceFile(string tempFile, string targetFile)
        {
            if (File.Exists(targetFile))
            {
                File.Replace(tempFile, targetFile, null);
            }
            else
            {
                File.Move(tempFile, targetFile);
            }
        }
    }
}
