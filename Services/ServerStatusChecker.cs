using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using BatUtils.Models;

namespace BatUtils.Services
{
    public static class ServerStatusChecker
    {
        private const int TimeoutSeconds = 12;

        public static async Task<ServerStatusResult> CheckRk7Async(
            Client client,
            ClientCredentials credentials
        )
        {
            if (
                !TryBuildUri(
                    client?.RKUseHttps == true ? "https" : "http",
                    client?.RKServerAddress,
                    client?.RKServerPort ?? 0,
                    "/rk7api/v0/xmlinterface.xml",
                    out Uri uri
                )
            )
            {
                return InvalidEndpoint();
            }

            try
            {
                using (var handler = CreateHandler(client.RKUseHttps))
                {
                    using (var httpClient = CreateClient(handler))
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                    {
                        request.Headers.TryAddWithoutValidation("Content-Type", "application/xml");

                        SetBasicAuthentication(
                            request,
                            credentials?.RKUserName,
                            credentials?.RKPassword
                        );

                        using (HttpResponseMessage response = await httpClient.SendAsync(request))
                        {
                            if (!response.IsSuccessStatusCode)
                                return HttpFailure(response);

                            string xml = await response.Content.ReadAsStringAsync();

                            string version;

                            try
                            {
                                version = ReadRk7Version(xml);
                            }
                            catch (Exception ex)
                            {
                                return ServerStatusResult.Failure(
                                    string.Format(
                                        Properties.Strings.ClientStatusRkInvalidXml,
                                        ex.Message
                                    )
                                );
                            }

                            return SuccessOrMissingVersion(
                                version,
                                Properties.Strings.ClientStatusRkMissingVersion
                            );
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return TimedOut();
            }
            catch (HttpRequestException ex)
            {
                return ConnectionFailure(ex);
            }
            catch (Exception ex)
            {
                return UnexpectedFailure(ex);
            }
        }

        public static async Task<ServerStatusResult> CheckSh5Async(
            Client client,
            ClientCredentials credentials
        )
        {
            if (
                !TryBuildUri(
                    client?.SHUseHttps == true ? "https" : "http",
                    client?.SHServerAddress,
                    client?.SHServerPort ?? 0,
                    "/api/sh5info",
                    out Uri uri
                )
            )
            {
                return InvalidEndpoint();
            }

            try
            {
                using (var handler = CreateHandler(client.SHUseHttps))
                using (var httpClient = CreateClient(handler))
                using (var content = CreateSh5Content(credentials))
                using (HttpResponseMessage response = await httpClient.PostAsync(uri, content))
                {
                    if (!response.IsSuccessStatusCode)
                        return HttpFailure(response);

                    string json = await response.Content.ReadAsStringAsync();

                    Sh5Info info;

                    try
                    {
                        info = DeserializeSh5Info(json);
                    }
                    catch (Exception ex)
                    {
                        return ServerStatusResult.Failure(
                            string.Format(Properties.Strings.ClientStatusShInvalidJson, ex.Message)
                        );
                    }

                    if (info == null || info.ErrorCode != 0)
                    {
                        return ServerStatusResult.Failure(
                            string.IsNullOrWhiteSpace(info?.ErrorMessage)
                                ? Properties.Strings.ClientStatusShErrorResponse
                                : string.Format(
                                    Properties.Strings.ClientStatusShError,
                                    info.ErrorMessage
                                )
                        );
                    }

                    return SuccessOrMissingVersion(
                        info.Database?.Version,
                        Properties.Strings.ClientStatusShMissingVersion
                    );
                }
            }
            catch (TaskCanceledException)
            {
                return TimedOut();
            }
            catch (HttpRequestException ex)
            {
                return ConnectionFailure(ex);
            }
            catch (Exception ex)
            {
                return UnexpectedFailure(ex);
            }
        }

        private static HttpClient CreateClient(HttpMessageHandler handler)
        {
            return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutSeconds) };
        }

        private static HttpClientHandler CreateHandler(bool useHttps)
        {
            var handler = new HttpClientHandler();

            if (useHttps)
            {
                handler.ServerCertificateCustomValidationCallback = (
                    request,
                    certificate,
                    chain,
                    errors
                ) => true;

                handler.SslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
            }

            return handler;
        }

        private static void SetBasicAuthentication(
            HttpRequestMessage request,
            string userName,
            string password
        )
        {
            string value = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    (userName ?? string.Empty) + ":" + (password ?? string.Empty)
                )
            );

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", value);
        }

        private static StringContent CreateSh5Content(ClientCredentials credentials)
        {
            var login = new Sh5Login
            {
                UserName = credentials?.SHUserName ?? string.Empty,

                Password = credentials?.SHPassword ?? string.Empty,
            };

            string json;

            var serializer = new DataContractJsonSerializer(typeof(Sh5Login));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, login);

                json = Encoding.UTF8.GetString(stream.ToArray());
            }

            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private static string ReadRk7Version(string xml)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,

                XmlResolver = null,
            };

            using (var textReader = new StringReader(xml))
            using (XmlReader reader = XmlReader.Create(textReader, settings))
            {
                XDocument document = XDocument.Load(reader);

                return (string)document.Root?.Attribute("ServerVersion");
            }
        }

        private static Sh5Info DeserializeSh5Info(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(Sh5Info));

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return serializer.ReadObject(stream) as Sh5Info;
            }
        }

        private static bool TryBuildUri(
            string scheme,
            string address,
            ushort port,
            string path,
            out Uri uri
        )
        {
            uri = null;

            if (string.IsNullOrWhiteSpace(address) || port == 0)
            {
                return false;
            }

            string value = address.Trim();

            if (!value.Contains("://"))
                value = scheme + "://" + value;

            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri addressUri))
            {
                return false;
            }

            var builder = new UriBuilder(scheme, addressUri.Host, port, path);

            uri = builder.Uri;
            return true;
        }

        private static ServerStatusResult SuccessOrMissingVersion(
            string version,
            string missingVersionMessage
        )
        {
            return string.IsNullOrWhiteSpace(version)
                ? ServerStatusResult.Failure(missingVersionMessage)
                : ServerStatusResult.Success(version);
        }

        private static ServerStatusResult InvalidEndpoint()
        {
            return ServerStatusResult.Failure(Properties.Strings.ClientStatusInvalidEndpoint);
        }

        private static ServerStatusResult HttpFailure(HttpResponseMessage response)
        {
            if (
                response.StatusCode == HttpStatusCode.Unauthorized
                || response.StatusCode == HttpStatusCode.Forbidden
            )
            {
                return ServerStatusResult.Failure(
                    Properties.Strings.ClientStatusAuthenticationRejected
                );
            }

            return ServerStatusResult.Failure(
                string.Format(
                    Properties.Strings.ClientStatusHttpError,
                    (int)response.StatusCode,
                    response.ReasonPhrase
                )
            );
        }

        private static ServerStatusResult TimedOut()
        {
            return ServerStatusResult.Failure(
                string.Format(Properties.Strings.ClientStatusTimeout, TimeoutSeconds)
            );
        }

        private static ServerStatusResult ConnectionFailure(HttpRequestException exception)
        {
            Exception cause = exception.GetBaseException();

            return ServerStatusResult.Failure(
                string.Format(Properties.Strings.ClientStatusConnectionFailed, cause.Message)
            );
        }

        private static ServerStatusResult UnexpectedFailure(Exception exception)
        {
            return ServerStatusResult.Failure(
                string.Format(Properties.Strings.ClientStatusUnexpectedError, exception.Message)
            );
        }

        [DataContract]
        private sealed class Sh5Login
        {
            [DataMember(Name = "UserName")]
            public string UserName { get; set; }

            [DataMember(Name = "Password")]
            public string Password { get; set; }
        }

        [DataContract]
        private sealed class Sh5Info
        {
            [DataMember(Name = "errorCode")]
            public int ErrorCode { get; set; }

            [DataMember(Name = "errMessage")]
            public string ErrorMessage { get; set; }

            [DataMember(Name = "DB")]
            public Sh5Database Database { get; set; }
        }

        [DataContract]
        private sealed class Sh5Database
        {
            [DataMember(Name = "Version")]
            public string Version { get; set; }
        }
    }
}
