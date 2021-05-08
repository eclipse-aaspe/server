/* Copyright (c) 1996-2016, OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
   version 2 of the License are accompanied with this source code. See http://opcfoundation.org/License/GPLv2
   This source code is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
*/
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AdminShellNS;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace SampleClient
{
    public class UASampleClient
    {
        const int ReconnectPeriod = 10;
        public Session session;
        SessionReconnectHandler reconnectHandler;
        string endpointURL;
        int clientRunTime = Timeout.Infinite;
        static bool autoAccept = true;
        static ExitCode exitCode;
        string userName;
        string password;

        public UASampleClient(string _endpointURL, bool _autoAccept, int _stopTimeout, string _userName, string _password)
        {
            endpointURL = _endpointURL;
            autoAccept = _autoAccept;
            clientRunTime = _stopTimeout <= 0 ? Timeout.Infinite : _stopTimeout * 1000;
            userName = _userName;
            password = _password;
        }

        public void Run()
        {
            try
            {
                ConsoleSampleClient().Wait();
            }
            catch (Exception ex)
            {
                Utils.Trace("ServiceResultException:" + ex.Message);
                Console.WriteLine("Exception: {0}", ex.Message);
                Console.WriteLine("press any key to continue");
                return;
            }

            ManualResetEvent quitEvent = new ManualResetEvent(false);
            try
            {
                Console.CancelKeyPress += (sender, eArgs) =>
                {
                    quitEvent.Set();
                    eArgs.Cancel = true;
                };
            }
            catch
            {
            }

            // wait for timeout or Ctrl-C
            quitEvent.WaitOne(clientRunTime);

            // return error conditions
            if (session.KeepAliveStopped)
            {
                exitCode = ExitCode.ErrorNoKeepAlive;
                return;
            }

            exitCode = ExitCode.Ok;
        }

        public static ExitCode ExitCode { get => exitCode; }

        public async Task ConsoleSampleClient()
        {
            exitCode = ExitCode.ErrorCreateApplication;

            ApplicationInstance application = new ApplicationInstance
            {
                ApplicationName = "UA Core Sample Client",
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = Utils.IsRunningOnMono() ? "Opc.Ua.MonoSampleClient" : "Opc.Ua.SampleClient"
            };

            // load the application configuration.
            ApplicationConfiguration config = await application.LoadApplicationConfiguration(false);

            // check the application certificate.
            bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, 0);
            if (!haveAppCertificate)
            {
                throw new Exception("Application instance certificate invalid!");
            }

            if (haveAppCertificate)
            {
                config.ApplicationUri = Utils.GetApplicationUriFromCertificate(config.SecurityConfiguration.ApplicationCertificate.Certificate);
                if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                {
                    autoAccept = true;
                }
                config.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
            }
            else
            {
            }

            exitCode = ExitCode.ErrorDiscoverEndpoints;
            var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointURL, haveAppCertificate, 15000);
            // Console.WriteLine("    Selected endpoint uses: {0}",
            selectedEndpoint.SecurityPolicyUri.Substring(selectedEndpoint.SecurityPolicyUri.LastIndexOf('#') + 1);

            exitCode = ExitCode.ErrorCreateSession;
            var endpointConfiguration = EndpointConfiguration.Create(config);
            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

            if ((userName == "" || userName == null) && (password == "" || password == null))
            {
                session = await Session.Create(config, endpoint, false, "OPC UA Console Client", 60000, new UserIdentity(new AnonymousIdentityToken()), null);
            }
            else
            {
                session = await Session.Create(config, endpoint, false, "OPC UA Console Client", 60000, new UserIdentity(userName, password), null);
            }

            // register keep alive handler
            session.KeepAlive += Client_KeepAlive;
        }

        private void Client_KeepAlive(Session sender, KeepAliveEventArgs e)
        {
            if (e.Status != null && ServiceResult.IsNotGood(e.Status))
            {
                Console.WriteLine("{0} {1}/{2}", e.Status, sender.OutstandingRequestCount, sender.DefunctRequestCount);

                if (reconnectHandler == null)
                {
                    Console.WriteLine("--- RECONNECTING ---");
                    reconnectHandler = new SessionReconnectHandler();
                    reconnectHandler.BeginReconnect(sender, ReconnectPeriod * 1000, Client_ReconnectComplete);
                }
            }
        }

        private void Client_ReconnectComplete(object sender, EventArgs e)
        {
            // ignore callbacks from discarded objects.
            if (!Object.ReferenceEquals(sender, reconnectHandler))
            {
                return;
            }

            session = reconnectHandler.Session;
            reconnectHandler.Dispose();
            reconnectHandler = null;

            Console.WriteLine("--- RECONNECTED ---");
        }

        private static void OnNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            foreach (var value in item.DequeueValues())
            {
                Console.WriteLine("{0}: {1}, {2}, {3}", item.DisplayName, value.Value, value.SourceTimestamp, value.StatusCode);
            }
        }

        private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                e.Accept = autoAccept;
            }
        }

        public string ReadSubmodelElementValue(string nodeName, int index)
        {
            NodeId node = new NodeId(nodeName, (ushort)index);
            return (session.ReadValue(node).ToString());
        }

        public string ReadSubmodelElementValue(NodeId nodeId)
        {
            return (session.ReadValue(nodeId).ToString(null, CultureInfo.InvariantCulture));
        }

        public void WriteSubmodelElementValue(NodeId nodeId, object value)
        {
            WriteValue nodeToWrite = new WriteValue();
            nodeToWrite.NodeId = nodeId;
            nodeToWrite.AttributeId = Attributes.Value;
            nodeToWrite.Value = new DataValue();
            nodeToWrite.Value.WrappedValue = new Variant(value);

            WriteValueCollection nodesToWrite = new WriteValueCollection();
            nodesToWrite.Add(nodeToWrite);

            // read the attributes.
            StatusCodeCollection results = null;
            DiagnosticInfoCollection diagnosticInfos = null;

            ResponseHeader responseHeader = session.Write(
                null,
                nodesToWrite,
                out results,
                out diagnosticInfos);

            ClientBase.ValidateResponse(results, nodesToWrite);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToWrite);

            // check for error.
            if (StatusCode.IsBad(results[0]))
            {
                throw ServiceResultException.Create(results[0], 0, diagnosticInfos, responseHeader.StringTable);
            }
        }
    }
}
