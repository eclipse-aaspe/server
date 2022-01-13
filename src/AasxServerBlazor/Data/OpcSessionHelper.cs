using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AasxServerBlazor.Data
{
    public class OpcSessionCacheData
    {
        public bool Trusted { get; set; }

        public Session OPCSession { get; set; }

        public string CertThumbprint { get; set; }

        public Uri EndpointURL { get; set; }

        public OpcSessionCacheData()
        {
            Trusted = false;
            EndpointURL = new Uri("opc.tcp://localhost:4840");
            CertThumbprint = string.Empty;
            OPCSession = null;
        }
    }

    public class OpcSessionHelper
    {
        public ConcurrentDictionary<string, OpcSessionCacheData> OpcSessionCache = new ConcurrentDictionary<string, OpcSessionCacheData>();

        private static OpcSessionHelper _instance = null;
        private static Object _instanceLock = new Object();

        private static SemaphoreSlim _trustedSessionCertificateValidation = null;

        private static string _trustedSessionId { get; set; } = null;

        internal static string Delimiter { get; } = "__$__";

        public static OpcSessionHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new OpcSessionHelper();
                        }
                    }
                }

                return _instance;
            }
        }

        public OpcSessionHelper()
        {
            _trustedSessionCertificateValidation = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Action to disconnect from the currently connected OPC UA server.
        /// </summary>
        public void Disconnect(string sessionID)
        {
            OpcSessionCacheData entry;
            if (OpcSessionCache.TryRemove(sessionID, out entry))
            {
                try
                {
                    if (entry.OPCSession != null)
                    {
                        entry.OPCSession.Close();
                    }
                }
                catch (Exception)
                {
                    // do nothing
                }
            }
        }

        /// <summary>
        /// Ensures session is closed when server does not reply.
        /// </summary>
        private static void StandardClient_KeepAlive(Session sender, KeepAliveEventArgs e)
        {
            if (e != null)
            {
                if (ServiceResult.IsBad(e.Status))
                {
                    e.CancelKeepAlive = true;

                    sender.Close();
                }
            }
        }

        /// <summary>
        /// Checks if there is an active OPC UA session for the provided browser session. If the persisted OPC UA session does not exist,
        /// a new OPC UA session to the given endpoint URL is established.
        /// </summary>
        public async Task<Session> GetSessionAsync(ApplicationConfiguration config, string sessionID, string endpointURL, bool enforceTrust = false)
        {
            if (string.IsNullOrEmpty(sessionID) || string.IsNullOrEmpty(endpointURL))
            {
                return null;
            }

            OpcSessionCacheData entry;
            if (OpcSessionCache.TryGetValue(sessionID, out entry))
            {
                if (entry.OPCSession != null)
                {
                    if (entry.OPCSession.Connected)
                    {
                        return entry.OPCSession;
                    }

                    try
                    {
                        entry.OPCSession.Close(500);
                    }
                    catch (Exception)
                    {
                        // do nothing
                    }

                    entry.OPCSession = null;
                }
            }
            else
            {
                // create a new entry
                OpcSessionCacheData newEntry = new OpcSessionCacheData { EndpointURL = new Uri(endpointURL) };
                OpcSessionCache.TryAdd(sessionID, newEntry);
            }

            Uri endpointURI = new Uri(endpointURL);
            EndpointDescriptionCollection endpointCollection = DiscoverEndpoints(config, endpointURI, 10);
            EndpointDescription selectedEndpoint = SelectUaTcpEndpoint(endpointCollection);
            EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(config);
            ConfiguredEndpoint endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

            Session session = null;
            try
            {
                // lock the session creation for the enforced trust case
                await _trustedSessionCertificateValidation.WaitAsync().ConfigureAwait(false);

                if (enforceTrust)
                {
                    // enforce trust in the certificate validator by setting the trusted session id
                    _trustedSessionId = sessionID;
                }

                session = await Session.Create(
                    config,
                    endpoint,
                    true,
                    false,
                    sessionID,
                    60000,
                    new UserIdentity(new AnonymousIdentityToken()),
                    null).ConfigureAwait(false);

                if (session != null)
                {
                    session.KeepAlive += new KeepAliveEventHandler(StandardClient_KeepAlive);

                    // Update our cache data
                    if (OpcSessionCache.TryGetValue(sessionID, out entry))
                    {
                        if (string.Equals(entry.EndpointURL.AbsoluteUri, endpointURL, StringComparison.InvariantCultureIgnoreCase))
                        {
                            OpcSessionCacheData newValue = new OpcSessionCacheData
                            {
                                CertThumbprint = entry.CertThumbprint,
                                EndpointURL = entry.EndpointURL,
                                Trusted = entry.Trusted,
                                OPCSession = session
                            };
                            OpcSessionCache.TryUpdate(sessionID, newValue, entry);
                        }
                    }
                }
            }
            finally
            {
                _trustedSessionId = null;
                _trustedSessionCertificateValidation.Release();
            }

            return session;
        }

        /// <summary>
        /// Uses a discovery client to discover the endpoint description of a given server
        /// </summary>
        private EndpointDescriptionCollection DiscoverEndpoints(ApplicationConfiguration config, Uri discoveryUrl, int timeout)
        {
            EndpointConfiguration configuration = EndpointConfiguration.Create(config);
            configuration.OperationTimeout = timeout;

            using (DiscoveryClient client = DiscoveryClient.Create(
                discoveryUrl,
                EndpointConfiguration.Create(config)))
            {
                try
                {
                    EndpointDescriptionCollection endpoints = client.GetEndpoints(null);
                    return ReplaceLocalHostWithRemoteHost(endpoints, discoveryUrl);
                }
                catch (Exception e)
                {
                    Trace.TraceError("Can not fetch endpoints from url: {0}", discoveryUrl);
                    Trace.TraceError("Reason = {0}", e.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Selects the UA TCP endpoint with the highest security level
        /// </summary>
        private EndpointDescription SelectUaTcpEndpoint(EndpointDescriptionCollection endpointCollection)
        {
            EndpointDescription bestEndpoint = null;
            foreach (EndpointDescription endpoint in endpointCollection)
            {
                if (endpoint.TransportProfileUri == Profiles.UaTcpTransport)
                {
                    if ((bestEndpoint == null) ||
                        (endpoint.SecurityLevel > bestEndpoint.SecurityLevel))
                    {
                        bestEndpoint = endpoint;
                    }
                }
            }

            return bestEndpoint;
        }

        /// <summary>
        /// Parsing JSTreeNode to read OPC UA Node ID
        /// </summary>
        /// <param name="nodeID"></param>
        /// <returns></returns>
        internal static string GetNodeIdFromJsTreeNode(string nodeID)
        {
            string[] delimiter = { Delimiter };
            string[] nodeIDSplit = nodeID.Split(delimiter, 3, StringSplitOptions.None);

            string node;
            if (nodeIDSplit.Length == 1)
            {
                node = nodeIDSplit[0];
            }
            else
            {
                node = nodeIDSplit[1];
            }
            return node;
        }

        /// <summary>
        /// Replaces all instances of "LocalHost" in a collection of endpoint description with the real host name
        /// </summary>
        private EndpointDescriptionCollection ReplaceLocalHostWithRemoteHost(EndpointDescriptionCollection endpoints, Uri discoveryUrl)
        {
            EndpointDescriptionCollection updatedEndpoints = endpoints;

            foreach (EndpointDescription endpoint in updatedEndpoints)
            {
                endpoint.EndpointUrl = Utils.ReplaceLocalhost(endpoint.EndpointUrl, discoveryUrl.DnsSafeHost);

                StringCollection updatedDiscoveryUrls = new StringCollection();
                foreach (string url in endpoint.Server.DiscoveryUrls)
                {
                    updatedDiscoveryUrls.Add(Utils.ReplaceLocalhost(url, discoveryUrl.DnsSafeHost));
                }

                endpoint.Server.DiscoveryUrls = updatedDiscoveryUrls;
            }

            return updatedEndpoints;
        }
    }
}