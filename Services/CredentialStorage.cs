using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using BatUtils.Models;

namespace BatUtils.Services
{
    public enum CredentialUnlockFailure
    {
        None,
        InvalidPasswordOrCorrupted,
        InvalidStore,
        Unavailable,
    }

    public sealed class CredentialUnlockResult
    {
        public bool Succeeded => Failure == CredentialUnlockFailure.None;

        public CredentialUnlockFailure Failure { get; }

        public string Details { get; }

        private CredentialUnlockResult(CredentialUnlockFailure failure, string details = null)
        {
            Failure = failure;
            Details = details;
        }

        public static CredentialUnlockResult Success()
        {
            return new CredentialUnlockResult(CredentialUnlockFailure.None);
        }

        public static CredentialUnlockResult Failed(
            CredentialUnlockFailure failure,
            string details = null
        )
        {
            return new CredentialUnlockResult(failure, details);
        }
    }

    public static class CredentialStorage
    {
        private static CredentialStore _current;
        private static string _password;

        private const string Magic = "BUCREDV1";

        private const int Iterations = 150000;
        private const int MaxIterations = 2000000;
        private const int MaxFileSize = 16 * 1024 * 1024;

        private const int SaltSize = 16;
        private const int EncryptionKeySize = 32;
        private const int MacKeySize = 32;
        private const int IvSize = 16;
        private const int HmacSize = 32;

        private static readonly Encoding FileEncoding = new UTF8Encoding(false);

        private static readonly XmlSerializer Serializer = new XmlSerializer(
            typeof(CredentialStore)
        );

        public static bool Exists => File.Exists(AppPaths.CredentialsFile);

        public static bool IsUnlocked => _current != null;

        public static CredentialStore Current
        {
            get
            {
                if (_current == null)
                    throw new InvalidOperationException("Credential store is locked.");

                return _current;
            }
        }

        public static CredentialUnlockResult Unlock(string password)
        {
            try
            {
                CredentialStore store = Load(password);

                _current = store;
                _password = password;

                return CredentialUnlockResult.Success();
            }
            catch (CryptographicException)
            {
                return CredentialUnlockResult.Failed(
                    CredentialUnlockFailure.InvalidPasswordOrCorrupted
                );
            }
            catch (InvalidDataException ex)
            {
                return CredentialUnlockResult.Failed(
                    CredentialUnlockFailure.InvalidStore,
                    ex.Message
                );
            }
            catch (InvalidOperationException ex)
            {
                return CredentialUnlockResult.Failed(
                    CredentialUnlockFailure.InvalidStore,
                    ex.Message
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                return CredentialUnlockResult.Failed(
                    CredentialUnlockFailure.Unavailable,
                    ex.Message
                );
            }
            catch (IOException ex)
            {
                return CredentialUnlockResult.Failed(
                    CredentialUnlockFailure.Unavailable,
                    ex.Message
                );
            }
        }

        public static void Lock()
        {
            _current = null;
            _password = null;
        }

        public static void Create(string password)
        {
            if (Exists)
                throw new InvalidOperationException("Credential store already exists.");

            var store = new CredentialStore();

            Save(store, password);

            _current = store;
            _password = password;
        }

        public static CredentialStore Load(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            if (!Exists)
                throw new FileNotFoundException(
                    "Credential store does not exist.",
                    AppPaths.CredentialsFile
                );

            long fileLength = new FileInfo(AppPaths.CredentialsFile).Length;

            if (fileLength <= 0 || fileLength > MaxFileSize)
            {
                throw new InvalidDataException("Credential store size is invalid.");
            }

            byte[] fileData = File.ReadAllBytes(AppPaths.CredentialsFile);

            if (fileData.Length > MaxFileSize)
            {
                throw new InvalidDataException("Credential store is too large.");
            }

            CredentialStore store = Decrypt(fileData, password);

            if (store == null || store.Clients == null)
            {
                throw new InvalidDataException("Credential store content is invalid.");
            }

            return store;
        }

        public static void Save()
        {
            if (_current == null || _password == null)
            {
                throw new InvalidOperationException("Credential store is locked.");
            }

            Save(_current, _password);
        }

        public static void Save(CredentialStore store, string password)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            Directory.CreateDirectory(AppPaths.DataDirectory);

            byte[] fileData = Encrypt(store, password);

            WriteAtomic(AppPaths.CredentialsFile, fileData);
        }

        public static ClientCredentials GetClient(string code)
        {
            return TryGetClient(code, out ClientCredentials credentials)
                ? credentials
                : new ClientCredentials { Code = code };
        }

        public static bool TryGetClient(string code, out ClientCredentials credentials)
        {
            EnsureUnlocked();

            ClientCredentials stored = _current.Clients.FirstOrDefault(item =>
                string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase)
            );

            credentials = stored == null ? null : Clone(stored);

            return stored != null;
        }

        public static void SetClient(string originalCode, ClientCredentials credentials)
        {
            EnsureUnlocked();

            if (credentials == null)
                throw new ArgumentNullException(nameof(credentials));

            if (string.IsNullOrWhiteSpace(credentials.Code))
                throw new ArgumentException("Client code cannot be empty.", nameof(credentials));

            var previousClients = CloneClients();

            try
            {
                _current.Clients.RemoveAll(item =>
                    string.Equals(item.Code, originalCode, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(
                        item.Code,
                        credentials.Code,
                        StringComparison.OrdinalIgnoreCase
                    )
                );

                _current.Clients.Add(Clone(credentials));
                Save();
            }
            catch
            {
                _current.Clients = previousClients;
                throw;
            }
        }

        public static void RemoveClient(string code)
        {
            EnsureUnlocked();

            var previousClients = CloneClients();

            try
            {
                int removed = _current.Clients.RemoveAll(item =>
                    string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase)
                );

                if (removed > 0)
                    Save();
            }
            catch
            {
                _current.Clients = previousClients;
                throw;
            }
        }

        private static byte[] Encrypt(CredentialStore store, string password)
        {
            byte[] plaintext = Serialize(store);

            byte[] salt = GenerateRandomBytes(SaltSize);

            byte[] iv = GenerateRandomBytes(IvSize);

            DeriveKeys(password, salt, Iterations, out byte[] encryptionKey, out byte[] macKey);

            byte[] ciphertext;

            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;

                aes.Mode = CipherMode.CBC;

                aes.Padding = PaddingMode.PKCS7;

                aes.Key = encryptionKey;

                aes.IV = iv;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
                }
            }

            byte[] authenticatedData = BuildAuthenticatedData(Iterations, salt, iv, ciphertext);

            byte[] hmac;

            using (var hmacSha256 = new HMACSHA256(macKey))
            {
                hmac = hmacSha256.ComputeHash(authenticatedData);
            }

            return BuildFile(Iterations, salt, iv, ciphertext, hmac);
        }

        private static CredentialStore Decrypt(byte[] fileData, string password)
        {
            ParseFile(
                fileData,
                out int iterations,
                out byte[] salt,
                out byte[] iv,
                out byte[] ciphertext,
                out byte[] storedHmac
            );

            DeriveKeys(password, salt, iterations, out byte[] encryptionKey, out byte[] macKey);

            byte[] authenticatedData = BuildAuthenticatedData(iterations, salt, iv, ciphertext);

            byte[] calculatedHmac;

            using (var hmacSha256 = new HMACSHA256(macKey))
            {
                calculatedHmac = hmacSha256.ComputeHash(authenticatedData);
            }

            if (!FixedTimeEquals(storedHmac, calculatedHmac))
            {
                throw new CryptographicException("Invalid password or corrupted credential store.");
            }

            byte[] plaintext;

            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;

                aes.Mode = CipherMode.CBC;

                aes.Padding = PaddingMode.PKCS7;

                aes.Key = encryptionKey;

                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    plaintext = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                }
            }

            return Deserialize(plaintext);
        }

        private static void DeriveKeys(
            string password,
            byte[] salt,
            int iterations,
            out byte[] encryptionKey,
            out byte[] macKey
        )
        {
            byte[] keyMaterial;

            using (
                var deriveBytes = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256
                )
            )
            {
                keyMaterial = deriveBytes.GetBytes(EncryptionKeySize + MacKeySize);
            }

            encryptionKey = new byte[EncryptionKeySize];

            macKey = new byte[MacKeySize];

            Buffer.BlockCopy(keyMaterial, 0, encryptionKey, 0, EncryptionKeySize);

            Buffer.BlockCopy(keyMaterial, EncryptionKeySize, macKey, 0, MacKeySize);
        }

        private static byte[] Serialize(CredentialStore store)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, store);

                return stream.ToArray();
            }
        }

        private static CredentialStore Deserialize(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return (CredentialStore)Serializer.Deserialize(stream);
            }
        }

        private static byte[] BuildFile(
            int iterations,
            byte[] salt,
            byte[] iv,
            byte[] ciphertext,
            byte[] hmac
        )
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, FileEncoding, true))
            {
                writer.Write(FileEncoding.GetBytes(Magic));

                writer.Write(iterations);

                writer.Write(salt);

                writer.Write(iv);

                writer.Write(ciphertext.Length);

                writer.Write(ciphertext);

                writer.Write(hmac);

                writer.Flush();

                return stream.ToArray();
            }
        }

        private static void ParseFile(
            byte[] fileData,
            out int iterations,
            out byte[] salt,
            out byte[] iv,
            out byte[] ciphertext,
            out byte[] hmac
        )
        {
            if (
                fileData == null
                || fileData.Length
                    < Magic.Length + sizeof(int) + SaltSize + IvSize + sizeof(int) + HmacSize
            )
            {
                throw new InvalidDataException("Invalid credential store.");
            }

            using (var stream = new MemoryStream(fileData))
            using (var reader = new BinaryReader(stream, FileEncoding, true))
            {
                string magic = FileEncoding.GetString(reader.ReadBytes(Magic.Length));

                if (magic != Magic)
                {
                    throw new InvalidDataException("Unsupported credential store format.");
                }

                iterations = reader.ReadInt32();

                if (iterations <= 0 || iterations > MaxIterations)
                {
                    throw new InvalidDataException("Invalid credential store.");
                }

                salt = reader.ReadBytes(SaltSize);

                iv = reader.ReadBytes(IvSize);

                int ciphertextLength = reader.ReadInt32();

                long remaining = stream.Length - stream.Position;

                if (
                    ciphertextLength <= 0
                    || ciphertextLength % IvSize != 0
                    || remaining != ciphertextLength + HmacSize
                )
                {
                    throw new InvalidDataException("Invalid credential store.");
                }

                ciphertext = reader.ReadBytes(ciphertextLength);

                hmac = reader.ReadBytes(HmacSize);
            }
        }

        private static byte[] BuildAuthenticatedData(
            int iterations,
            byte[] salt,
            byte[] iv,
            byte[] ciphertext
        )
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, FileEncoding, true))
            {
                writer.Write(FileEncoding.GetBytes(Magic));

                writer.Write(iterations);

                writer.Write(salt);

                writer.Write(iv);

                writer.Write(ciphertext.Length);

                writer.Write(ciphertext);

                writer.Flush();

                return stream.ToArray();
            }
        }

        private static byte[] GenerateRandomBytes(int length)
        {
            byte[] data = new byte[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data);
            }

            return data;
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            int difference = 0;

            for (int i = 0; i < left.Length; i++)
            {
                difference |= left[i] ^ right[i];
            }

            return difference == 0;
        }

        private static void EnsureUnlocked()
        {
            if (_current == null)
            {
                throw new InvalidOperationException("Credential store is locked.");
            }
        }

        private static ClientCredentials Clone(ClientCredentials credentials)
        {
            return new ClientCredentials
            {
                Code = credentials.Code,
                RKUserName = credentials.RKUserName,
                RKPassword = credentials.RKPassword,
                SHUserName = credentials.SHUserName,
                SHPassword = credentials.SHPassword,
            };
        }

        private static System.Collections.Generic.List<ClientCredentials> CloneClients()
        {
            return _current.Clients.Select(Clone).ToList();
        }

        private static void WriteAtomic(string targetFile, byte[] data)
        {
            string tempFile = Path.Combine(
                AppPaths.DataDirectory,
                "credentials." + Guid.NewGuid().ToString("N") + ".tmp"
            );

            try
            {
                using (
                    var stream = new FileStream(
                        tempFile,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None
                    )
                )
                {
                    stream.Write(data, 0, data.Length);

                    stream.Flush(true);
                }

                if (File.Exists(targetFile))
                {
                    File.Replace(tempFile, targetFile, null);
                }
                else
                {
                    File.Move(tempFile, targetFile);
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
                catch
                {
                    // A cleanup failure must not hide the original exception.
                }
            }
        }
    }
}
