namespace BatUtils.Models
{
    public sealed class ServerStatusResult
    {
        public string Status { get; }

        public string Details { get; }

        private ServerStatusResult(string status, string details)
        {
            Status = status;
            Details = details;
        }

        public static ServerStatusResult Success(string version)
        {
            return new ServerStatusResult(
                string.Format(Properties.Strings.ClientStatusOkVersion, version.Trim()),
                null
            );
        }

        public static ServerStatusResult Failure(string details)
        {
            return new ServerStatusResult(Properties.Strings.ClientStatusUnreachable, details);
        }
    }
}
