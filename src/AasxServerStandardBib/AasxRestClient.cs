using AasxIntegrationBase;
using AdminShellNS;
using Grapevine;
using Grapevine.Client;
using Grapevine.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxRestServerLibrary
{
    public class AasxRestClient : IAasxOnlineConnection
    {
        // Instance management

        private Uri uri = null;
        private RestClient client = null;

        public AasxRestClient(string hostpart)
        {
            this.uri = new Uri(hostpart.TrimEnd('/'));
            this.client = new RestClient();
            this.client.Host = this.uri.Host;
            this.client.Port = this.uri.Port;
        }

        // interface

        public bool IsValid() { return this.uri != null ; } // assume validity
        public bool IsConnected() { return true; } // always, as there is no open connection by principle
        public string GetInfo() { return uri.ToString(); }

        public Stream GetThumbnailStream()
        {
            var request = new RestRequest("/aas/id/thumbnail");
            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.Ok)
                throw new Exception($"REST {response.ResponseUri} response {response.StatusCode} with {response.StatusDescription}");

            // Note: the normal response.GetContent() internally reads ContentStream as a string and screws up binary data.
            // Necessary to access the real implementing object
            var rr = response as RestResponse;
            if (rr != null)
            {
                return rr.Advanced.GetResponseStream();
            }
            return null;
        }

        // individual functions

        public AdminShell.PackageEnv OpenPackageByAasEnv()
        {
            var request = new RestRequest("/aas/id/aasenv");
            var respose = client.Execute(request);
            if (respose.ReturnedError)
                throw new Exception($"REST {respose.ResponseUri} response {respose.StatusCode} with {respose.StatusDescription}");
            var res = new AdminShell.PackageEnv();
            res.LoadFromAasEnvString(respose.GetContent());
            return res;
        }

        public string GetSubmodel(string name)
        {
            string fullname = "/aas/id/submodels/" + name + "/complete";
            var request = new RestRequest(fullname);
            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.Ok)
                throw new Exception($"REST {response.ResponseUri} response {response.StatusCode} with {response.StatusDescription}");
            return response.GetContent();
        }

        public string PutSubmodel(string payload)
        {
            string fullname = "/aas/id/submodels/";
            var request = new RestRequest(fullname);
            request.HttpMethod = HttpMethod.PUT;
            request.ContentType = ContentType.JSON;
            request.Payload = payload;
            request.Timeout = 30000;
            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.Ok)
                throw new Exception($"REST {response.ResponseUri} response {response.StatusCode} with {response.StatusDescription}");
            return response.GetContent();
        }
    }
}
