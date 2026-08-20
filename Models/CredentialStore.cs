using System.Collections.Generic;

namespace BatUtils.Models
{
    public class CredentialStore
    {
        public List<ClientCredentials> Clients { get; set; } = new List<ClientCredentials>();
    }
}
