using AasxServer;
using AdminShellNS;
using Extensions;
using Grapevine.Interfaces.Server;
using Grapevine.Server;
using Grapevine.Server.Attributes;
using Grapevine.Shared;
using Jose;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HttpStatusCode = Grapevine.Shared.HttpStatusCode;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

/* Please notice: the API and REST routes implemented in this version of the source code are not specified and standardised by the
specification Details of the Administration Shell. The hereby stated approach is solely the opinion of its author(s). */

namespace AasxRestServerLibrary
{
    using System.Text.Json;
    using JsonConverter = System.Text.Json.Serialization.JsonConverter;
    using JsonSerializer = System.Text.Json.JsonSerializer;

    public class AasxHttpContextHelper
    {
        public static String SwitchToAASX = "";
        public static String DataPath = ".";

        public AdminShellPackageEnv[] Packages = null;

        public AasxHttpHandleStore IdRefHandleStore = new AasxHttpHandleStore();

        #region // Path helpers

        public bool PathEndsWith(string path, string tag)
        {
            return path.Trim().ToLower().TrimEnd(new[] {'/'}).EndsWith(tag);
        }

        public bool PathEndsWith(Grapevine.Interfaces.Server.IHttpContext context, string tag)
        {
            return PathEndsWith(context.Request.PathInfo, tag);
        }

        // see also: https://stackoverflow.com/questions/33619469/how-do-i-write-a-regular-expression-to-route-traffic-with-grapevine-when-my-requ

        public Match PathInfoRegexMatch(MethodBase methodWithRestRoute, string input)
        {
            if (methodWithRestRoute == null)
                return null;
            string piRegex = null;
            foreach (var attr in methodWithRestRoute.GetCustomAttributes<RestRoute>())
                if (attr.PathInfo != null)
                    piRegex = attr.PathInfo;
            if (piRegex == null)
                return null;
            var m = Regex.Match(input, piRegex);
            return m;
        }

        public List<AasxHttpHandleIdentification> CreateHandlesFromQueryString(System.Collections.Specialized.NameValueCollection queryStrings)
        {
            // start
            var res = new List<AasxHttpHandleIdentification>();
            if (queryStrings == null)
                return res;

            // over all query strings
            foreach (var kr in queryStrings.AllKeys)
            {
                try
                {
                    var k = kr.Trim().ToLower();
                    var v = queryStrings[k];
                    if (k.StartsWith("q") && k.Length > 1 && v.Contains(','))
                    {
                        var vl = v.Split(',');
                        if (vl.Length == 2)
                        {
                            //var id = new IIdentifiable(vl[1]);
                            var id = vl[1];
                            var h  = new AasxHttpHandleIdentification(id, "@" + k);
                            res.Add(h);
                        }
                    }
                }
                catch
                {
                }
            }

            // done
            return res;
        }

        public List<AasxHttpHandleIdentification> CreateHandlesFromRawUrl(string rawUrl)
        {
            // start
            var res = new List<AasxHttpHandleIdentification>();
            if (rawUrl == null)
                return res;

            // un-escape
            var url = System.Uri.UnescapeDataString(rawUrl);

            // split for query string traditional
            var i = url.IndexOf('?');
            if (i < 0 || i == url.Length - 1)
                return res;
            var query = url.Substring(i + 1);

            // try make a Regex wonder, again
            var m = Regex.Match(query, @"(\s*([^&]+)(&|))+");
            if (m.Success && m.Groups.Count >= 3 && m.Groups[2].Captures != null)
                foreach (var cp in m.Groups[2].Captures)
                {
                    var m2 = Regex.Match(cp.ToString(), @"\s*(\w+)\s*=\s*([^,]+),(.+)$");
                    if (m2.Success && m2.Groups.Count >= 4)
                    {
                        var k   = m2.Groups[1].ToString();
                        var idt = m2.Groups[2].ToString();
                        var ids = m2.Groups[3].ToString();

                        //var id = new IIdentifiable(ids);
                        var id = ids;
                        var h  = new AasxHttpHandleIdentification(id, "@" + k);
                        res.Add(h);
                    }
                }

            // done
            return res;
        }

        #endregion

        #region // Access package structures

        public FindAasReturn FindAAS(string aasid, System.Collections.Specialized.NameValueCollection queryStrings = null, string rawUrl = null)
        {
            FindAasReturn findAasReturn = new FindAasReturn();

            if (Packages == null)
                return null;

            if (Regex.IsMatch(aasid, @"^\d+$")) // only number, i.e. index
            {
                // Index
                int i = Convert.ToInt32(aasid);

                if (i > Packages.Length)
                    return null;

                if (Packages[i] == null || Packages[i].AasEnv == null || Packages[i].AasEnv.AssetAdministrationShells == null
                    || Packages[i].AasEnv.AssetAdministrationShells.Count < 1)
                    return null;

                findAasReturn.aas      = Packages[i].AasEnv.AssetAdministrationShells[0];
                findAasReturn.iPackage = i;
            }
            else
            {
                // Name
                if (aasid == "id")
                {
                    findAasReturn.aas      = Packages[0].AasEnv.AssetAdministrationShells[0];
                    findAasReturn.iPackage = 0;
                }
                else
                {
                    for (int i = 0; i < Packages.Length; i++)
                    {
                        if (Packages[i] != null)
                        {
                            if (Packages[i].AasEnv.AssetAdministrationShells[0].IdShort == aasid)
                            {
                                findAasReturn.aas      = Packages[i].AasEnv.AssetAdministrationShells[0];
                                findAasReturn.iPackage = i;
                                break;
                            }
                        }
                    }
                }
            }

            return findAasReturn;


            // trivial
            /*
            if (Packages[0] == null || Packages[0].AasEnv == null || Packages[0].AasEnv.AssetAdministrationShells == null || Packages[0].AasEnv.AssetAdministrationShells.Count < 1)
                return null;

            // default aas?
            if (aasid == null || aasid.Trim() == "" || aasid.Trim().ToLower() == "id")
                return Packages[0].AasEnv.AssetAdministrationShells[0];


            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(aasid, specialHandles);
            if (handleId != null && handleId.identification != null)
                return Packages[0].AasEnv.FindAAS(handleId.identification);

            // no, iterate over IdShort
            return Packages[0].AasEnv.FindAAS(aasid);
            */
        }

        public IReference FindSubmodelRefWithinAas(FindAasReturn findAasReturn, string smid, System.Collections.Specialized.NameValueCollection queryStrings = null,
                                                   string rawUrl = null)
        {
            // trivial
            if (Packages[findAasReturn.iPackage] == null || Packages[findAasReturn.iPackage].AasEnv == null || findAasReturn.aas == null || smid == null || smid.Trim() == "")
                return null;

            // via handle
            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId       = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(smid, specialHandles);

            // no, iterate & find
            foreach (var smref in findAasReturn.aas.Submodels)
            {
                if (handleId != null && handleId.identification != null)
                {
                    if (smref.Matches(handleId.identification))
                        return smref;
                }
                else
                {
                    var sm = this.Packages[findAasReturn.iPackage].AasEnv.FindSubmodel(smref);
                    if (sm != null && sm.IdShort != null && sm.IdShort.Trim().ToLower() == smid.Trim().ToLower())
                        return smref;
                }
            }

            // no
            return null;
        }

        public ISubmodel FindSubmodelWithinAas(FindAasReturn findAasReturn, string smid, System.Collections.Specialized.NameValueCollection queryStrings = null,
                                               string rawUrl = null)
        {
            // trivial
            if (Packages[findAasReturn.iPackage] == null || Packages[findAasReturn.iPackage].AasEnv == null || findAasReturn.aas == null || smid == null || smid.Trim() == "")
                return null;

            // via handle
            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId       = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(smid, specialHandles);
            if (handleId != null && handleId.identification != null)
                return Packages[findAasReturn.iPackage].AasEnv.FindSubmodelById(handleId.identification);

            // no, iterate & find

            foreach (var smref in findAasReturn.aas.Submodels)
            {
                var sm = this.Packages[findAasReturn.iPackage].AasEnv.FindSubmodel(smref);
                if (sm != null && sm.IdShort != null && sm.IdShort.Trim().ToLower() == smid.Trim().ToLower())
                    return sm;
            }

            // no
            return null;
        }


        public ISubmodel FindSubmodelWithinAas(string aasid, string smid, System.Collections.Specialized.NameValueCollection queryStrings = null, string rawUrl = null)
        {
            IAssetAdministrationShell aas      = null;
            int                       iPackage = -1;

            if (Packages == null)
                return null;

            if (Regex.IsMatch(aasid, @"^\d+$")) // only number, i.e. index
            {
                // Index
                int i = Convert.ToInt32(aasid);

                if (i > Packages.Length)
                    return null;

                if (Packages[i] == null || Packages[i].AasEnv == null || Packages[i].AasEnv.AssetAdministrationShells == null
                    || Packages[i].AasEnv.AssetAdministrationShells.Count < 1)
                    return null;

                aas      = Packages[i].AasEnv.AssetAdministrationShells[0];
                iPackage = i;
            }
            else
            {
                // Name
                if (aasid == "id")
                {
                    aas      = Packages[0].AasEnv.AssetAdministrationShells[0];
                    iPackage = 0;
                }
                else
                {
                    for (int i = 0; i < Packages.Length; i++)
                    {
                        if (Packages[i] != null)
                        {
                            if (Packages[i].AasEnv.AssetAdministrationShells[0].IdShort == aasid)
                            {
                                aas      = Packages[i].AasEnv.AssetAdministrationShells[0];
                                iPackage = i;
                                break;
                            }
                        }
                    }
                }
            }

            if (aas == null)
                return null;

            // no, iterate & find

            foreach (var smref in aas.Submodels)
            {
                var sm = this.Packages[iPackage].AasEnv.FindSubmodel(smref);
                if (sm != null && sm.IdShort != null && sm.IdShort.Trim().ToLower() == smid.Trim().ToLower())
                    return sm;
            }

            // no
            return null;
        }


        public ISubmodel FindSubmodelWithoutAas(string smid, System.Collections.Specialized.NameValueCollection queryStrings = null, string rawUrl = null)
        {
            // trivial
            if (Packages[0] == null || Packages[0].AasEnv == null || smid == null || smid.Trim() == "")
                return null;

            // via handle
            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId       = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(smid, specialHandles);
            if (handleId != null && handleId.identification != null)
                return Packages[0].AasEnv.FindSubmodelById(handleId.identification);

            // no, iterate & find
            foreach (var sm in this.Packages[0].AasEnv.Submodels)
            {
                if (sm != null && sm.IdShort != null && sm.IdShort.Trim().ToLower() == smid.Trim().ToLower())
                    return sm;
            }

            // no
            return null;
        }

        public IConceptDescription FindCdWithoutAas(FindAasReturn findAasReturn, string cdid, System.Collections.Specialized.NameValueCollection queryStrings = null,
                                                    string rawUrl = null)
        {
            // trivial
            if (Packages[findAasReturn.iPackage] == null || Packages[findAasReturn.iPackage].AasEnv == null || findAasReturn.aas == null || cdid == null || cdid.Trim() == "")
                return null;

            // via handle
            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId       = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(cdid, specialHandles);
            if (handleId != null && handleId.identification != null)
                return Packages[findAasReturn.iPackage].AasEnv.FindConceptDescriptionById(handleId.identification);

            // no, iterate & find
            foreach (var cd in Packages[findAasReturn.iPackage].AasEnv.ConceptDescriptions)
            {
                if (cd.IdShort != null && cd.IdShort.Trim().ToLower() == cdid.Trim().ToLower())
                    return cd;
            }

            // no
            return null;
        }


        public class FindSubmodelElementResult
        {
            public IReferable elem = null;
            public ISubmodelElement submodelElement = null;
            public IReferable parent = null;

            public FindSubmodelElementResult(IReferable elem = null, ISubmodelElement wrapper = null, IReferable parent = null)
            {
                this.elem            = elem;
                this.submodelElement = wrapper;
                this.parent          = parent;
            }
        }

        public FindSubmodelElementResult FindSubmodelElement(IReferable parent, List<ISubmodelElement> wrappers, string[] elemids, int elemNdx = 0)
        {
            // trivial
            if (wrappers == null || elemids == null || elemNdx >= elemids.Length)
                return null;

            // dive into each
            foreach (var smw in wrappers)
                if (smw != null)
                {
                    // IdShort need to match
                    if (smw.IdShort.Trim().ToLower() != elemids[elemNdx].Trim().ToLower())
                        continue;

                    // leaf
                    if (elemNdx == elemids.Length - 1)
                    {
                        return new FindSubmodelElementResult(elem: smw, wrapper: smw, parent: parent);
                    }
                    else
                    {
                        // recurse into?
                        var xsmc = smw as SubmodelElementCollection;
                        if (xsmc != null)
                        {
                            var r = FindSubmodelElement(xsmc, xsmc.Value, elemids, elemNdx + 1);
                            if (r != null)
                                return r;
                        }

                        var xop = smw as Operation;
                        if (xop != null)
                        {
                            //var w2 = new List<SubmodelElementWrapper>();
                            var w2 = new List<ISubmodelElement>();

                            //for (int i = 0; i < 2; i++)
                            //    foreach (var opv in xop[i])
                            //        if (opv.Value != null)
                            //            w2.Add(opv.Value);

                            foreach (var opv in xop.InputVariables)
                            {
                                if (opv.Value != null)
                                {
                                    w2.Add(opv.Value);
                                }
                            }

                            foreach (var opv in xop.OutputVariables)
                            {
                                if (opv.Value != null)
                                {
                                    w2.Add(opv.Value);
                                }
                            }

                            var r = FindSubmodelElement(xop, w2, elemids, elemNdx + 1);
                            if (r != null)
                                return r;
                        }
                    }
                }

            // nothing
            return null;
        }

        #endregion

        #region // Generate responses

        public static string makeJsonLD(string json, int count)
        {
            int    total          = json.Length;
            string header         = "";
            string jsonld         = "";
            string name           = "";
            int    state          = 0;
            int    identification = 0;
            string id             = "idNotFound";

            for (int i = 0; i < total; i++)
            {
                var c = json[i];
                switch (state)
                {
                    case 0:
                        if (c == '"')
                        {
                            state = 1;
                        }
                        else
                        {
                            jsonld += c;
                        }

                        break;
                    case 1:
                        if (c == '"')
                        {
                            state = 2;
                        }
                        else
                        {
                            name += c;
                        }

                        break;
                    case 2:
                        if (c == ':')
                        {
                            bool   skip    = false;
                            string pattern = ": null";
                            if (i + pattern.Length < total)
                            {
                                if (json.Substring(i, pattern.Length) == pattern)
                                {
                                    skip =  true;
                                    i    += pattern.Length;
                                    // remove last "," in jsonld if character after null is not ","
                                    int j = jsonld.Length - 1;
                                    while (Char.IsWhiteSpace(jsonld[j]))
                                    {
                                        j--;
                                    }

                                    if (jsonld[j] == ',' && json[i] != ',')
                                    {
                                        jsonld = jsonld.Substring(0, j) + "\r\n";
                                    }
                                    else
                                    {
                                        jsonld = jsonld.Substring(0, j + 1) + "\r\n";
                                    }

                                    while (json[i] != '\n')
                                        i++;
                                }
                            }

                            if (!skip)
                            {
                                if (name == "identification")
                                    identification++;
                                if (name == "id" && identification == 1)
                                {
                                    id = "";
                                    int j = i;
                                    while (j < json.Length && json[j] != '"')
                                    {
                                        j++;
                                    }

                                    j++;
                                    while (j < json.Length && json[j] != '"')
                                    {
                                        id += json[j];
                                        j++;
                                    }
                                }

                                count++;
                                name += "__" + count;
                                if (header != "")
                                    header += ",\r\n";
                                header += "  \"" + name + "\": " + "\"aio:" + name + "\"";
                                jsonld += "\"" + name + "\":";
                            }
                        }
                        else
                        {
                            jsonld += "\"" + name + "\"" + c;
                        }

                        state = 0;
                        name  = "";
                        break;
                }
            }

            string prefix = "  \"aio\": \"https://admin-shell-io.com/ns#\",\r\n";
            prefix += "  \"I40GenericCredential\": \"aio:I40GenericCredential\",\r\n";
            prefix += "  \"__AAS\": \"aio:__AAS\",\r\n";
            header =  prefix + header;
            header =  "\"context\": {\r\n" + header + "\r\n},\r\n";
            int k = jsonld.Length - 2;
            while (k >= 0 && jsonld[k] != '}' && jsonld[k] != ']')
            {
                k--;
            }
#pragma warning disable format
            jsonld =  jsonld.Substring(0, k + 1);
            jsonld += ",\r\n" + "  \"id\": \"" + id + "\"\r\n}\r\n";
            jsonld =  "\"doc\": " + jsonld;
            jsonld =  "{\r\n\r\n" + header + jsonld + "\r\n\r\n}\r\n";
#pragma warning restore format

            return jsonld;
        }

        private static JsonSerializerOptions _jsonSerializerOptions = new() {WriteIndented = true};

        public static async Task SendJsonResponse(IHttpContext context, object obj, JsonConverter contractResolver = null)
        {
            var    queryString = context.Request.QueryString;
            string refresh     = queryString["refresh"];
            if (!string.IsNullOrEmpty(refresh))
            {
                context.Response.Headers.Remove("Refresh");
                context.Response.Headers.Add("Refresh", refresh);
            }

            string jsonld = queryString["jsonld"];
            string vc     = queryString["vc"];

            if (contractResolver != null)
            {
                _jsonSerializerOptions.Converters.Add(contractResolver);
            }

            var json = JsonSerializer.Serialize(obj, _jsonSerializerOptions);

            if (jsonld != null || vc != null)
            {
                jsonld = makeJsonLD(json, 0);
                json   = jsonld;

                if (!string.IsNullOrEmpty(vc) && !string.IsNullOrEmpty(jsonld))
                {
                    string requestPath = "https://nameplate.h2894164.stratoserver.net/demo/sign?create_as_verifiable_presentation=false";

                    var handler = new HttpClientHandler();

                    if (AasxServer.AasxTask.proxy != null)
                        handler.Proxy = AasxServer.AasxTask.proxy;
                    else
                        handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;

                    using var client = new HttpClient(handler);
                    client.Timeout = TimeSpan.FromSeconds(60);

                    try
                    {
                        var content  = new StringContent(jsonld, Encoding.UTF8, "application/json");
                        var response = await client.PostAsync(requestPath, content);

                        if (response.IsSuccessStatusCode)
                        {
                            json = await response.Content.ReadAsStringAsync();
                        }
                        else
                        {
                            json = $"ERROR POST; {response.StatusCode}; {requestPath}; {await response.Content.ReadAsStringAsync()}";
                            Console.WriteLine(json);
                        }
                    }
                    catch (Exception ex)
                    {
                        json = $"ERROR POST; {ex.Message}";
                        Console.WriteLine(json);
                    }
                }
            }

            // TODO (jtikekar, 2023-09-04):Remove
            if (context.Request.RawUrl.Equals("/aas/0/core") && obj is ExpandoObject findAasReturn)
            {
                var value = new JsonObject();
                foreach (var kvp in findAasReturn)
                {
                    if (kvp.Key.Equals("AAS"))
                    {
                        value["AAS"] = Jsonization.Serialize.ToJsonObject((AssetAdministrationShell)kvp.Value);
                    }
                    else if (kvp.Key.Equals("Asset"))
                    {
                        value["AssetInformation"] = Jsonization.Serialize.ToJsonObject((AssetInformation)kvp.Value);
                    }
                }

                json = value.ToString();
            }

            var buffer = Encoding.UTF8.GetBytes(json);
            var length = buffer.Length;

            // Assuming allowCORS is a method that handles CORS headers
            AasxRestServer.TestResource.allowCORS(context);

            context.Response.ContentType     = ContentType.JSON;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = length;
            context.Response.SendResponse(buffer);
        }

        protected static void SendTextResponse(Grapevine.Interfaces.Server.IHttpContext context, string txt, string mimeType = null)
        {
            var    queryString = context.Request.QueryString;
            string refresh     = queryString["refresh"];
            if (refresh != null && refresh != "")
            {
                context.Response.Headers.Remove("Refresh");
                context.Response.Headers.Add("Refresh", refresh);
            }

            AasxRestServer.TestResource.allowCORS(context);

            context.Response.ContentType = ContentType.TEXT;
            if (mimeType != null)
                context.Response.Advanced.ContentType = mimeType;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = txt.Length;
            context.Response.SendResponse(txt);
        }

        protected static void SendStreamResponse(IHttpContext context, Stream stream,
                                                 string headerAttachmentFileName = null)
        {
            AasxRestServer.TestResource.allowCORS(context);

            context.Response.ContentType = ContentType.APPLICATION;
            //// context.Response.SendChunked = true;
            context.Response.ContentLength64 = stream.Length;

            if (headerAttachmentFileName != null)
                context.Response.AddHeader("Content-Disposition", $"attachment; filename={headerAttachmentFileName}");

            stream.CopyTo(context.Response.Advanced.OutputStream);
            context.Response.Advanced.Close();
        }

        protected static void SendRedirectResponse(Grapevine.Interfaces.Server.IHttpContext context, string redirectUrl)
        {
            AasxRestServer.TestResource.allowCORS(context);

            context.Response.AppendHeader("redirectInfo", "URL");
            context.Response.Redirect(redirectUrl);
            context.Response.SendResponse(HttpStatusCode.TemporaryRedirect, redirectUrl);
        }

        #endregion

        #region AAS and Asset

        public void EvalGetAasAndAsset(IHttpContext context, string aasid, bool deep = false, bool complete = false)
        {
            dynamic res = new ExpandoObject();
            // access the first AAS
            var findAasReturn = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (findAasReturn.aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with id '{aasid}' found.");
                return;
            }

            // try to get the asset as well
            // TODO (MIHO, 2022-01-07): decide what to do with the frame
            AssetInformation asset = null;

            // result
            res.AAS   = findAasReturn.aas;
            res.Asset = findAasReturn.aas.AssetInformation;

            // return as JSON
            var cr = new AdminShellConverters.AdaptiveFilterContractResolver(deep, complete);
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, res, cr);
        }

        public void EvalGetAasEnv(IHttpContext context, string aasid)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/aasenv", "READ"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            if (this.Packages[0] == null || this.Packages[0].AasEnv == null)
            {
                context.Response.SendResponse(HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }

            // access the first AAS
            var findAasReturn = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (findAasReturn.aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with id '{aasid}' found.");
                return;
            }

            // create a new, filtered AasEnv
            AasCore.Aas3_0.Environment copyenv = new AasCore.Aas3_0.Environment();
            try
            {
                var sourceEnvironment = Packages[findAasReturn.iPackage].AasEnv;
                var aasList           = new List<IAssetAdministrationShell>() {findAasReturn.aas};
                copyenv = copyenv.CreateFromExistingEnvironment(sourceEnvironment, aasList);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.BadRequest, $"Cannot filter aas envioronment: {ex.Message}.");
                return;
            }

            try
            {
                if (PathEndsWith(context, "aasenv"))
                {
                    // return as FILE
                    using (var ms = new MemoryStream())
                    {
                        // build a file name
                        var fn = "aasenv.json";
                        if (findAasReturn.aas.IdShort != null)
                            fn = findAasReturn.aas.IdShort + "." + fn;
                        // serialize via helper
                        var jsonwriter = copyenv.SerializeJsonToStream(new StreamWriter(ms), leaveJsonWriterOpen: true);
                        // write out again
                        ms.Position = 0;
                        SendStreamResponse(context, ms, Path.GetFileName(fn));
                        // a bit ugly
                        jsonwriter.Flush();
                    }
                }

                if (PathEndsWith(context, "aasenvjson"))
                {
                    // result
                    res.env = copyenv;

                    // return as JSON
                    var cr = new AdminShellConverters.AdaptiveFilterContractResolver();
                    SendJsonResponse(context, res, cr);
                }
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot serialize and send aas envioronment: {ex.Message}.");
                return;
            }


            context.Response.StatusCode = HttpStatusCode.Ok;
        }


        public void EvalGetAasThumbnail(IHttpContext context, string aasid)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/thumbnail", "READ"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            if (this.Packages[0] == null)
            {
                context.Response.SendResponse(HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }

            // access the first AAS
            var findAasReturn = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (findAasReturn.aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with id '{aasid}' found.");
                return;
            }

            // access the thumbnail
            // Note: in this version, the thumbnail is not specific to the AAS, but maybe in later versions ;-)
            Uri thumbUri    = null;
            var thumbStream = this.Packages[findAasReturn.iPackage].GetLocalThumbnailStream(ref thumbUri);
            if (thumbStream == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No thumbnail available in package.");
                return;
            }

            // return as FILE
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendStreamResponse(context, thumbStream, Path.GetFileName(thumbUri.ToString() ?? ""));
            thumbStream.Close();
        }

        public void EvalPutAas(IHttpContext context)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/aas", "UPDATE"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                return;
            }

            // list of Identification
            AssetAdministrationShell aas = null;
            try
            {
                aas = System.Text.Json.JsonSerializer.Deserialize<AssetAdministrationShell>(context.Request.Payload);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                return;
            }

            // need id for idempotent behaviour
            if (aas.Id == null)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Identification of entity is (null); PUT cannot be performed.");
                return;
            }

            // datastructure update
            context.Server.Logger.Debug($"Putting AdministrationShell with IdShort {aas.IdShort ?? "--"} and id {aas.Id?.ToString() ?? "--"}");

            bool emptyPackageAvailable = false;
            int  emptyPackageIndex     = -1;
            for (int envi = 0; envi < this.Packages.Length; envi++)
            {
                if (this.Packages[envi] != null)
                {
                    var existingAas = this.Packages[envi].AasEnv.FindAasById(aas.Id);
                    if (existingAas != null)
                    {
                        this.Packages[envi].AasEnv.AssetAdministrationShells.Remove(existingAas);
                        this.Packages[envi].AasEnv.AssetAdministrationShells.Add(aas);
                        SendTextResponse(context, "OK (update, index=" + envi + ")");
                        return;
                    }
                }
                else
                {
                    if (!emptyPackageAvailable)
                    {
                        emptyPackageAvailable = true;
                        emptyPackageIndex     = envi;
                    }
                }
            }

            if (emptyPackageAvailable)
            {
                this.Packages[emptyPackageIndex] = new AdminShellPackageEnv();
                this.Packages[emptyPackageIndex].AasEnv.AssetAdministrationShells.Add(aas);
                SendTextResponse(context, "OK (new, index=" + emptyPackageIndex + ")");
                return;
            }

            context.Response.StatusCode = HttpStatusCode.Ok;
            SendTextResponse(context, "Error: not added since datastructure completely filled already");
        }

        public void EvalPutAasxOnServer(IHttpContext context)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/aasx", "UPDATE"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            Console.WriteLine("EvalPutAasxOnServer: " + context.Request.Payload);
            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                return;
            }

            AasxFileInfo file = System.Text.Json.JsonSerializer.Deserialize<AasxFileInfo>(context.Request.Payload);
            if (!file.path.ToLower().EndsWith(".aasx"))
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Not a path ending with \".aasx\"...:{file.path}. Aborting...");
                return;
            }

            AdminShellPackageEnv aasEnv = null;
            try
            {
                aasEnv = new AdminShellPackageEnv(file.path, true);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot open {file.path}. Aborting... {ex.Message}");
                return;
            }

            if (file.instantiateTemplate)
            {
                if (file.instancesIdentificationSuffix == null)
                {
                    context.Response.SendResponse(HttpStatusCode.BadRequest, $"Received no identification suffix. Aborting...");
                    return;
                }
                else
                {
                    Console.WriteLine("EvalPutAasxOnServer: file.instancesIdentificationSuffix = " + file.instancesIdentificationSuffix);

                    // instantiate aas
                    foreach (var aas in aasEnv.AasEnv.AssetAdministrationShells)
                    {
                        aas.IdShort                        += file.instancesIdentificationSuffix;
                        aas.Id                             += file.instancesIdentificationSuffix;
                        aas.AssetInformation.GlobalAssetId =  file.instancesIdentificationSuffix;
                        foreach (var smref in aas.Submodels)
                        {
                            foreach (var key in smref.Keys)
                            {
                                key.Value += file.instancesIdentificationSuffix;
                            }
                        }
                    }

                    // instantiate asset
                    //foreach (var asset in aasEnv.AasEnv.Assets)
                    //{
                    //    //asset.IdShort += file.instancesIdentificationSuffix;
                    //    //asset.identification.id += file.instancesIdentificationSuffix;
                    //    //asset.SetIdentification(new AdminShellV30.Identifier(file.instancesIdentificationSuffix));
                    //    var assetIdKey = new Key(KeyTypes.GlobalReference, file.instancesIdentificationSuffix);
                    //    var keyList = new List<Key>() { assetIdKey };
                    //    asset.GlobalAssetId = new Reference(ReferenceTypes.GlobalReference, keyList);
                    //}

                    // instantiate submodel
                    foreach (var submodel in aasEnv.AasEnv.Submodels)
                    {
                        submodel.Id += file.instancesIdentificationSuffix;
                        if (file.instantiateSubmodelsIdShort)
                        {
                            submodel.IdShort += file.instancesIdentificationSuffix;
                        }
                    }
                }
            }

            string aasIdShort = "";
            try
            {
                aasIdShort = aasEnv.AasEnv.AssetAdministrationShells[0].IdShort;
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot find IdShort in {file.path}. Aborting... {ex.Message}");
                return;
            }

            var findAasReturn = this.FindAAS(aasIdShort, context.Request.QueryString, context.Request.RawUrl);
            Console.WriteLine("FindAAS() with IdShort \"" + aasIdShort + "\" yields package-index " + findAasReturn.iPackage);

            if (findAasReturn.aas == null)
            {
                for (int envi = 0; envi < this.Packages.Length; envi++)
                {
                    if (this.Packages[envi] == null)
                    {
                        this.Packages[envi]         = aasEnv;
                        Program.envFileName[envi]   = file.path;
                        context.Response.StatusCode = HttpStatusCode.Ok;
                        SendTextResponse(context, "OK (new, index=" + envi + ")");
                        return;
                    }
                }

                context.Response.StatusCode = HttpStatusCode.NotFound;
                SendTextResponse(context, "Failed: Server used to capacity.");
                return;
            }
            else
            {
                Packages[findAasReturn.iPackage]            = aasEnv;
                Program.envFileName[findAasReturn.iPackage] = file.path;
                context.Response.StatusCode                 = HttpStatusCode.Ok;
                SendTextResponse(context, "OK (update, index=" + findAasReturn.iPackage + ")");
                return;
            }
        }

        public void EvalPutAasxToFilesystem(IHttpContext context, string aasid)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/aasx", "UPDATE"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            Console.WriteLine("EvalPutAasxToFilesystem");
            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                return;
            }

            AasxFileInfo file = System.Text.Json.JsonSerializer.Deserialize<AasxFileInfo>(context.Request.Payload);
            Console.WriteLine("EvalPutAasxToFilesystem: " + JsonSerializer.Serialize(file.path, new JsonSerializerOptions { WriteIndented = true }));
            if (!file.path.ToLower().EndsWith(".aasx"))
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Not a path ending with \".aasx\"...:{file.path}. Aborting...");
                return;
            }

            var findAasReturn = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            Console.WriteLine("FindAAS() with IdShort \"" + aasid + "\" yields package-index " + findAasReturn.iPackage);

            if (findAasReturn.aas == null)
            {
                context.Response.StatusCode = HttpStatusCode.NotFound;
                SendTextResponse(context, "Failed: AAS not found.");
                return;
            }
            else
            {
                try
                {
                    Packages[findAasReturn.iPackage].SaveAs(file.path, false, AdminShellPackageEnv.SerializationFormat.Json, null);
                    Program.envFileName[findAasReturn.iPackage] = file.path;
                    context.Response.StatusCode                 = HttpStatusCode.Ok;
                    SendTextResponse(context, "OK (saved)");
                    return;
                }
                catch (Exception ex)
                {
                    context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot save in {file.path}. Aborting... {ex.Message}");
                    return;
                }
            }
        }

        public void EvalPutAasxReplacePackage(IHttpContext context, string aasid)
        {
            dynamic res          = new ExpandoObject();
            int     index        = -1;
            string  accessrights = null;

            var aasInfo = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);

            // check authentication
            if (withAuthentification)
            {
                accessrights = SecurityCheck(context, ref index);

                var aas = Program.env[aasInfo.iPackage].AasEnv.AssetAdministrationShells[0];
                if (!checkAccessRights(context, accessrights, "/aasx", "UPDATE", "", "aas", aas))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // first check
            if (context.Request.Payload == null || context.Request.ContentLength64 < 1)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"No payload for replace AASX.");
                return;
            }

            // find package index to replace
            Console.WriteLine("FindAAS() with IdShort \"" + aasid + "\" yields package-index " + aasInfo.iPackage);
            var packIndex = aasInfo.iPackage;
            if (packIndex < 0 || packIndex >= Packages.Length)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"AASX package to be replaced not found. Aborting!");
                return;
            }

            /*
            if (withAuthentification)
            {
                string IdShort = AasxServer.Program.env[packIndex].AasEnv.AssetAdministrationShells[0].IdShort;
                string aasRights = "NONE";
                if (securityRightsAAS.Count != 0)
                    aasRights = securityRightsAAS[IdShort];
                if (!checkAccessRights(context, accessrights, aasRights))
                {
                    return;
                }
            }
            */

            var packFn = Packages[packIndex].Filename;
            Console.WriteLine($"Will replace AASX package on server: {packFn}");

            // make temp file
            var tempFn = System.IO.Path.GetTempFileName().Replace(".tmp", ".aasx");
            try
            {
                var ba = Convert.FromBase64String(context.Request.Payload);
                System.IO.File.WriteAllBytes(tempFn, ba);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot save AASX temporarily in {tempFn}. Aborting... {ex.Message}");
                return;
            }

            lock (Program.changeAasxFile)
            {
                // close old and renamed
                try
                {
                    // free to overwrite
                    Packages[packIndex].Close();

                    // copy to back (rename experienced to be more error-prone)
                    System.IO.File.Copy(packFn, packFn + ".bak", overwrite: true);
                }
                catch (Exception ex)
                {
                    context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot close/ backup old AASX {packFn}. Aborting... {ex.Message}");
                    return;
                }

                // replace exactly the file
                try
                {
                    // replace loaded original when saving
                    packFn = Program.envFileName[packIndex];
                    Console.WriteLine($"Replace original AASX package on server: {packFn}");

                    // copy into same location
                    System.IO.File.Copy(tempFn, packFn, overwrite: true);

                    // open again
                    var newAasx = new AdminShellPackageEnv(packFn, true);
                    if (newAasx != null)
                        Packages[packIndex] = newAasx;
                    else
                    {
                        context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot load new package {packFn} for replacing via PUT. Aborting.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot replace AASX {packFn} with new {tempFn}. Aborting... {ex.Message}");
                    return;
                }

                if (withAuthentification)
                {
                    securityInit();
                }
            }

            Program.signalNewData(2);
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendTextResponse(context, "OK (saved)");
        }

        public void EvalGetAasxByAssetId(IHttpContext context)
        {
            string   path    = context.Request.PathInfo;
            string[] split   = path.Split('/');
            string   node    = split[2];
            string   assetId = split[3].ToUpper();

            for (int envi = 0; envi < Packages.Length; envi++)
            {
                if (this.Packages[envi] != null)
                {
                    foreach (var aas in this.Packages[envi].AasEnv.AssetAdministrationShells)
                    {
                        if (aas.AssetInformation != null)
                        {
                            object asset = null;
                            //var asset = Program.env[envi].AasEnv.FindAsset(aas.assetInformation);
                            if (asset != null)
                            {
                                string url = System.Net.WebUtility.UrlEncode("").ToUpper();
                                if (assetId == url)
                                {
                                    string headers = context.Request.Headers.ToString();
                                    string token   = context.Request.Headers.Get("accept");
                                    if (token == null || token != "application/aas")
                                    {
                                        // Human by browser
                                        string text = "";

                                        text += "<strong>" + "This is the human readable page for your asset" + "</strong><br><br>";

                                        text += "AssetID = " + System.Net.WebUtility.UrlDecode(assetId) + "<br><br>";

                                        lock (Program.changeAasxFile)
                                        {
                                            string           detailsImage = "";
                                            System.IO.Stream s            = null;
                                            try
                                            {
                                                s = Program.env[envi].GetLocalThumbnailStream();
                                            }
                                            catch
                                            {
                                            }

                                            if (s != null)
                                            {
                                                using (var m = new System.IO.MemoryStream())
                                                {
                                                    s.CopyTo(m);
                                                    detailsImage = System.Convert.ToBase64String(m.ToArray());
                                                }

                                                if (detailsImage != "")
                                                {
                                                    text += "<br>" +
                                                            "Your product image:" +
                                                            "<div><img src=data:image;base64," +
                                                            detailsImage +
                                                            " style=\"max-width: 25%;\" alt=\"Details Image\" /></div>";
                                                }
                                            }
                                        }

                                        text += "<br>";

                                        // var link = "http://" + Program.hostPort + "/server/getaasxbyassetid/" + assetId;
                                        var link = Program.externalRest + "/server/getaasxbyassetid/" + assetId;

                                        text += "Please open AAS in AASX Package Explorer by: File / Other Connect Options / Connect via REST:<br>" +
                                                "<a href= \"" + link + "\" target=\"_blank\">" +
                                                link + "</a>" + "<br><br>";

                                        text += "Please use Postman to get raw data:<br>GET " +
                                                "<a href= \"" + link + "\" target=\"_blank\">" +
                                                link + "</a>" + "<br>" +
                                                "and set Headers / Accept application/aas" + "<br><br>";

                                        context.Response.ContentType     = ContentType.HTML;
                                        context.Response.ContentEncoding = System.Text.Encoding.UTF8;
                                        context.Response.SendResponse(text);
                                        return;
                                    }

                                    EvalGetAASX(context, envi);
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with assetId '{assetId}' found.");
        }

        public void EvalDeleteAasAndAsset(IHttpContext context, string aasid, bool deleteAsset = false)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/aas", "DELETE"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // datastructure update
            if (this.Packages[0] == null || this.Packages[0].AasEnv == null || this.Packages[0].AasEnv.AssetAdministrationShells == null)
            {
                context.Response.SendResponse(HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }

            // access the AAS
            var findAasReturn = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (findAasReturn.aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with IdShort '{aasid}' found.");
                return;
            }

            // find the asset
            //var asset = this.Packages[findAasReturn.iPackage].AasEnv.FindAsset(findAasReturn.aas.assetInformation);
            var asset = findAasReturn.aas.AssetInformation;

            // delete
            context.Server.Logger.Debug($"Deleting AdministrationShell with IdShort {findAasReturn.aas.IdShort ?? "--"} and id {findAasReturn.aas.Id?.ToString() ?? "--"}");
            this.Packages[findAasReturn.iPackage].AasEnv.AssetAdministrationShells.Remove(findAasReturn.aas);

            if (this.Packages[findAasReturn.iPackage].AasEnv.AssetAdministrationShells.Count == 0)
            {
                this.Packages[findAasReturn.iPackage] = null;
            }
            else
            {
                if (deleteAsset && asset != null)
                {
                    context.Server.Logger.Debug($"Deleting Asset with Global Asset Id {asset.GlobalAssetId ?? "--"}");
                    //this.Packages[findAasReturn.iPackage].AasEnv.Assets.Remove(asset);
                }
            }

            // simple OK
            Program.signalNewData(2);
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendTextResponse(context, "OK");
        }

        #endregion

        #region // Asset links

        public void EvalGetAssetLinks(IHttpContext context, string assetid)
        {
            dynamic res1  = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/aas", "READ"))
                {
                    return;
                }

                res1.confirm = "Authorization = " + accessrights;
            }

            // trivial
            if (assetid == null)
            {
                context.Response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            // do a manual search
            var res            = new List<ExpandoObject>();
            var specialHandles = this.CreateHandlesFromQueryString(context.Request.QueryString);
            var handle         = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(assetid, specialHandles);
            if (handle != null && handle.identification != null)
            {
                foreach (var aas in this.Packages[0].AasEnv.AssetAdministrationShells)
                    if (aas.AssetInformation != null && (aas.AssetInformation.GlobalAssetId.Equals(handle.identification)))
                    {
                        dynamic o = new ExpandoObject();
                        o.identification = aas.Id;
                        o.IdShort        = aas.IdShort;
                        res.Add(o);
                    }
            }
            else
            {
                foreach (var aas in this.Packages[0].AasEnv.AssetAdministrationShells)
                    if (aas.IdShort != null && aas.IdShort.Trim() != "" && aas.IdShort.Trim().ToLower() == assetid.Trim().ToLower())
                    {
                        dynamic o = new ExpandoObject();
                        o.identification = aas.Id;
                        o.IdShort        = aas.IdShort;
                        res.Add(o);
                    }
            }

            // return as JSON
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, res);
        }

        public void EvalPutAsset(IHttpContext context)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/aas", "UPDATE"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                return;
            }

            // de-serialize asset
            AssetInformation asset = null;
            try
            {
                asset = System.Text.Json.JsonSerializer.Deserialize<AssetInformation>(context.Request.Payload);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                return;
            }

            //// need id for idempotent behaviour
            //if (asset.identification == null)
            //{
            //    context.Response.SendResponse(HttpStatusCode.BadRequest, $"Identification of entity is (null); PUT cannot be performed.");
            //    return;
            //}

            //// datastructure update
            //if (this.Packages[0] == null || this.Packages[0].AasEnv == null || this.Packages[0].AasEnv.Assets == null)
            //{
            //    context.Response.SendResponse(HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
            //    return;
            //}
            //context.Server.Logger.Debug($"Adding Asset with IdShort {asset.IdShort ?? "--"}");
            //var existingAsset = this.Packages[0].AasEnv.FindAsset(asset.identification);
            //if (existingAsset != null)
            //    this.Packages[0].AasEnv.Assets.Remove(existingAsset);
            //this.Packages[0].AasEnv.Assets.Add(asset);

            // simple OK
            Program.signalNewData(2);
            context.Response.StatusCode = HttpStatusCode.Ok;
            object existingAsset = null;
            SendTextResponse(context, "OK" + ((existingAsset != null) ? " (updated)" : " (new)"));
        }

        public void EvalPutAssetToAas(IHttpContext context, string aasid)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/aas", "UPDATE"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                Console.WriteLine("ERROR PUT: No payload or content type is not JSON.");
                return;
            }

            // access the AAS
            var findAasReturn = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (findAasReturn.aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with IdShort '{aasid}' found.");
                Console.WriteLine("ERROR PUT: No AAS with IdShort '{0}' found.", aasid);
                return;
            }
            
            // simple OK
            Program.signalNewData(2);
            context.Response.StatusCode = HttpStatusCode.Ok;
            object existingAsset = null;
            SendTextResponse(context, "OK" + ((existingAsset != null) ? " (updated)" : " (new)"));
        }

        #endregion

        #region // List of Submodels

        public class GetSubmodelsItem
        {
            public IIdentifiable id;
            public string IdShort = "";
            public string kind = "";

            public GetSubmodelsItem()
            {
            }

            public GetSubmodelsItem(IIdentifiable id, string IdShort, string kind)
            {
                this.id      = id;
                this.IdShort = IdShort;
                this.kind    = kind;
            }

            public GetSubmodelsItem(IIdentifiable idi, string kind)
            {
                this.id      = idi;
                this.IdShort = idi.IdShort;
                this.kind    = kind;
            }
        }

        public void EvalGetSubmodels(IHttpContext context, string aasid)
        {
            dynamic res1  = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/submodels", "READ"))
                {
                    return;
                }

                res1.confirm = "Authorization = " + accessrights;
            }

            // access the AAS
            var findAasReturn = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (findAasReturn.aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with IdShort '{aasid}' found.");
                return;
            }

            // build a list of results
            var res = new List<GetSubmodelsItem>();

            // get all submodels
            foreach (var smref in findAasReturn.aas.Submodels)
            {
                var sm = this.Packages[findAasReturn.iPackage].AasEnv.FindSubmodel(smref);
                if (sm != null)
                {
                    //res.Add(new GetSubmodelsItem(sm, sm.kind.kind));
                    res.Add(new GetSubmodelsItem(sm, sm.Kind.ToString()));
                }
            }

            // return as JSON
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, res);
        }

        static long countPut = 0;

        public void EvalPutSubmodel(IHttpContext context, string aasid)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/submodels", "UPDATE"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                Console.WriteLine("ERROR PUT: No payload or content type is not JSON.");
                return;
            }

            // access the AAS
            var findAasReturn = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (findAasReturn.aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with IdShort '{aasid}' found.");
                Console.WriteLine("ERROR PUT: No AAS with IdShort '{0}' found.", aasid);
                return;
            }

            // de-serialize Submodel
            Submodel submodel = null;
            try
            {
                using var reader = new StringReader(context.Request.Payload);
                _jsonSerializerOptions.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));

                // Deserialize the JSON directly into a Submodel object
                submodel = JsonSerializer.Deserialize<Submodel>(reader.ReadToEnd(), _jsonSerializerOptions);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                Console.WriteLine("ERROR PUT: Cannot deserialize payload.");
                return;
            }

            // need id for idempotent behaviour
            if (submodel.Id == null)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Identification of entity is (null); PUT cannot be performed.");
                Console.WriteLine("ERROR PUT: Identification of entity is (null); PUT cannot be performed.");
                return;
            }

            // datastructure update
            if (this.Packages[findAasReturn.iPackage] == null ||
                this.Packages[findAasReturn.iPackage].AasEnv == null /*|| this.Packages[findAasReturn.iPackage].AasEnv.Assets == null*/)
            {
                context.Response.SendResponse(HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }

            // add Submodel
            context.Server.Logger.Debug($"Adding Submodel with IdShort {submodel.IdShort ?? "--"} and id {submodel.Id?.ToString() ?? "--"}");
            var existingSm = this.Packages[findAasReturn.iPackage].AasEnv.FindSubmodelById(submodel.Id);
            if (existingSm != null)
            {
                int indexOfExistingSm = this.Packages[findAasReturn.iPackage].AasEnv.Submodels.IndexOf(existingSm);
                this.Packages[findAasReturn.iPackage].AasEnv.Submodels.RemoveAt(indexOfExistingSm);
                this.Packages[findAasReturn.iPackage].AasEnv.Submodels.Insert(indexOfExistingSm, submodel);
            }
            else
            {
                this.Packages[findAasReturn.iPackage].AasEnv.Submodels.Add(submodel);
            }

            // add SubmodelRef to AAS
            var       key     = new Key(KeyTypes.Submodel, submodel.Id);
            var       KeyList = new List<IKey>() {key};
            Reference newsmr  = new Reference(AasCore.Aas3_0.ReferenceTypes.ModelReference, KeyList);
            //var newsmr = SubmodelRef.CreateNew("Submodel", submodel.Id);
            var existsmr = findAasReturn.aas.HasSubmodelReference(newsmr);
            if (!existsmr)
            {
                context.Server.Logger.Debug($"Adding SubmodelRef to AAS with IdShort {findAasReturn.aas.IdShort ?? "--"} and id {findAasReturn.aas.Id?.ToString() ?? "--"}");
                findAasReturn.aas.Submodels.Add(newsmr);
            }

            Console.WriteLine("{0} Received PUT Submodel {1}", countPut++, submodel.IdShort);

            // simple OK
            Program.signalNewData(2);
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendTextResponse(context, "OK" + ((existingSm != null) ? " (updated)" : " (new)"));
        }

        public void EvalDeleteSubmodel(IHttpContext context, string aasid, string smid)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/submodels", "DELETE"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                return;
            }

            // access the AAS (absolutely mandatory)
            var findAasReturn = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (findAasReturn.aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with IdShort '{aasid}' found.");
                return;
            }

            // delete SubmodelRef 1st
            var smref = this.FindSubmodelRefWithinAas(findAasReturn, smid, context.Request.QueryString, context.Request.RawUrl);
            if (smref != null)
            {
                context.Server.Logger.Debug(
                                            $"Removing SubmodelRef {smid} from AAS with IdShort {findAasReturn.aas.IdShort ?? "--"} and id {findAasReturn.aas.Id?.ToString() ?? "--"}");
                findAasReturn.aas.Submodels.Remove(smref);
            }

            // delete Submodel 2nd
            var sm = this.FindSubmodelWithoutAas(smid, context.Request.QueryString, context.Request.RawUrl);
            if (sm != null)
            {
                context.Server.Logger.Debug($"Removing Submodel {smid} from data structures.");
                this.Packages[findAasReturn.iPackage].AasEnv.Submodels.Remove(sm);
            }

            // simple OK
            Program.signalNewData(2);
            var cmt = "";
            if (smref == null && sm == null)
                cmt += " (nothing deleted)";
            cmt                         += ((smref != null) ? " (SubmodelRef deleted)" : "") + ((sm != null) ? " (Submodel deleted)" : "");
            context.Response.StatusCode =  HttpStatusCode.Ok;
            SendTextResponse(context, "OK" + cmt);
        }

        #endregion

        #region // Submodel Complete

        static long countGet = 0;

        public void EvalGetSubmodelContents(IHttpContext context, string aasid, string smid, bool deep = false, bool complete = false)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/submodels", "READ"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);
            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with IdShort '{smid}' found.");
                return;
            }

            Console.WriteLine("{0} Received GET Submodel {1}", countGet++, sm.IdShort);

            // return as JSON
            var cr = new AdminShellConverters.AdaptiveFilterContractResolver(deep, complete);
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, sm, cr);
        }

        public void EvalGetSubmodelContentsAsTable(IHttpContext context, string aasid, string smid)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/submodels", "READ"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with IdShort '{smid}' found.");
                return;
            }

            // AAS ENV
            if (this.Packages[0] == null || this.Packages[0].AasEnv == null)
            {
                context.Response.SendResponse(HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }

            // make a table
            var table = new List<ExpandoObject>();
            sm.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                                               {
                                                   // start a row
                                                   dynamic row = new ExpandoObject();

                                                   // defaults
                                                   row.IdShorts  = "";
                                                   row.typeName  = "";
                                                   row.semIdType = "";
                                                   row.semId     = "";
                                                   row.shortName = "";
                                                   row.unit      = "";
                                                   row.Value     = "";

                                                   // IdShort is a concatenation
                                                   var path = "";
                                                   foreach (var p in parents)
                                                       path += p.IdShort + "/";

                                                   // SubnmodelElement general
                                                   row.IdShorts = path + sme.IdShort ?? "(-)";
                                                   //row.typeName = sme.GetElementName();
                                                   row.typeName = sme.GetType().ToString();
                                                   if (sme.SemanticId == null || sme.SemanticId.Keys == null /*|| sme.semanticId.Keys.Count == 0*/)
                                                   {
                                                   }
                                                   else if (sme.SemanticId.Keys.Count > 1)
                                                   {
                                                       row.semId = "(complex)";
                                                   }
                                                   else
                                                   {
                                                       row.semId = sme.SemanticId.Keys.First().Value;
                                                       //row.semIdType = sme.semanticId.Keys[0].idType;
                                                       //row.semId = sme.semanticId.Keys[0].Value;
                                                   }

                                                   // try find a concept description
                                                   if (sme.SemanticId != null)
                                                   {
                                                       var cd = this.Packages[0].AasEnv.FindConceptDescriptionByReference(sme.SemanticId);
                                                       if (cd != null)
                                                       {
                                                           // TODO (jtikekar, 2023-09-04): Temporarily commented
                                                           //var ds = cd.GetIEC61360();
                                                           //if (ds != null)
                                                           //{
                                                           //    row.shortName = (ds.shortName == null ? "" : ds.shortName.GetDefaultStr());
                                                           //    row.unit = ds.unit ?? "";
                                                           //}
                                                       }
                                                   }

                                                   // try add a value
                                                   if (sme is Property)
                                                   {
                                                       var p = sme as Property;
                                                       row.Value = "" + (p.Value ?? "") + ((p.ValueId != null) ? p.ValueId.ToString() : "");
                                                   }

                                                   if (sme is AasCore.Aas3_0.File)
                                                   {
                                                       var p = sme as AasCore.Aas3_0.File;
                                                       row.Value = "" + p.Value;
                                                   }

                                                   if (sme is Blob)
                                                   {
                                                       var p = sme as Blob;
                                                       if (p.Value.Length < 128)
                                                           row.Value = "" + p.Value;
                                                       else
                                                           row.Value = "(" + p.Value.Length + " bytes)";
                                                   }

                                                   if (sme is ReferenceElement gre)
                                                       row.Value = "" + gre.Value.ToString();

                                                   if (sme is ReferenceElement mre)
                                                       row.Value = "" + mre.Value.ToString();

                                                   if (sme is RelationshipElement)
                                                   {
                                                       var p = sme as RelationshipElement;
                                                       row.Value = "" + (p.First?.ToString() ?? "(-)") + " <-> " + (p.Second?.ToString() ?? "(-)");
                                                   }

                                                   // now, add the row
                                                   table.Add(row);

                                                   // recurse
                                                   return true;
                                               });

            // return as JSON
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, table);
        }

        #endregion

        #region // Submodel Elements

        public void EvalGetSubmodelElementContents(IHttpContext context, string aasid, string smid, string[] elemids, bool deep = false, bool complete = false)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string objPath = smid;
                foreach (var el in elemids)
                {
                    objPath += "." + el;
                }

                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/submodelelements", "READ", objPath))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with IdShort '{smid}' found.");
                return;
            }

            // find the right SubmodelElement
            var sme = this.FindSubmodelElement(sm, sm.SubmodelElements, elemids);
            if (sme == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No matching element in Submodel found.");
                return;
            }

            // return as JSON
            var cr = new AdminShellConverters.AdaptiveFilterContractResolver(deep, complete);
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, sme, cr);
        }

        public void EvalGetSubmodelElementsBlob(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/submodelelements", "READ"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with IdShort '{smid}' found.");
                return;
            }

            // find the right SubmodelElement
            var fse  = this.FindSubmodelElement(sm, sm.SubmodelElements, elemids);
            var smeb = fse?.elem as Blob;
            if (smeb == null || smeb.Value == null || (smeb.Value.Length == 0))
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No matching Blob element in Submodel found.");
                return;
            }

            // return as TEXT
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendTextResponse(context, smeb.Value.ToString(), mimeType: smeb.ContentType);
        }

        private string EvalGetSubmodelElementsProperty_EvalValue(Property smep)
        {
            // access
            if (smep == null)
                return null;

            // try to apply a little bit voodo
            double dblval = 0.0;
            string strval = smep.Value;
            if (smep.FindQualifierOfType("DEMO") != null && smep.Value != null
                                                         && smep.ValueType == DataTypeDefXsd.Double
                                                         && double.TryParse(smep.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out dblval))
            {
                // add noise
                dblval += Math.Sin((0.001 * DateTime.UtcNow.Millisecond) * 6.28);
                strval =  dblval.ToString(CultureInfo.InvariantCulture);
            }

            return strval;
        }

        private List<ExpandoObject> EvalGetSubmodelElementsProperty_EvalValues(
            List<ISubmodelElement> wrappers)
        {
            // access
            if (wrappers == null)
                return null;
            List<ExpandoObject> res = new List<ExpandoObject>();

            // recurse for results
            wrappers.RecurseOnSubmodelElements(null, new List<ISubmodelElement>(),
                                               (_, pars, el) =>
                                               {
                                                   if (el is Property smep && pars != null)
                                                   {
                                                       var path = new List<string>();
                                                       path.Add("" + smep?.IdShort);
                                                       for (int i = pars.Count - 1; i >= 0; i--)
                                                           path.Insert(0, "" + pars[i].IdShort);

                                                       dynamic tuple = new ExpandoObject();
                                                       tuple.path  = path;
                                                       tuple.Value = "" + EvalGetSubmodelElementsProperty_EvalValue(smep);
                                                       if (smep.ValueId != null)
                                                           tuple.ValueId = smep.ValueId;

                                                       res.Add(tuple);
                                                   }
                                               });

            // ok
            return res;
        }

        public void EvalGetSubmodelAllElementsProperty(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/submodelelements", "READ"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with IdShort '{smid}' found.");
                return;
            }

            // Submodel or SME?
            if (elemids == null || elemids.Length < 1)
            {
                // send the whole Submodel
                res.Values = EvalGetSubmodelElementsProperty_EvalValues(sm.SubmodelElements);
            }
            else
            {
                // find the right SubmodelElement
                var fse = this.FindSubmodelElement(sm, sm.SubmodelElements, elemids);

                if (fse?.elem is SubmodelElementCollection smec)
                {
                    res.Values = EvalGetSubmodelElementsProperty_EvalValues(smec.Value);
                }
                else if (fse?.elem is Property smep)
                {
                    res.Value = "" + EvalGetSubmodelElementsProperty_EvalValue(smep);
                    if (smep.ValueId != null)
                        res.ValueId = smep.ValueId;
                }
                else
                {
                    context.Response.SendResponse(HttpStatusCode.NotFound, $"No matching Property element(s) " +
                                                                           $"in Submodel found.");
                    return;
                }
            }

            // just send the result
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, res);
        }

        public void EvalGetSubmodelElementsFile(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/submodelelements", "READ"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with IdShort '{smid}' found.");
                return;
            }

            // find the right SubmodelElement
            var fse  = this.FindSubmodelElement(sm, sm.SubmodelElements, elemids);
            var smef = fse?.elem as AasCore.Aas3_0.File;
            if (smef == null || smef.Value == null || smef.Value == "")
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No matching File element in Submodel found.");
                return;
            }

            // access
            var packageStream = this.Packages[0].GetLocalStreamFromPackage(smef.Value);
            if (packageStream == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No file contents available in package.");
                return;
            }

            // return as FILE
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendStreamResponse(context, packageStream, Path.GetFileName(smef.Value));
            packageStream.Close();
        }

        public void EvalPutSubmodelElementContents(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string objPath = smid;
                foreach (var el in elemids)
                {
                    objPath += "." + el;
                }

                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/submodelelements", "UPDATE", objPath))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                return;
            }

            // de-serialize SubmodelElement
            ISubmodelElement sme = null;
            try
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));

                sme = JsonSerializer.Deserialize<ISubmodelElement>(context.Request.Payload, options);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                return;
            }


            // need id for idempotent behaviour
            if (sme.IdShort == null)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"IdShort of entity is (null); PUT cannot be performed.");
                return;
            }

            // access AAS and Submodel
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with IdShort '{smid}' found.");
                return;
            }

            // Check query parameter
            bool   first       = false;
            var    queryString = context.Request.QueryString;
            string f           = queryString["first"];
            if (f != null && f != "")
            {
                first = true;
            }

            // special case: parent is Submodel itself
            var timeStamp = DateTime.UtcNow;
            var updated   = false;
            if (elemids == null || elemids.Length < 1)
            {
                var existsmw = sm.FindSubmodelElementByIdShort(sme.IdShort);
                if (existsmw != null)
                {
                    updated             = true;
                    sme.TimeStampCreate = existsmw.TimeStampCreate;
                    context.Server.Logger.Debug($"Removing old SubmodelElement {sme.IdShort} from Submodel {smid}.");
                    int indexOfExistingSmw = sm.SubmodelElements.IndexOf(existsmw);
                    sm.SubmodelElements.RemoveAt(indexOfExistingSmw);
                    if (!first)
                    {
                        sm.SubmodelElements.Insert(indexOfExistingSmw, sme);
                    }
                    else
                    {
                        sm.SubmodelElements.Insert(0, sme);
                    }
                }
                else
                {
                    context.Server.Logger.Debug($"Adding new SubmodelElement {sme.IdShort} to Submodel {smid}.");
                    sme.TimeStampCreate = timeStamp;
                    if (!first)
                    {
                        sm.Add(sme);
                    }
                    else
                    {
                        sm.Insert(0, sme);
                    }
                }

                sme.SetAllParentsAndTimestamps(sm, timeStamp, sme.TimeStampCreate, sme.TimeStampDelete);
                sme.SetTimeStamp(timeStamp);
            }
            else
            {
                // find the right SubmodelElement
                var parent = this.FindSubmodelElement(sm, sm.SubmodelElements, elemids);
                if (parent == null)
                {
                    context.Response.SendResponse(HttpStatusCode.NotFound, $"No matching element in Submodel found.");
                    return;
                }

                if (parent.elem != null && parent.elem is SubmodelElementCollection)
                {
                    var parentsmc = parent.elem as SubmodelElementCollection;
                    var existsmw  = parentsmc.FindFirstIdShortAs<ISubmodelElement>(sme.IdShort);
                    if (existsmw != null)
                    {
                        updated             = true;
                        sme.TimeStampCreate = existsmw.TimeStampCreate;
                        context.Server.Logger.Debug($"Removing old SubmodelElement {sme.IdShort} from SubmodelCollection.");
                        int indexOfExistingSmw = parentsmc.Value.IndexOf(existsmw);
                        parentsmc.Value.RemoveAt(indexOfExistingSmw);
                        if (!first)
                        {
                            parentsmc.Insert(indexOfExistingSmw, sme);
                        }
                        else
                        {
                            parentsmc.Insert(0, sme);
                        }
                    }
                    else
                    {
                        sme.TimeStampCreate = timeStamp;
                        context.Server.Logger.Debug($"Adding new SubmodelElement {sme.IdShort} to SubmodelCollection.");
                        parentsmc.Add(sme);
                    }

                    sme.SetAllParentsAndTimestamps(parentsmc, timeStamp, sme.TimeStampCreate, sme.TimeStampDelete);
                    sme.SetTimeStamp(timeStamp);
                }
                else
                {
                    context.Response.SendResponse(HttpStatusCode.BadRequest, $"Matching SubmodelElement in Submodel {smid} is not suitable to add childs.");
                    return;
                }
            }

            // simple OK
            Program.signalNewData(2);
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendTextResponse(context, "OK" + (updated ? " (with updates)" : ""));
        }

        public void EvalDeleteSubmodelElementContents(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/submodelelements", "DELETE"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null || elemids == null || elemids.Length < 1)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with IdShort '{smid}' found or no elements to delete specified.");
                return;
            }

            // OK, Submodel and Element existing
            var fse = this.FindSubmodelElement(sm, sm.SubmodelElements, elemids);
            if (fse == null || fse.elem == null || fse.parent == null || fse.submodelElement == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No matching element in Submodel found.");
                return;
            }

            // where to delete?
            var deleted = false;
            var elinfo  = string.Join(".", elemids);
            if (fse.parent == sm)
            {
                context.Server.Logger.Debug($"Deleting specified SubmodelElement {elinfo} from Submodel {smid}.");
                AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.add(
                                                                                   fse.submodelElement, "Remove", sm, (ulong)DateTime.UtcNow.Ticks);
                sm.SubmodelElements.Remove(fse.submodelElement);
                deleted = true;
            }

            if (fse.parent is SubmodelElementCollection)
            {
                var smc = fse.parent as SubmodelElementCollection;
                context.Server.Logger.Debug($"Deleting specified SubmodelElement {elinfo} from SubmodelElementCollection {smc.IdShort}.");
                smc.Value.Remove(fse.submodelElement);
                deleted = true;
            }

            // simple OK
            Program.signalNewData(2);
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendTextResponse(context, "OK" + (!deleted ? " (but nothing deleted)" : ""));
        }

        public void EvalInvokeSubmodelElementOperation(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/submodelelements", "UPDATE"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with IdShort '{smid}' found.");
                return;
            }

            // find the right SubmodelElement
            var fse  = this.FindSubmodelElement(sm, sm.SubmodelElements, elemids);
            var smep = fse?.elem as Operation;
            if (smep == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No matching Operation element in Submodel found.");
                return;
            }

            // make 1st expectation
            int numExpectedInputArgs  = smep.InputVariables?.Count ?? 0;
            int numGivenInputArgs     = 0;
            int numExpectedOutputArgs = smep.OutputVariables?.Count ?? 0;
            var inputArguments        = (new int[numExpectedInputArgs]).Select(x => "").ToList();
            var outputArguments       = (new int[numExpectedOutputArgs]).Select(x => "my value").ToList();

            // is a payload required? Always, if at least one input argument required

            if (smep.InputVariables != null && smep.InputVariables.Count > 0)
            {
                // payload present
                if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
                {
                    context.Response.SendResponse(HttpStatusCode.BadRequest, $"No payload for Operation input argument or content type is not JSON.");
                    return;
                }

                // de-serialize SubmodelElement
                try
                {
                    // serialize
                    var input = System.Text.Json.JsonSerializer.Deserialize<List<string>>(context.Request.Payload);

                    // set inputs
                    if (input != null && input.Count > 0)
                    {
                        numGivenInputArgs = input.Count;
                        for (int i = 0; i < numGivenInputArgs; i++)
                            inputArguments[i] = input[i];
                    }
                }
                catch (Exception ex)
                {
                    context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                    return;
                }
            }

            // do a check
            if (numExpectedInputArgs != numGivenInputArgs)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Number of input arguments in payload does not fit expected input arguments of Operation.");
                return;
            }

            // just a test
            if (smep.FindQualifierOfType("DEMO") != null)
            {
                for (int i = 0; i < Math.Min(numExpectedInputArgs, numExpectedOutputArgs); i++)
                    outputArguments[i] = "CALC on " + inputArguments[i];
            }

            // return as little dynamic object
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, outputArguments);
        }

        public void EvalGetAllCds(IHttpContext context, string aasid)
        {
            dynamic res1  = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/cds", "READ"))
                {
                    return;
                }

                res1.confirm = "Authorization = " + accessrights;
            }

            // access the AAS
            var findAasReturn = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (findAasReturn.aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with IdShort '{aasid}' found.");
                return;
            }

            // build a list of results
            var res = new List<ExpandoObject>();

            // create a new, filtered AasEnv
            // (this is expensive, but delivers us with a list of CDs which are in relation to the respective AAS)
            var copyenv = new AasCore.Aas3_0.Environment();
            copyenv = copyenv.CreateFromExistingEnvironment(this.Packages[findAasReturn.iPackage].AasEnv,
                                                            filterForAas: new List<IAssetAdministrationShell>(new AssetAdministrationShell[]
                                                                                                              {
                                                                                                                  (AssetAdministrationShell)findAasReturn.aas
                                                                                                              }));

            // get all CDs and describe them
            foreach (var cd in copyenv.ConceptDescriptions)
            {
                // describe
                dynamic o = new ExpandoObject();
                o.IdShort = cd.IdShort;
                // TODO (jtikekar, 2023-09-04): temporarily commented 
                //o.shortName = cd.GetDefaultShortName();
                o.identification = cd.Id;
                o.isCaseOf       = cd.IsCaseOf;

                // add
                res.Add(o);
            }

            // return as JSON
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, res);
        }

        public void EvalGetCdContents(IHttpContext context, string aasid, string cdid)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/cds", "READ"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and CD
            var findAasReturn = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            var cd            = this.FindCdWithoutAas(findAasReturn, cdid, context.Request.QueryString, context.Request.RawUrl);

            if (cd == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no ConceptDescription with id '{cdid}' found.");
                return;
            }

            // return as JSON
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, cd);
        }

        public void EvalDeleteSpecificCd(IHttpContext context, string aasid, string cdid)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/cds", "DELETE"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and CD
            var findAasReturn = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            var cd            = this.FindCdWithoutAas(findAasReturn, cdid, context.Request.QueryString, context.Request.RawUrl);
            if (cd == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no ConceptDescription with id '{cdid}' found.");
                return;
            }

            // delete ?!
            var deleted = false;
            if (this.Packages[findAasReturn.iPackage] != null && this.Packages[findAasReturn.iPackage].AasEnv != null &&
                this.Packages[findAasReturn.iPackage].AasEnv.ConceptDescriptions.Contains(cd))
            {
                this.Packages[findAasReturn.iPackage].AasEnv.ConceptDescriptions.Remove(cd);
                deleted = true;
            }

            // return as JSON
            Program.signalNewData(2);
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendTextResponse(context, "OK" + (!deleted ? " (but nothing deleted)" : ""));
        }

        #endregion

        #region // GET + POST handles/identification

        public void EvalGetHandlesIdentification(IHttpContext context)
        {
            dynamic res1  = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/handles", "READ"))
                {
                    return;
                }

                res1.confirm = "Authorization = " + accessrights;
            }

            // get the list
            var res = IdRefHandleStore.FindAll<AasxHttpHandleIdentification>();

            // return this list
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, res);
        }

        public void EvalPostHandlesIdentification(IHttpContext context)
        {
            dynamic res1  = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/handles", "UPDATE"))
                {
                    return;
                }

                res1.confirm = "Authorization = " + accessrights;
            }

            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                return;
            }

            // list of Identification
            List<IIdentifiable> ids = null;
            try
            {
                ids = System.Text.Json.JsonSerializer.Deserialize<List<IIdentifiable>>(context.Request.Payload);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                return;
            }

            if (ids == null || ids.Count < 1)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"No Identification entities in payload.");
                return;
            }

            // OZ
            // Hack ASSX laden

            // turn these list into a list of Handles
            var res = new List<AasxHttpHandleIdentification>();
            foreach (var id in ids)
            {
                var h = new AasxHttpHandleIdentification(id.Id);
                IdRefHandleStore.Add(h);
                res.Add(h);
            }

            // return this list
            Program.signalNewData(2);
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, res);
        }

        #endregion

        #region // Server profile ..

        public void EvalGetServerProfile(IHttpContext context)
        {
            // get the list
            dynamic res = new ExpandoObject();
            var capabilities = new List<ulong>(new ulong[]
                                               {
                                                   80, 81, 82, 10, 11, 12, 13, 15, 16, 20, 21, 30, 31, 40, 41, 42, 43, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 70, 71, 72,
                                                   73
                                               });
            res.apiversion   = 1;
            res.capabilities = capabilities;

            // return this list
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, res);
        }

        public static bool withAuthentification = false;

        public static string GuestToken = null;

        public static string secretString = "Industrie4.0-Asset-Administration-Shell";

        private static RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();


        public int sessionCount = 0;
        public static char[] sessionUserType = new char[100];
        public static string[] sessionUserName = new string[100];
        public static RSA[] sessionUserPulicKey = new RSA[100];
        public static string[] sessionRandom = new string[100];
        public static string[] sessionChallenge = new string[100];

        public void EvalGetAuthenticateGuest(IHttpContext context)
        {
            Console.WriteLine();
            Console.WriteLine("AuthenticateGuest"); // GET

            // string with real random numbers
            Byte[] barray = new byte[100];
            randomNumberGenerator.GetBytes(barray);
            sessionRandom[sessionCount] = Convert.ToBase64String(barray);

            dynamic res     = new ExpandoObject();
            var     payload = new Dictionary<string, object>() {{"sessionID", sessionCount}, {"sessionRandom", sessionRandom[sessionCount]}};

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            GuestToken = Jose.JWT.Encode(payload, enc.GetBytes(secretString), JwsAlgorithm.HS256);

            Console.WriteLine("SessionID: " + sessionCount);
            Console.WriteLine("sessionRandom: " + GuestToken);

            withAuthentification = true;

            sessionUserType[sessionCount] = 'G';
            sessionUserName[sessionCount] = "guest";
            sessionCount++;
            if (sessionCount >= 100)
            {
                Console.WriteLine("ERROR: More than 100 sessions!");
                System.Environment.Exit(-1);
            }

            res.token = GuestToken;

            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, res);
        }

        public void EvalPostAuthenticateUser(IHttpContext context)
        {
            Console.WriteLine();
            Console.WriteLine("AuthenticateUser"); // POST User, Password

            bool userFound = false;
            bool error     = false;

            dynamic res    = new ExpandoObject();
            var     parsed = JsonDocument.Parse(context.Request.Payload);

            string user     = null;
            string password = null;
            try
            {
                user     = parsed.RootElement.GetProperty("user").GetString();
                password = parsed.RootElement.GetProperty("password").GetString();
            }
            catch
            {
                error = true;
            }

            if (!error)
            {
                int userCount = securityUserName.Count;

                for (int i = 0; i < userCount; i++)
                {
                    if (user == securityUserName[i] && password == securityUserPassword[i])
                    {
                        userFound = true;
                        break;
                    }
                }
            }

            if (error || !userFound)
            {
                res.error = "User not authorized!";
                SendJsonResponse(context, res);
                return;
            }

            // string with real random numbers
            Byte[] barray = new byte[100];
            randomNumberGenerator.GetBytes(barray);
            sessionRandom[sessionCount] = Convert.ToBase64String(barray);

            var payload = new Dictionary<string, object>() {{"sessionID", sessionCount}, {"sessionRandom", sessionRandom[sessionCount]}};

            System.Text.ASCIIEncoding enc   = new System.Text.ASCIIEncoding();
            string                    token = Jose.JWT.Encode(payload, enc.GetBytes(secretString), JwsAlgorithm.HS256);

            Console.WriteLine("SessionID: " + sessionCount);
            Console.WriteLine("sessionRandom: " + token);

            sessionUserType[sessionCount] = 'U';
            sessionUserName[sessionCount] = user;
            sessionCount++;
            if (sessionCount >= 100)
            {
                Console.WriteLine("ERROR: More than 100 sessions!");
                System.Environment.Exit(-1);
            }

            withAuthentification = true;

            res.token = token;

            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, res);
        }

        public void EvalPostAuthenticateCert1(IHttpContext context)
        {
            Console.WriteLine();
            Console.WriteLine("Security 2 Server: /AuthenticateCert1"); // POST token with user

            sessionUserType[sessionCount]  = ' ';
            sessionUserName[sessionCount]  = "";
            sessionRandom[sessionCount]    = "";
            sessionChallenge[sessionCount] = "";

            bool error = false;

            dynamic          res       = new ExpandoObject();
            X509Certificate2 x509      = null;
            string           user      = null;
            string           token     = null;
            RSA              publicKey = null;

            try
            {
                var     parsed = JsonDocument.Parse(context.Request.Payload);
                token = parsed.RootElement.GetProperty("token").GetString();

                var    headers = Jose.JWT.Headers(token);
                string x5c     = headers["x5c"].ToString();

                if (x5c != "")
                {
                    Console.WriteLine("Security 2.1a Server: x5c with certificate chain received");

                    parsed = JsonDocument.Parse(Jose.JWT.Payload(token));
                    user   = parsed.RootElement.GetProperty("user").GetString();

                    X509Store storeCA = new X509Store("CA", StoreLocation.CurrentUser);
                    storeCA.Open(OpenFlags.ReadWrite);
                    bool     valid = false;
                    
                    var x5c64 = JsonSerializer.Deserialize<string[]>(x5c);

                    X509Certificate2Collection xcc           = new X509Certificate2Collection();
                    Byte[]                     certFileBytes = Convert.FromBase64String(x5c64[0]);
                    string                     fileCert      = "./temp/" + user + ".cer";
                    System.IO.File.WriteAllBytes(fileCert, certFileBytes);
                    Console.WriteLine("Security 2.1b Server: " + fileCert + " received");

                    x509 = new X509Certificate2(certFileBytes);
                    xcc.Add(x509);

                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine("-----BEGIN CERTIFICATE-----");
                    builder.AppendLine(
                                       Convert.ToBase64String(x509.RawData, Base64FormattingOptions.InsertLineBreaks));
                    builder.AppendLine("-----END CERTIFICATE-----");
                    Console.WriteLine("Certificate: ");
                    Console.WriteLine(builder);

                    for (int i = 1; i < x5c64.Length; i++)
                    {
                        var cert = new X509Certificate2(Convert.FromBase64String(x5c64[i]));
                        Console.WriteLine("Security 2.1c Certificate in Chain: " + cert.Subject);
                        if (cert.Subject != cert.Issuer)
                        {
                            xcc.Add(cert);
                            storeCA.Add(cert);
                        }
                    }

                    X509Chain c = new X509Chain();
                    c.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                    valid = c.Build(x509);

                    storeCA.RemoveRange(xcc);
                    Console.WriteLine("Security 2.1d Server: Validate chain with root cert");

                    if (!valid)
                    {
                        Console.WriteLine("ERROR: Certificate " + x509.Subject + " not valid!");
                        error = true;
                    }
                }
                else
                {
                    parsed = JsonDocument.Parse(Jose.JWT.Payload(token));
                    user   = parsed.RootElement.GetProperty("user").GetString();

                    string fileCert = "./user/" + user + ".cer";
                    if (System.IO.File.Exists(fileCert))
                    {
                        x509 = new X509Certificate2(fileCert);
                        Console.WriteLine("Security 2.1a Server: " + fileCert + "exists");
                    }
                    else
                    {
                        // receive .cer and verify against root
                        string certFileBase64 = parsed.RootElement.GetProperty("certFile").GetString();
                        Byte[] certFileBytes  = Convert.FromBase64String(certFileBase64);
                        fileCert = "./temp/" + user + ".cer";
                        System.IO.File.WriteAllBytes(fileCert, certFileBytes);
                        Console.WriteLine("Security 2.1b Server: " + fileCert + " received");

                        x509 = new X509Certificate2(certFileBytes);

                        // check if certificate is valid according to root certificates
                        X509Chain chain = new X509Chain();
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                        bool isValid = chain.Build(x509);
                        Console.WriteLine("Security 2.1c Server: Validate " + fileCert + " with root cert");

                        if (!isValid)
                        {
                            error = true;
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("ERROR: Certificate " + user + ".cer" + " not found!");
                error = true;
            }

            if (!error)
            {
                try
                {
                    publicKey = x509.GetRSAPublicKey();

                    Jose.JWT.Decode(token, publicKey, JwsAlgorithm.RS256); // signed by user key?
                    Console.WriteLine("Security 2.2 Server: Validate signature with publicKey");
                }
                catch
                {
                    error = true;
                }
            }

            if (!error)
            {
                // user information was correctly signed

                // string with real random numbers
                Byte[] barray = new byte[100];
                randomNumberGenerator.GetBytes(barray);

                Console.WriteLine("Security 2.3 Server: Create session unique challenge by real random");
                sessionChallenge[sessionCount] = Convert.ToBase64String(barray);
            }

            if (error)
            {
                res.error = "User not authorized!";
                SendJsonResponse(context, res);
                return;
            }

            Console.WriteLine("sessionID: " + sessionCount);
            Console.WriteLine("session challenge: " + sessionChallenge[sessionCount]);

            sessionUserType[sessionCount]     = 'T';
            sessionUserName[sessionCount]     = user;
            sessionUserPulicKey[sessionCount] = publicKey;

            withAuthentification = true;

            res.challenge = sessionChallenge[sessionCount];

            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, res);
        }

        public void EvalPostAuthenticateCert2(IHttpContext context)
        {
            Console.WriteLine();
            Console.WriteLine("Security 3 Server: /AuthenticateCert2"); // POST token with user

            sessionRandom[sessionCount] = "";

            bool error = false;

            dynamic res       = new ExpandoObject();
            string  token     = null;
            string  challenge = null;
            RSA     publicKey = null;

            try
            {
                var parsed = JsonDocument.Parse(context.Request.Payload);
                token = parsed.RootElement.GetProperty("token").GetString();

                parsed    = JsonDocument.Parse(Jose.JWT.Payload(token));
                challenge = parsed.RootElement.GetProperty("challenge").GetString();
            }
            catch
            {
                Console.WriteLine("ERROR: Challenge sent is not received back!");
                error = true;
            }

            if (challenge != sessionChallenge[sessionCount] || sessionChallenge[sessionCount] == null || sessionChallenge[sessionCount] == "")
            {
                error = true;
            }

            if (!error)
            {
                try
                {
                    publicKey = sessionUserPulicKey[sessionCount];

                    Jose.JWT.Decode(token, publicKey, JwsAlgorithm.RS256); // signed by user key?
                    Console.WriteLine("Security 3.1 Server: Validate challenge signature with publicKey");
                }
                catch
                {
                    error = true;
                }
            }

            if (!error)
            {
                // challenge was correctly signed

                // string with real random numbers
                Byte[] barray = new byte[100];
                randomNumberGenerator.GetBytes(barray);
                Console.WriteLine("Security 3.2 Server: Create session unique bearerToken signed by real random");
                sessionRandom[sessionCount] = Convert.ToBase64String(barray);

                var payload = new Dictionary<string, object>() {{"sessionID", sessionCount},};

                try
                {
                    var enc = new System.Text.ASCIIEncoding();
                    token = Jose.JWT.Encode(payload, enc.GetBytes(sessionRandom[sessionCount]), JwsAlgorithm.HS256);
                    Console.WriteLine("Security 3.3 Server: Sign sessionID by server sessionRandom");
                }
                catch
                {
                    error = true;
                }
            }

            if (error)
            {
                res.error = "User not authorized!";
                SendJsonResponse(context, res);
                return;
            }

            Console.WriteLine("sessionID: " + sessionCount);
            Console.WriteLine("session random: " + sessionRandom[sessionCount]);
            Console.WriteLine("session bearerToken: " + token);

            sessionCount++;
            if (sessionCount >= 100)
            {
                Console.WriteLine("ERROR: More than 100 sessions!");
                System.Environment.Exit(-1);
            }

            withAuthentification = true;

            res.token = token;

            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, res);
        }

        public static bool checkAccessLevel(string currentRole, string operation, string neededRights,
                                            string objPath = "", string aasOrSubmodel = null, object objectAasOrSubmodel = null)
        {
            string error     = "";
            string getPolicy = null;
            bool   withAllow = false;
            return checkAccessLevelWithError(out error, currentRole, operation, neededRights, out withAllow, out getPolicy,
                                             objPath, aasOrSubmodel, objectAasOrSubmodel);
        }

        public static bool checkAccessLevelWithAllow(string currentRole, string operation, string neededRights,
                                                     out bool withAllow, string objPath = "", string aasOrSubmodel = null, object objectAasOrSubmodel = null,
                                                     string policy = null)
        {
            string error     = "";
            string getPolicy = null;
            withAllow = false;
            return checkAccessLevelWithError(out error, currentRole, operation, neededRights, out withAllow, out getPolicy,
                                             objPath, aasOrSubmodel, objectAasOrSubmodel, policy);
        }

        public static bool checkAccessLevelWithError(out string error, string currentRole, string operation,
                                                     string neededRights, out bool withAllow, out string getPolicy,
                                                     string objPath = "", string aasOrSubmodel = null, object objectAasOrSubmodel = null, string policy = null)
        {
            error     = "";
            getPolicy = null;
            withAllow = false;

            if (Program.secretStringAPI != null)
            {
                /*
                if (neededRights == "READ")
                    return true;
                if ((neededRights == "UPDATE" || neededRights == "DELETE") && currentRole == "UPDATE")
                    return true;
                */
                if (currentRole == "CREATE")
                    return true;
            }

            if (currentRole == null)
                currentRole = "isNotAuthenticated";

            Console.WriteLine("checkAccessLevel: " +
                              " currentRole = " + currentRole +
                              " operation = " + operation +
                              " neededRights = " + neededRights +
                              " objPath = " + objPath
                             );

            if (objPath == "")
            {
                int iRole = 0;
                while (securityRole != null && iRole < securityRole.Count && securityRole[iRole].name != null)
                {
                    if (aasOrSubmodel == "aas" && securityRole[iRole].objType == "aas")
                        /* (aasOrSubmodel == "submodel" && securityRole[iRole].objType == "sm")) */
                    {
                        if (objectAasOrSubmodel != null && securityRole[iRole].objReference == objectAasOrSubmodel &&
                            securityRole[iRole].permission == neededRights)
                        {
                            if ((securityRole[iRole].condition == "" && securityRole[iRole].name == currentRole) ||
                                (securityRole[iRole].condition == "not" && securityRole[iRole].name != currentRole))
                            {
                                if (securityRole[iRole].kind == "allow")
                                    return true;
                                if (securityRole[iRole].kind == "deny")
                                {
                                    error = "DENY AAS " + (objectAasOrSubmodel as AssetAdministrationShell).Id;
                                    return false;
                                }
                            }
                        }
                    }

                    if (securityRole[iRole].name == currentRole && securityRole[iRole].objType == "api" &&
                        securityRole[iRole].permission == neededRights)
                    {
                        if (securityRole[iRole].apiOperation == "*" || securityRole[iRole].apiOperation == operation)
                        {
                            if (securityRole[iRole].permission == neededRights)
                            {
                                return checkPolicy(out error, securityRole[iRole], out getPolicy);
                            }
                        }
                    }

                    iRole++;
                }
            }

            if (objPath != "" && (operation == "/submodels" || operation == "/submodelelements"))
            {
                // next object with rule must have allow
                // no objects below must have deny
                string            deepestDeny      = "";
                string            deepestAllow     = "";
                securityRoleClass deepestAllowRole = null;
                foreach (var role in securityRole)
                {
                    if (role.name != currentRole)
                        continue;

                    if (role.objType == "semanticid")
                    {
                        if (objectAasOrSubmodel is Submodel s)
                        {
                            if (role.semanticId == "*" || (s.SemanticId != null && s.SemanticId.Keys != null && s.SemanticId.Keys.Count != 0))
                            {
                                if (role.semanticId == "*" || (role.semanticId.ToLower() == s.SemanticId.Keys[0].Value.ToLower()))
                                {
                                    if (role.kind == "allow")
                                    {
                                        if (deepestAllow == "")
                                        {
                                            deepestAllow     = s.IdShort;
                                            withAllow        = true;
                                            deepestAllowRole = role;
                                        }
                                    }

                                    if (role.kind == "deny")
                                    {
                                        if (deepestDeny == "")
                                            deepestDeny = s.IdShort;
                                    }
                                }
                            }
                        }

                        if (objectAasOrSubmodel is string s2)
                        {
                            if (s2 != null && s2 != "")
                            {
                                if (role.semanticId == s2)
                                {
                                    if (role.kind == "allow")
                                    {
                                        if (deepestAllow == "")
                                        {
                                            deepestAllow     = objPath;
                                            withAllow        = true;
                                            deepestAllowRole = role;
                                        }
                                    }

                                    if (role.kind == "deny")
                                    {
                                        if (deepestDeny == "")
                                            deepestDeny = objPath;
                                    }
                                }
                            }
                        }
                    }

                    if ((role.objType == "sm" || role.objType == "submodelElement") &&
                        role.submodel == objectAasOrSubmodel && role.permission == neededRights)
                    {
                        if (role.kind == "deny")
                        {
                            if (objPath.Length >= role.objPath.Length) // deny in tree above
                            {
                                if (role.objPath == objPath.Substring(0, role.objPath.Length))
                                    deepestDeny = role.objPath;
                            }

                            if (role.objPath.Length >= objPath.Length) // deny in tree below
                            {
                                if (objPath == role.objPath.Substring(0, objPath.Length))
                                {
                                    error = "DENY " + role.objPath;
                                    return false;
                                }
                            }
                        }

                        if (role.kind == "allow")
                        {
                            if (objPath.Length >= role.objPath.Length) // allow in tree above
                            {
                                if (role.objPath == objPath.Substring(0, role.objPath.Length))
                                {
                                    deepestAllow     = role.objPath;
                                    withAllow        = true;
                                    deepestAllowRole = role;
                                }
                            }
                        }
                    }
                }

                if (deepestAllow == "")
                {
                    error = "ALLOW not defined";
                    return false;
                }

                if (deepestDeny.Length > deepestAllow.Length)
                {
                    error = "DENY " + deepestDeny;
                    return false;
                }

                return checkPolicy(out error, deepestAllowRole, out getPolicy, policy);
            }

            error = "ALLOW not defined";
            return false;
        }

        public static bool checkPolicy(out string error, securityRoleClass sr, out string getPolicy, string policy = null)
        {
            error     = "";
            getPolicy = null;
            Property            pPolicy = null;
            AasCore.Aas3_0.File fPolicy = null;

            if (sr.usage == null)
                return true;

            foreach (var sme in sr.usage.Value)
            {
                switch (sme.IdShort)
                {
                    case "accessPerDuration":
                        if (sme is SubmodelElementCollection smc)
                        {
                            Property maxCount    = null;
                            Property actualCount = null;
                            Property duration    = null;
                            Property actualTime  = null;
                            foreach (var sme2 in smc.Value)
                            {
                                switch (sme2.IdShort)
                                {
                                    case "maxCount":
                                        maxCount = sme2 as Property;
                                        break;
                                    case "duration":
                                        duration = sme2 as Property;
                                        break;
                                    case "actualCount":
                                        actualCount = sme2 as Property;
                                        break;
                                    case "actualTime":
                                        actualTime = sme2 as Property;
                                        break;
                                }
                            }

                            if (maxCount == null || duration == null || actualCount == null || actualTime == null)
                                return false;
                            int d = 0;
                            if (!int.TryParse(duration.Value, out d))
                            {
                                return false;
                            }

                            DateTime dt = new DateTime();
                            if (actualTime.Value != null && actualTime.Value != "")
                            {
                                try
                                {
                                    dt = DateTime.Parse(actualTime.Value);
                                    if (dt.AddSeconds(d) < DateTime.UtcNow)
                                    {
                                        Program.signalNewData(0);
                                        actualTime.Value = null;
                                    }
                                }
                                catch
                                {
                                }
                            }

                            if (actualTime.Value == null || actualTime.Value == "")
                            {
                                actualTime.Value  = DateTime.UtcNow.ToString();
                                actualCount.Value = null;
                            }

                            if (actualCount.Value == null || actualCount.Value == "")
                            {
                                actualCount.Value = "0";
                            }

                            int ac = 0;
                            if (!int.TryParse(actualCount.Value, out ac))
                            {
                                Program.signalNewData(0);
                                return false;
                            }

                            int mc = 0;
                            if (!int.TryParse(maxCount.Value, out mc))
                            {
                                Program.signalNewData(0);
                                return false;
                            }

                            ac++;
                            actualCount.Value = ac.ToString();
                            if (ac <= mc)
                            {
                                Program.signalNewData(0);
                                return true;
                            }
                        }

                        break;
                    case "policy":
                        pPolicy = sme as Property;
                        break;
                    case "license":
                        fPolicy = sme as AasCore.Aas3_0.File;
                        break;
                    case "policyRequestedResource":
                        break;
                }
            }

            if (pPolicy != null)
            {
                if (!Program.withPolicy)
                    return true;

                getPolicy = pPolicy.Value;
                if (getPolicy == "" && fPolicy != null)
                {
                    try
                    {
                        using (System.IO.Stream s = Program.env[sr.usageEnvIndex].GetLocalStreamFromPackage(fPolicy.Value))
                        using (SHA256 mySHA256 = SHA256.Create())
                        {
                            if (s != null)
                            {
                                s.Position = 0;
                                byte[] hashValue = mySHA256.ComputeHash(s);
                                getPolicy = Convert.ToHexString(hashValue);
                                Console.WriteLine("hash: " + getPolicy);
                                pPolicy.Value = getPolicy;
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                if (policy == null || policy.Contains(getPolicy))
                {
                    // Program.signalNewData(0);
                    return true;
                }
            }

            // Program.signalNewData(0);
            return false;
        }

        public bool checkAccessRights(IHttpContext context, string currentRole, string operation, string neededRights,
                                      string objPath = "", string aasOrSubmodel = null, object objectAasOrSubmodel = null)
        {
            if (Program.secretStringAPI != null)
            {
                /*
                if (neededRights == "READ")
                    return true;
                if ((neededRights == "UPDATE" || neededRights == "DELETE") && currentRole == "UPDATE")
                    return true;
                */
                if (currentRole == "CREATE")
                    return true;
            }

            // else
            {
                if (checkAccessLevel(currentRole, operation, neededRights,
                                     objPath, aasOrSubmodel, objectAasOrSubmodel))
                    return true;

                if (currentRole == null)
                {
                    if (AasxServer.Program.redirectServer != "")
                    {
                        System.Collections.Specialized.NameValueCollection queryString     = System.Web.HttpUtility.ParseQueryString(string.Empty);
                        string                                             originalRequest = context.Request.Url.ToString();
                        queryString.Add("OriginalRequest", originalRequest);
                        Console.WriteLine("\nRedirect OriginalRequset: " + originalRequest);
                        string response = AasxServer.Program.redirectServer + "?" + "authType=" + AasxServer.Program.authType + "&" + queryString;
                        Console.WriteLine("Redirect Response: " + response + "\n");
                        SendRedirectResponse(context, response);
                        return false;
                    }
                }
            }

            dynamic res = new ExpandoObject();
            res.error                   = "You are not authorized for this operation!";
            context.Response.StatusCode = HttpStatusCode.Unauthorized;
            SendJsonResponse(context, res);

            return false;
        }

        public static string SecurityCheck(IHttpContext context, ref int index)
        {
            return SecurityCheck(context.Request.QueryString, context.Request.Headers, ref index);
        }

        static string checkUserPW(string userPW64)
        {
            var    credentialBytes = Convert.FromBase64String(userPW64);
            var    credentials     = Encoding.UTF8.GetString(credentialBytes).Split(new[] {':'}, 2);
            string username        = credentials[0];
            string password        = credentials[1];

            int userCount = securityUserName.Count;

            for (int i = 0; i < userCount; i++)
            {
                if (username == securityUserName[i] && password == securityUserPassword[i])
                {
                    return (username);
                }
            }

            return null;
        }

        public static string SecurityCheck(NameValueCollection queryString, NameValueCollection headers, ref int index)
        {
            string policy                  = "";
            string policyRequestedResource = "";

            return SecurityCheckWithPolicy(queryString, headers, ref index, out policy, out policyRequestedResource);
        }

        class userCert
        {
            public string userName = "";
            public X509Certificate2 certificate = null;
        }

        static List<userCert> certList = new List<userCert>();
        public static object lockCertList = new object();

        public static string SecurityCheckWithPolicy(NameValueCollection queryString, NameValueCollection headers, ref int index,
                                                     out string policy, out string policyRequestedResource)
        {
            Console.WriteLine("SecurityCheck");
            bool   error        = false;
            string accessrights = null;
            policy                  = "";
            policyRequestedResource = "";

            // receive token with sessionID inside
            // check if token is signed by sessionRandom
            // read username for sessionID
            // check accessrights for username

            dynamic res         = new ExpandoObject();
            int     id          = -1;
            string  token       = null;
            string  random      = null;
            string  bearerToken = null;
            string  user        = null;
            string  certificate = null;

            index = -1; // not found

            string[] split = null;

            // check for secret

            string s = queryString["s"];
            if (s != null && s != "")
            {
                if (Program.secretStringAPI != null)
                {
                    // accessrights = "READ";

                    // Query string with Secret?
                    {
                        if (s == Program.secretStringAPI)
                            accessrights = "CREATE";
                    }

                    return accessrights;
                }
            }

            // string headers = request.Headers.ToString();

            // Check bearer token
            token = headers["Authorization"];
            if (token != null)
            {
                split = token.Split(new Char[] {' ', '\t'});
                if (split[0] != null)
                {
                    if (split[0].ToLower() == "bearer")
                    {
                        Console.WriteLine("Received bearer token = " + split[1]);
                        bearerToken = split[1];
                    }

                    if (bearerToken == null && split[0].ToLower() == "basic")
                    {
                        try
                        {
                            if (Program.secretStringAPI != null)
                            {
                                var    credentialBytes = Convert.FromBase64String(split[1]);
                                var    credentials     = Encoding.UTF8.GetString(credentialBytes).Split(new[] {':'}, 2);
                                string u               = credentials[0];
                                string p               = credentials[1];
                                Console.WriteLine("Received username+password http header = " + u + " : " + p);

                                if (u == "secret")
                                {
                                    // accessrights = "READ";
                                    {
                                        if (p == Program.secretStringAPI)
                                            accessrights = "CREATE";
                                    }
                                    Console.WriteLine("accessrights " + accessrights);
                                    return accessrights;
                                }
                            }

                            string username = checkUserPW(split[1]);
                            if (username != null)
                            {
                                user = username;
                                Console.WriteLine("Received username+password http header = " + user);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            else // check query string for bearer token
            {
                /*
                split = request.Url.ToString().Split(new char[] { '?' });
                if (split != null && split.Length > 1 && split[1] != null)
                {
                    Console.WriteLine("Received query string = " + split[1]);
                    bearerToken = split[1];
                }
                */
                bearerToken = queryString["bearer"];
            }

            if (bearerToken == null)
            {
                error = true;
            }

            // Check email token
            token = headers["Email"];
            if (token != null)
            {
                Console.WriteLine("Received Email token = " + token);
                user  = token;
                error = false;
            }

            // Check email query string
            token = queryString["Email"];
            if (token != null)
            {
                Console.WriteLine("Received Email query string = " + token);
                user  = token;
                error = false;
            }

            // Username+password query string
            token = queryString["_up"];
            if (token != null)
            {
                try
                {
                    string username = checkUserPW(token);
                    if (username != null)
                    {
                        user = username;
                        Console.WriteLine("Received username+password query string = " + user);
                    }
                }
                catch
                {
                }
            }

            if (!error)
            {
                JsonDocument parsed2 = null;

                try
                {
                    if (bearerToken != null)
                    {
                        string serverName = "";
                        string email      = "";

                        parsed2 = JsonDocument.Parse(Jose.JWT.Payload(bearerToken));

                        try
                        {
                            email = parsed2.RootElement.GetProperty("email").GetString();
                        }
                        catch
                        {
                        }

                        try
                        {
                            serverName = parsed2.RootElement.GetProperty("serverName").GetString();
                        }
                        catch
                        {
                            // serverName = "keycloak";
                        }

                        try
                        {
                            policy = parsed2.RootElement.GetProperty("policy").GetString();
                        }
                        catch
                        {
                        }

                        try
                        {
                            policyRequestedResource = parsed2.RootElement.GetProperty("policyRequestedResource").GetString();
                        }
                        catch
                        {
                        }

                        if (email != "")
                        {
                            user = email.ToLower();
                        }


                        try
                        {
                            user = parsed2.RootElement.GetProperty("userName").GetString();
                            user = user.ToLower();
                        }
                        catch
                        {
                        }

                        if (serverName != "") // token from Auth Server
                        {
                            X509Certificate2 cert = serverCertFind(serverName);

                            if (cert == null) return null;

                            StringBuilder builder = new StringBuilder();
                            builder.AppendLine("-----BEGIN CERTIFICATE-----");
                            builder.AppendLine(
                                               Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks));
                            builder.AppendLine("-----END CERTIFICATE-----");
                            Console.WriteLine("Token Server Certificate: " + serverName);
                            Console.WriteLine(builder);

                            try
                            {
                                Jose.JWT.Decode(bearerToken, cert.GetRSAPublicKey(), JwsAlgorithm.RS256); // correctly signed by auth server cert?
                            }
                            catch
                            {
                                return null;
                            }

                            try
                            {
                                certificate = parsed2.RootElement.GetProperty("certificate").GetString();
                            }
                            catch
                            {
                            }

                            if (certificate != "")
                            {
                                lock (lockCertList)
                                {
                                    Byte[] certFileBytes = Convert.FromBase64String(certificate);
                                    var    x509          = new X509Certificate2(certFileBytes);
                                    int    i             = 0;
                                    for (i = 0; i < certList.Count; i++)
                                    {
                                        if (certList[i].userName == user)
                                        {
                                            if (certList[i].certificate != null)
                                                certList[i].certificate.Dispose();
                                            certList[i].certificate = x509;
                                            break;
                                        }
                                    }

                                    if (i == certList.Count)
                                    {
                                        var cl = new userCert();
                                        cl.userName    = user;
                                        cl.certificate = x509;
                                        certList.Add(cl);
                                    }
                                }
                            }
                        }
                        else // client token
                        {
                            X509Certificate2 x509 = null;
                            lock (lockCertList)
                            {
                                int i = 0;
                                for (i = 0; i < certList.Count; i++)
                                {
                                    if (certList[i].userName == user)
                                    {
                                        x509 = certList[i].certificate;
                                        break;
                                    }
                                }
                            }

                            try
                            {
                                Jose.JWT.Decode(bearerToken, x509.GetRSAPublicKey(), JwsAlgorithm.RS256); // correctly signed by stored client cert?
                            }
                            catch
                            {
                                x509 = null;
                            }

                            if (x509 == null)
                            {
                                Console.WriteLine("Invalid client token!");
                                return null;
                            }
                        }
                    }
                }
                catch
                {
                    error = true;
                }
            }

            if (user != null && user != "")
            {
                if (securityRights != null)
                {
                    int rightsCount = securityRights.Count;

                    if (user.Contains("@")) // email address
                    {
                        for (int i = 0; i < rightsCount; i++)
                        {
                            if (securityRights[i].name.Contains("@")) // email address
                            {
                                if (user == securityRights[i].name)
                                {
                                    // accessrights = securityRightsValue[i];
                                    accessrights = securityRights[i].role;
                                    return accessrights;
                                }
                            }
                        }
                    }

                    for (int i = 0; i < rightsCount; i++)
                    {
                        if (!securityRights[i].name.Contains("@")) // domain name only or non email
                        {
                            string u = user;
                            if (user.Contains("@"))
                            {
                                string[] splitUser = user.Split('@');
                                u = splitUser[1]; // domain only
                            }

                            if (u == securityRights[i].name)
                            {
                                // accessrights = securityRightsValue[i];
                                accessrights = securityRights[i].role;
                                return accessrights;
                            }
                        }
                    }
                }

                return accessrights;
            }
            /*
            else
            {
                id = Convert.ToInt32(parsed2.SelectToken("sessionID").Value<string>());

                random = sessionRandom[id];
                user = sessionUserName[id];

                if (random == null || random == "" || user == null || user == "")
                {
                    error = true;
                }
            }
            */

            if (!error)
            {
                try
                {
                    string payload = null;

                    switch (sessionUserType[id])
                    {
                        case 'G':
                        case 'U':
                        case 'T':
                            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                            payload = Jose.JWT.Decode(bearerToken, enc.GetBytes(random), JwsAlgorithm.HS256); // correctly signed by session token?
                            break;
                    }
                }
                catch
                {
                    error = true;
                }
            }

            /*
            if (!error)
            {
                switch (sessionUserType[id])
                {
                    case 'G':
                        accessrights = "GUEST";
                        break;
                    case 'U':
                    case 'T':
                        if (securityRightsName != null)
                        {
                            int rightsCount = securityRightsName.Length;

                            for (int i = 0; i < rightsCount; i++)
                            {
                                if (user == securityRightsName[i])
                                {
                                    accessrights = securityRightsValue[i];
                                    break;
                                }
                            }
                        }
                        break;
                }
            }
            */

            if (!error)
            {
                index = id;
            }

            return accessrights;
        }

        public void EvalGetListAAS(IHttpContext context, bool withasset = false)
        {
            dynamic res     = new ExpandoObject();
            int     index   = -1;
            var     aaslist = new List<string>();

            Console.WriteLine("Security 4 Server: /server/listaas");

            string accessrights = SecurityCheck(context, ref index);

            if (!withAuthentification || checkAccessLevel(accessrights, "/server/listaas", "READ"))
            {
                // get the list
                int aascount = AasxServer.Program.env.Length;

                for (int i = 0; i < aascount; i++)
                {
                    if (AasxServer.Program.env[i] != null)
                    {
                        var    aas       = AasxServer.Program.env[i].AasEnv.AssetAdministrationShells[0];
                        string IdShort   = aas.IdShort;
                        string aasRights = "NONE";
                        if (securityRightsAAS != null && securityRightsAAS.Count != 0)
                            securityRightsAAS.TryGetValue(IdShort, out aasRights);
                        // aasRights = securityRightsAAS[IdShort];

                        bool addEntry = false;
                        // if (!withAuthentification || checkAccessLevel(accessrights, "", "READ", "", "aas", aas))
                        // allow all AAS in list
                        {
                            addEntry = true;
                        }

                        if (addEntry)
                        {
                            string s = i.ToString() + " : "
                                                    + IdShort + " : "
                                                    + aas.Id + " : "
                                                    + AasxServer.Program.envFileName[i];
                            if (withasset)
                            {
                                //var asset = Program.env[i].AasEnv.FindAsset(aas.assetRef);
                                var asset = aas.AssetInformation;
                                s += " : " + asset.GlobalAssetId;
                                s += " : " + asset.AssetKind;
                            }

                            aaslist.Add(s);
                        }
                    }
                }
            }

            res.aaslist = aaslist;

            // return this list
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, res);
        }

        public void EvalAssetId(IHttpContext context, int assetId)
        {
            dynamic res = new ExpandoObject();

            Console.WriteLine("Test Asset ID");

            string headers = context.Request.Headers.ToString();
            string token   = context.Request.Headers.Get("accept");
            if (token != null)
            {
                if (token == "application/aas")
                {
                    Console.WriteLine("Received Accept header = " + token);
// context.Response.ContentType = ContentType.JSON;
                    context.Response.AddHeader("Content-Type", "application/aas");

                    res.client        = "I40 IT client";
                    res.assetID       = assetId;
                    res.humanEndpoint = "https://admin-shell-io.com:5001";
                    res.restEndpoint  = "http://" + AasxServer.Program.hostPort;

                    var json   = JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true });
                    var buffer = Encoding.UTF8.GetBytes(json);
                    var length = buffer.Length;

                    context.Response.ContentEncoding = Encoding.UTF8;
                    context.Response.ContentLength64 = length;
                    context.Response.StatusCode      = HttpStatusCode.Ok;
                    context.Response.SendResponse(buffer);

                    return;

                }
            }

            // SendJsonResponse(context, res);
            SendRedirectResponse(context, "https://admin-shell-io.com:5001");
        }

        public void EvalGetAASX(IHttpContext context, int fileIndex)
        {
            dynamic res          = new ExpandoObject();
            int     index        = -1;
            string  accessrights = null;

            // check authentication
            if (withAuthentification)
            {
                accessrights = SecurityCheck(context, ref index);

                /*
                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
                */

                var aas = Program.env[fileIndex].AasEnv.AssetAdministrationShells[0];
                if (!checkAccessRights(context, accessrights, "/aasx", "READ", "", "aas", aas))
                {
                    return;
                }

                /*
                string IdShort = AasxServer.Program.env[fileIndex].AasEnv.AssetAdministrationShells[0].IdShort;
                string aasRights = "NONE";
                if (securityRightsAAS.Count != 0)
                    aasRights = securityRightsAAS[IdShort];
                if (!checkAccessRights(context, accessrights, aasRights))
                {
                    return;
                }
                */
            }

            // save actual data as file

            lock (Program.changeAasxFile)
            {
                string fname = "./temp/" + Path.GetFileName(Program.envFileName[fileIndex]);
                Program.env[fileIndex].SaveAs(fname);

                // return as FILE
                FileStream packageStream = System.IO.File.OpenRead(fname);
                context.Response.StatusCode = HttpStatusCode.Ok;
                SendStreamResponse(context, packageStream,
                                   Path.GetFileName(AasxServer.Program.envFileName[fileIndex]));
                packageStream.Close();

                // Reload
                // Program.env[fileIndex] = new AdminShellPackageEnv(fname);
            }
        }

        public void EvalGetAASX2(IHttpContext context, int fileIndex)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            string accessrights = null;
            if (withAuthentification)
            {
                accessrights = SecurityCheck(context, ref index);

                /*
                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
                */
            }
            else
            {
                accessrights = "READ";
            }

            Console.WriteLine("Security 5 Server: /server/getaasx2/" + fileIndex);

            // check authentication
            /*
            if (!withAuthentification)
            {
                res.error = "You are not authorized for this operation!";
                SendJsonResponse(context, res);
                return;
            }
            string accessrights = SecurityCheck(context, ref index);
            */

            Console.WriteLine("Security 5.1 Server: Check bearer token and access rights");
            Console.WriteLine("Security 5.2 Server: Validate that bearer token is signed by session unique random");

            if (!checkAccessRights(context, accessrights, "/aasx", "READ"))
            {
                return;
            }

            res.confirm = "Authorization = " + accessrights;

            Byte[] binaryFile   = System.IO.File.ReadAllBytes(AasxServer.Program.envFileName[fileIndex]);
            string binaryBase64 = Convert.ToBase64String(binaryFile);

            string payload = "{ \"file\" : \" " + binaryBase64 + " \" }";

            System.Text.ASCIIEncoding enc       = new System.Text.ASCIIEncoding();
            string                    fileToken = Jose.JWT.Encode(payload, enc.GetBytes(secretString), JwsAlgorithm.HS256);

            res.fileName = Path.GetFileName(AasxServer.Program.envFileName[fileIndex]);
            res.fileData = fileToken;

            context.Response.StatusCode = HttpStatusCode.Ok;
            SendJsonResponse(context, res);
        }

        #endregion

        public void EvalGetFile(IHttpContext context, int envIndex, string filePath)
        {
            context.Response.StatusCode = HttpStatusCode.Ok;
            var fname = Path.GetFileName(filePath);
            var s     = Program.env[envIndex].GetLocalStreamFromPackage(filePath);
            SendStreamResponse(context, s, fname);
        }

        public static List<string> securityUserName = new List<string>();
        public static List<string> securityUserPassword = new List<string>();

        public static string[] serverCertfileNames = null;
        public static X509Certificate2[] serverCerts = null;

        public static Dictionary<string, string> securityRightsAAS = null;

        public class securityRightsClass
        {
            public string name = null;
            public string value = null;
            public string role = null;
        }

        public static List<securityRightsClass> securityRights = null;

        public class securityRoleClass
        {
            public string rulePath = null;
            public string condition = null;
            public string name = null;
            public string objType = null;
            public string apiOperation = null;
            public object objReference = null;
            public string objPath = "";
            public string permission = null;
            public string kind = null;
            public Submodel submodel = null;
            public string semanticId = "";
            public SubmodelElementCollection usage = null;
            public int usageEnvIndex = -1;

            public securityRoleClass()
            {
            }
        }

        public static List<securityRoleClass> securityRole = null;

        public static string securityAccessRules()
        {
            string rules = "";

            foreach (var r in securityRole)
            {
                if (r.condition != null)
                    rules += r.condition;
                if (r.name != null)
                    rules += r.name;
                rules += "\t";
                if (r.kind != null)
                    rules += r.kind;
                rules += "\t";
                if (r.permission != null)
                    rules += r.permission;
                rules += "\t";
                if (r.objType != null)
                    rules += r.objType;
                rules += "\t";
                if (r.apiOperation != null)
                    rules += r.apiOperation;
                rules += "\t";
                if (r.objPath != null)
                    rules += r.objPath;
                rules += "\t";
                if (r.semanticId != null)
                    rules += r.semanticId;
                rules += "\t";
                if (r.rulePath != null)
                    rules += r.rulePath;
                rules += "\n";
            }

            return rules;
        }

        public static void securityInit()
        {
            withAuthentification = !Program.noSecurity;

            securityRole = new List<securityRoleClass>();

            int aascount = AasxServer.Program.env.Length;

            for (int i = 0; i < aascount; i++)
            {
                var env = AasxServer.Program.env[i];
                if (env != null)
                {
                    var aas = env.AasEnv.AssetAdministrationShells[0];
                    if (aas.Submodels != null && aas.Submodels.Count > 0)
                    {
                        foreach (var smr in aas.Submodels)
                        {
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm != null && sm.IdShort != null)
                            {
                                if (!sm.IdShort.ToLower().Contains("Security"))
                                {
                                    sm.SetAllParents();
                                }
                            }
                        }

                        foreach (var smr in aas.Submodels)
                        {
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm != null && sm.IdShort != null)
                            {
                                if (sm.IdShort == "SecuritySettingsForServer")
                                {
                                    int countSme = sm.SubmodelElements.Count;
                                    for (int iSme = 0; iSme < countSme; iSme++)
                                    {
                                        var sme = sm.SubmodelElements[iSme];
                                        if (sme is Property)
                                            continue;
                                        var smec      = sme as SubmodelElementCollection;
                                        int countSmec = smec.Value.Count;
                                        switch (smec.IdShort)
                                        {
                                            case "authenticationServer":
                                                for (int iSmec = 0; iSmec < countSmec; iSmec++)
                                                {
                                                    var sme2 = smec.Value[iSmec];
                                                    switch (sme2.IdShort)
                                                    {
                                                        case "endpoint":
                                                            var p2 = sme2 as Property;
                                                            AasxServer.Program.redirectServer = p2.Value;
                                                            break;
                                                        case "type":
                                                            var p3 = sme2 as Property;
                                                            AasxServer.Program.authType = p3.Value;
                                                            break;
                                                        case "publicCertificate":
                                                            var f = sme2 as AasCore.Aas3_0.File;
                                                            serverCertfileNames = new string[1];
                                                            serverCerts         = new X509Certificate2[1];
                                                            var s = Program.env[i].GetLocalStreamFromPackage(f.Value, init: true);
                                                            if (s != null)
                                                            {
                                                                using (var m = new MemoryStream())
                                                                {
                                                                    s.CopyTo(m);
                                                                    var b = m.GetBuffer();
                                                                    serverCerts[0] = new X509Certificate2(b);
                                                                    string[] split = f.Value.Split('/');
                                                                    serverCertfileNames[0] = split[3];
                                                                    Console.WriteLine("Loaded auth server certifcate: " + serverCertfileNames[0]);
                                                                }
                                                            }

                                                            break;
                                                    }
                                                }

                                                break;
                                            case "roleMapping":
                                                securityRights = new List<securityRightsClass>();

                                                for (int iSmec = 0; iSmec < countSmec; iSmec++)
                                                {
                                                    var          smec2      = smec.Value[iSmec] as SubmodelElementCollection;
                                                    int          countSmec2 = smec2.Value.Count;
                                                    List<string> subjects   = new List<string>();

                                                    for (int iSmec2 = 0; iSmec2 < countSmec2; iSmec2++)
                                                    {
                                                        var smec3      = smec2.Value[iSmec2] as SubmodelElementCollection;
                                                        int countSmec3 = smec3.Value.Count;

                                                        switch (smec3.IdShort)
                                                        {
                                                            case "subjects":
                                                                for (int iSmec3 = 0; iSmec3 < countSmec3; iSmec3++)
                                                                {
                                                                    var p = smec3.Value[iSmec3] as Property;
                                                                    switch (p.IdShort)
                                                                    {
                                                                        case "emailDomain":
                                                                        case "email":
                                                                            subjects.Add(p.Value);
                                                                            break;
                                                                        default:
                                                                            subjects.Add(p.IdShort);
                                                                            break;
                                                                    }
                                                                }

                                                                break;
                                                            case "roles":
                                                                for (int iSmec3 = 0; iSmec3 < countSmec3; iSmec3++)
                                                                {
                                                                    var p = smec3.Value[iSmec3] as Property;
                                                                    foreach (var s in subjects)
                                                                    {
                                                                        securityRightsClass sr = new securityRightsClass();
                                                                        sr.name = s;
                                                                        sr.role = p.IdShort;
                                                                        securityRights.Add(sr);
                                                                    }
                                                                }

                                                                break;
                                                        }
                                                    }
                                                }

                                                break;
                                            case "basicAuth":
                                                for (int iSmec = 0; iSmec < countSmec; iSmec++)
                                                {
                                                    if (smec.Value[iSmec] is Property p)
                                                    {
                                                        securityUserName.Add(p.IdShort);
                                                        securityUserPassword.Add(p.Value);
                                                    }
                                                }

                                                break;
                                        }
                                    }
                                }

                                if (sm.IdShort == "SecurityMetaModelForServer" || sm.IdShort == "SecurityMetaModelForAAS")
                                {
                                    //var smc1 = sm.SubmodelElements.FindFirstIdShortAs<SubmodelElementCollection>("accessControlPolicyPoints");
                                    var smc1 = sm.FindFirstIdShortAs<SubmodelElementCollection>("accessControlPolicyPoints");
                                    var smc2 = smc1?.FindFirstIdShortAs<SubmodelElementCollection>("policyAdministrationPoint");
                                    var smc3 = smc2?.FindFirstIdShortAs<SubmodelElementCollection>("localAccessControl");
                                    var smc4 = smc3?.FindFirstIdShortAs<SubmodelElementCollection>("accessPermissionRules");
                                    if (smc4 == null) continue;

                                    int countSme = smc4.Value.Count;
                                    for (int iSme = 0; iSme < countSme; iSme++)
                                    {
                                        var            sme   = smc4.Value[iSme]; // actual rule
                                        var            smc5  = sme as SubmodelElementCollection;
                                        var            smc6  = smc5?.FindFirstIdShortAs<SubmodelElementCollection>("targetSubjectAttributes");
                                        List<Property> role  = new List<Property>();
                                        int            iRole = 0;
                                        while (smc6?.Value.Count > iRole)
                                        {
                                            if (smc6?.Value[iRole] is Property rp)
                                            {
                                                role.Add(rp);
                                            }

                                            iRole++;
                                        }

                                        smc6 = smc5?.FindFirstIdShortAs<SubmodelElementCollection>("permissionsPerObject");
                                        var    smc7      = smc6?.Value[0] as SubmodelElementCollection;
                                        var    objProp   = smc7?.FindFirstIdShortAs<Property>("object");
                                        var    objRef    = smc7?.FindFirstIdShortAs<ReferenceElement>("object");
                                        object aasObject = null;
                                        if (objRef != null)
                                        {
                                            aasObject = env.AasEnv.FindReferableByReference(objRef.Value);
                                        }

                                        var smc8 = smc7?.FindFirstIdShortAs<SubmodelElementCollection>("permission");
                                        var smc9 = smc7?.FindFirstIdShortAs<SubmodelElementCollection>("usage");

                                        int          countSmc8      = smc8.Value.Count;
                                        List<string> listPermission = new List<string>();
                                        Property     kind           = null;
                                        for (int iSmc8 = 0; iSmc8 < countSmc8; iSmc8++)
                                        {
                                            var sme9 = smc8.Value[iSmc8];
                                            if (sme9 is Property)
                                                kind = sme9 as Property;
                                            if (sme9 is ReferenceElement)
                                            {
                                                var refer      = sme9 as ReferenceElement;
                                                var permission = env.AasEnv.FindReferableByReference(refer.Value);
                                                if (!(permission is Property))
                                                    continue;
                                                var p = permission as Property;
                                                listPermission.Add(p.IdShort);
                                            }
                                        }

                                        string[] split = null;
                                        foreach (var l in listPermission)
                                        {
                                            foreach (var r in role)
                                            {
                                                securityRoleClass src = new securityRoleClass();
                                                if (smc9 != null)
                                                {
                                                    src.usage         = smc9;
                                                    src.usageEnvIndex = i;
                                                }

                                                if (r.IdShort.Contains(":"))
                                                {
                                                    split         = r.IdShort.Split(':');
                                                    src.condition = split[0].ToLower();
                                                    src.name      = split[1];
                                                }
                                                else
                                                {
                                                    src.condition = "";
                                                    src.name      = r.IdShort;
                                                }

                                                if (objProp != null)
                                                {
                                                    string value = objProp.Value.ToLower();
                                                    src.objType = value;
                                                    if (value.Contains("api"))
                                                    {
                                                        split = value.Split(':');
                                                        if (split[0] == "api")
                                                        {
                                                            src.objType      = split[0];
                                                            src.apiOperation = split[1];
                                                        }
                                                    }

                                                    if (value.Contains("semanticid"))
                                                    {
                                                        split = value.Split(':');
                                                        if (split[0] == "semanticid")
                                                        {
                                                            src.objType    = split[0];
                                                            src.semanticId = split[1];
                                                            for (int j = 2; j < split.Length; j++)
                                                                src.semanticId += ":" + split[j];
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (aasObject != null)
                                                    {
                                                        src.objReference = aasObject;
                                                        if (aasObject is AssetAdministrationShell)
                                                            src.objType = "aas";
                                                        if (aasObject is Submodel)
                                                        {
                                                            src.objType  = "sm";
                                                            src.submodel = aasObject as Submodel;
                                                            src.objPath  = src.submodel.IdShort;
                                                        }

                                                        if (aasObject is ISubmodelElement smep)
                                                        {
                                                            IReferable rp = smep;
                                                            src.objType = "submodelElement";
                                                            string path = rp.IdShort;
                                                            while (rp.Parent != null)
                                                            {
                                                                rp   = (IReferable)rp.Parent;
                                                                path = rp.IdShort + "." + path;
                                                            }

                                                            src.submodel = rp as Submodel;
                                                            src.objPath  = path;
                                                        }
                                                    }
                                                }

                                                src.permission = l.ToUpper();
                                                if (kind != null)
                                                    src.kind = kind.Value.ToLower();
                                                src.rulePath = aas.IdShort + "." + sm.IdShort + "..." + smc4.IdShort + "." + sme.IdShort;
                                                securityRole.Add(src);
                                            }
                                        }

                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static X509Certificate2 serverCertFind(string authServerName)
        {
            if (serverCertfileNames != null)
            {
                for (int i = 0; i < serverCertfileNames.Length; i++)
                {
                    if (Path.GetFileName(serverCertfileNames[i]) == authServerName + ".cer")
                    {
                        return serverCerts[i];
                    }
                }
            }

            return null;
        }

        #region // Concept Descriptions

        public void EvalPutCd(IHttpContext context, string aasid)
        {
            dynamic res   = new ExpandoObject();
            int     index = -1;

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context, ref index);

                if (!checkAccessRights(context, accessrights, "/cds", "UPDATE"))
                {
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                return;
            }

            // access the AAS
            var findAasReturn = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (findAasReturn.aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with IdShort '{aasid}' found.");
                return;
            }

            // de-serialize CD
            ConceptDescription cd = null;
            try
            {
                cd = System.Text.Json.JsonSerializer.Deserialize<ConceptDescription>(context.Request.Payload);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                return;
            }

            // need id for idempotent behaviour
            if (cd.Id == null)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Identification of entity is (null); PUT cannot be performed.");
                return;
            }

            // datastructure update
            if (this.Packages[findAasReturn.iPackage] == null ||
                this.Packages[findAasReturn.iPackage].AasEnv == null /*|| this.Packages[findAasReturn.iPackage].AasEnv.Assets == null*/)
            {
                context.Response.SendResponse(HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }

            // add Submodel
            context.Server.Logger.Debug($"Adding ConceptDescription with IdShort {cd.IdShort ?? "--"} and id {cd.Id?.ToString() ?? "--"}");
            var existingCd = this.Packages[findAasReturn.iPackage].AasEnv.FindConceptDescriptionById(cd.Id);
            if (existingCd != null)
                this.Packages[findAasReturn.iPackage].AasEnv.ConceptDescriptions.Remove(existingCd);
            this.Packages[findAasReturn.iPackage].AasEnv.ConceptDescriptions.Add(cd);

            // simple OK
            Program.signalNewData(2);
            context.Response.StatusCode = HttpStatusCode.Ok;
            SendTextResponse(context, "OK" + ((existingCd != null) ? " (updated)" : " (new)"));
        }

        #endregion
    }
}