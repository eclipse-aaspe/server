/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/


using AasxIntegrationBase;
using AdminShellNS;
using Extensions;
using Grapevine.Client;
using System;
using System.IO;
using System.Net;

namespace AasxRestServerLibrary
{
    using System.Text.Json;

    public class AasxRestClient : IAasxOnlineConnection
    {
        // Instance management

        private Uri uri = null;
        private RestClient client = null;
        private WebProxy proxy = null;

        public AasxRestClient(string hostpart)
        {
            this.uri = new Uri(hostpart.TrimEnd('/'));
            this.client = new RestClient();
            this.client.Host = this.uri.Host;
            this.client.Port = this.uri.Port;
            if (System.IO.File.Exists("C:\\dat\\proxy.dat"))
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
            var request = new RestRequest("/aas/id/thumbnail");
            if (this.proxy != null)
                request.Proxy = this.proxy;
            var response = client.Execute(request);
            if (response.StatusCode != Grapevine.Shared.HttpStatusCode.Ok)
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

        public string ReloadPropertyValue()
        {
            return "";
        }

        // utilities

        string BuildUriQueryPartId(string tag, IIdentifiable entity)
        {
            if (entity == null || entity.Id == null)
                return "";
            var res = "";
            if (tag != null)
                res += tag.Trim() + "=";
            res += "" + entity.Id.Trim();
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
            var request = new RestRequest("/aas/id/aasenv");
            if (this.proxy != null)
                request.Proxy = this.proxy;
            var respose = client.Execute(request);
            if (respose.StatusCode != Grapevine.Shared.HttpStatusCode.Ok)
                throw new Exception($"REST {respose.ResponseUri} response {respose.StatusCode} with {respose.StatusDescription}");
            var res = new AdminShellPackageEnv();
            res.LoadFromAasEnvString(respose.GetContent());
            return res;
        }

        public string GetSubmodel(string name)
        {
            string fullname = "/aas/id/submodels/" + name + "/complete";
            var request = new RestRequest(fullname);
            if (this.proxy != null)
                request.Proxy = this.proxy;
            var response = client.Execute(request);
            if (response.StatusCode != Grapevine.Shared.HttpStatusCode.Ok)
                throw new Exception($"REST {response.ResponseUri} response {response.StatusCode} with {response.StatusDescription}");
            return response.GetContent();
        }

        public string PutSubmodel(string payload)
        {
            string fullname = "/aas/id/submodels/";
            var request = new RestRequest(fullname);
            request.HttpMethod = Grapevine.Shared.HttpMethod.PUT;
            request.ContentType = Grapevine.Shared.ContentType.JSON;
            request.Payload = payload;
            if (this.proxy != null)
                request.Proxy = this.proxy;
            var response = client.Execute(request);
            if (response.StatusCode != Grapevine.Shared.HttpStatusCode.Ok)
                throw new Exception($"REST {response.ResponseUri} response {response.StatusCode} with {response.StatusDescription}");
            return response.GetContent();
        }

        public string UpdatePropertyValue(AasCore.Aas3_0.Environment env, Submodel submodel, ISubmodelElement sme)
        {
            // trivial fails
            if (env == null || sme == null)
                return null;

            // need AAS, indirect
            var aas = env.FindAasWithSubmodelId(submodel.Id);
            if (aas == null)
                return null;

            // build path         
            var aasId = aas.IdShort;
            var submodelId = submodel.IdShort;
            var elementId = sme.CollectIdShortByParent();
            var reqpath = "./aas/" + aasId + "/submodels/" + submodelId + "/elements/" + elementId + "/property";

            // request
            var request = new RestRequest(reqpath);
            if (this.proxy != null)
                request.Proxy = this.proxy;
            var respose = client.Execute(request);
            if (respose.StatusCode != Grapevine.Shared.HttpStatusCode.Ok)
                throw new Exception($"REST {respose.ResponseUri} response {respose.StatusCode} with {respose.StatusDescription}");

            var json = respose.GetContent();
            var parsed = JsonDocument.Parse(json);
            var value = parsed.RootElement.GetProperty("value").GetString();
            return value;
        }
    }
}
