using AasxIntegrationBase;
using AdminShellNS;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;

namespace AasxRestServerLibrary
{
    public class AasxRestClient : IAasxOnlineConnection
    {
        // Instance management

        private Uri uri = null;
        private WebClient client = null;
        private WebProxy proxy = null;

        public AasxRestClient(string hostpart)
        {
            this.uri = new Uri(hostpart.TrimEnd('/'));
            this.client = new WebClient();
            this.client.BaseAddress = this.uri.ToString();
            if (File.Exists("C:\\dat\\proxy.dat"))
            {
                string proxyAddress = "";
                string username = "";
                string password = "";
                try
                {   // Open the text file using a stream reader.
                    using (StreamReader sr = new StreamReader("C:\\dat\\proxy.dat"))
                    {
                        proxyAddress = sr.ReadLine();
                        username = sr.ReadLine();
                        password = sr.ReadLine();
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("The file C:\\dat\\proxy.dat could not be read:");
                    Console.WriteLine(e.Message);
                }
                this.proxy = new WebProxy();
                Uri newUri = new Uri(proxyAddress);
                this.proxy.Address = newUri;
                this.proxy.Credentials = new NetworkCredential(username, password);
            }
            else
            {
                /*
                this.proxy = WebProxy.GetDefaultProxy();
                if (this.proxy != null)
                    this.proxy.UseDefaultCredentials = true;
                */
            }
        }

        // interface

        public bool IsValid() { return this.uri != null; } // assume validity
        public bool IsConnected() { return true; } // always, as there is no open connection by principle
        public string GetInfo() { return uri.ToString(); }

        public Stream GetThumbnailStream()
        {
            var request = "/aas/id/thumbnail";

            if (this.proxy != null)
                client.Proxy = this.proxy;

            return new MemoryStream(client.DownloadData(request));
        }

        public string ReloadPropertyValue()
        {
            return "";
        }

        // utilities

        string BuildUriQueryPartId(string tag, AdminShell.Identifiable entity)
        {
            if (entity == null || entity.identification == null)
                return "";
            var res = "";
            if (tag != null)
                res += tag.Trim() + "=";
            res += entity.identification.idType.Trim() + "," + entity.identification.id.Trim();
            return res;
        }

        string BuildUriQueryString(params string[] parts)
        {
            if (parts == null)
                return "";
            var res = "?";
            foreach (var p in parts)
            {
                if (res.Length > 1)
                    res += "&";
                res += p;
            }
            return res;
        }

        // individual functions

        public AdminShellPackageEnv OpenPackageByAasEnv()
        {
            var request = "/aas/id/aasenv";

            if (this.proxy != null)
                client.Proxy = this.proxy;

            var respose = client.DownloadString(request);

            var res = new AdminShellPackageEnv();
            res.LoadFromAasEnvString(respose);
            return res;
        }

        public string GetSubmodel(string name)
        {
            string fullname = "/aas/id/submodels/" + name + "/complete";

            if (this.proxy != null)
                client.Proxy = this.proxy;

            return client.DownloadString(fullname);
        }

        public string PutSubmodel(string payload)
        {
            string fullname = "/aas/id/submodels/";

            client.Headers[HttpRequestHeader.ContentType] = "application/json";

            if (this.proxy != null)
                client.Proxy = this.proxy;

            return client.UploadString(fullname, "PUT", payload);
        }

        public string UpdatePropertyValue(AdminShell.AdministrationShellEnv env, AdminShell.Submodel submodel, AdminShell.SubmodelElement sme)
        {
            // trivial fails
            if (env == null || sme == null)
                return null;

            // need AAS, indirect
            var aas = env.FindAASwithSubmodel(submodel.identification);
            if (aas == null)
                return null;

            // build path
            var aasId = aas.idShort;
            var submodelId = submodel.idShort;
            var elementId = sme.CollectIdShortByParent();
            var reqpath = "./aas/" + aasId + "/submodels/" + submodelId + "/elements/" + elementId + "/property";

            if (this.proxy != null)
                client.Proxy = this.proxy;

            var json = client.DownloadString(reqpath);

            var parsed = JObject.Parse(json);
            var value = parsed.SelectToken("value").Value<string>();
            return value;
        }
    }
}
