using System;
using System.Collections.Generic;
using System.Text;

namespace AasxServerBlazor.Data
{
    public class CredentialService : IDisposable
    {
        public List<AasxServer.AasxCredentialsEntry> credentials = new List<AasxServer.AasxCredentialsEntry>();

        public CredentialService()
        {
        }

        public void Dispose()
        {
        }
    }
}
