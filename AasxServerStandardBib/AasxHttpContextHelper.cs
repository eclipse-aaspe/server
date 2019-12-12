using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Text.RegularExpressions;

using Newtonsoft.Json;
using AdminShellNS;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.IO;
using System.Dynamic;
using Grapevine.Interfaces.Server;
using Grapevine.Server.Attributes;
using Grapevine.Shared;
using Grapevine.Server;

using Jose;
using Jose.jwe;
using Jose.netstandard1_4;

using System.Security.Cryptography;
using System.Security.Permissions;
using System.Security.Cryptography.X509Certificates;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). 
The Grapevine REST server framework is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

/* Please notice: the API and REST routes implemented in this version of the source code are not specified and standardised by the
specification Details of the Administration Shell. The hereby stated approach is solely the opinion of its author(s). */

namespace AasxRestServerLibrary
{
    public class AasxHttpContextHelper
    {
        public static String SwitchToAASX = "";
        public static String DataPath = ".";

        public AdminShell.PackageEnv [] Packages = null;

        public AasxHttpHandleStore IdRefHandleStore = new AasxHttpHandleStore();

        #region // Path helpers

        public bool PathEndsWith(string path, string tag)
        {
            return path.Trim().ToLower().TrimEnd(new char[] { '/' }).EndsWith(tag);
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

        public List<AasxHttpHandleIdentification> CreateHandlesFromQueryString (System.Collections.Specialized.NameValueCollection queryStrings)
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
                            var id = new AdminShell.Identification(vl[0], vl[1]);
                            var h = new AasxHttpHandleIdentification(id, "@"+k);
                            res.Add(h);
                        }
                    }
                }
                catch { }
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
                        var k = m2.Groups[1].ToString();
                        var idt = m2.Groups[2].ToString();
                        var ids = m2.Groups[3].ToString();

                        var id = new AdminShell.Identification(idt, ids);
                        var h = new AasxHttpHandleIdentification(id, "@" + k);
                        res.Add(h);
                    }
                }

            // done
            return res;
        }

        #endregion

        #region // Access package structures

        public AdminShell.AdministrationShell FindAAS(string aasid, System.Collections.Specialized.NameValueCollection queryStrings = null, string rawUrl = null)
        {
            AdminShell.AdministrationShell aas = null;
            int iPackage = -1;

            if (Packages == null)
                return null;

            if (Regex.IsMatch(aasid, @"^\d+$")) // only number, i.e. index
            {
                // Index
                int i = Convert.ToInt32(aasid);

                if (i > Packages.Length)
                    return null;

                if (Packages[i] == null || Packages[i].AasEnv == null || Packages[i].AasEnv.AdministrationShells == null
                    || Packages[i].AasEnv.AdministrationShells.Count < 1)
                    return null;

                aas = Packages[i].AasEnv.AdministrationShells[0];
                iPackage = i;
            }
            else
            {
                // Name
                if (aasid == "id")
                {
                    aas = Packages[0].AasEnv.AdministrationShells[0];
                    iPackage = 0;
                }
                else
                {
                    for (int i = 0; i < Packages.Length; i++)
                    {
                        if (Packages[i] != null)
                        {
                            if (Packages[i].AasEnv.AdministrationShells[0].idShort == aasid)
                            {
                                aas = Packages[i].AasEnv.AdministrationShells[0];
                                iPackage = i;
                                break;
                            }
                        }
                    }
                }
            }

            return aas;


            // trivial
            if (Packages[0] == null || Packages[0].AasEnv == null || Packages[0].AasEnv.AdministrationShells == null || Packages[0].AasEnv.AdministrationShells.Count < 1)
                return null;

            // default aas?
            if (aasid == null || aasid.Trim() == "" || aasid.Trim().ToLower() == "id")
                return Packages[0].AasEnv.AdministrationShells[0];

            // resolve an ID?
            // var specialHandles = this.CreateHandlesFromQueryString(queryStrings);
            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(aasid, specialHandles);
            if (handleId != null && handleId.identification != null)
                return Packages[0].AasEnv.FindAAS(handleId.identification);

            // no, iterate over idShort
            return Packages[0].AasEnv.FindAAS(aasid);
        }

        public AdminShell.SubmodelRef FindSubmodelRefWithinAas(AdminShell.AdministrationShell aas, string smid, System.Collections.Specialized.NameValueCollection queryStrings = null, string rawUrl = null)
        {
            // trivial
            if (Packages[0] == null || Packages[0].AasEnv == null || aas == null || smid == null || smid.Trim() == "")
                return null;

            // via handle
            // var specialHandles = this.CreateHandlesFromQueryString(queryStrings);
            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(smid, specialHandles);

            // no, iterate & find
            foreach (var smref in aas.submodelRefs)
            {
                if (handleId != null && handleId.identification != null)
                {
                    if (smref.MatchesTo(handleId.identification))
                        return smref;
                } 
                else
                {
                    var sm = this.Packages[0].AasEnv.FindSubmodel(smref);
                    if (sm != null && sm.idShort != null && sm.idShort.Trim().ToLower() == smid.Trim().ToLower())
                        return smref;
                }
            }

            // no
            return null;
        }

        public AdminShell.Submodel FindSubmodelWithinAas(AdminShell.AdministrationShell aas, string smid, System.Collections.Specialized.NameValueCollection queryStrings = null, string rawUrl = null)
        {
            // trivial
            if (Packages[0] == null || Packages[0].AasEnv == null || aas == null || smid == null || smid.Trim() == "")
                return null;

            // via handle
            // var specialHandles = this.CreateHandlesFromQueryString(queryStrings);
            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(smid, specialHandles);
            if (handleId != null && handleId.identification != null)
                return Packages[0].AasEnv.FindSubmodel(handleId.identification);

            // no, iterate & find
            
            foreach (var smref in aas.submodelRefs)
            {
                var sm = this.Packages[0].AasEnv.FindSubmodel(smref);
                if (sm != null && sm.idShort != null && sm.idShort.Trim().ToLower() == smid.Trim().ToLower())
                    return sm;
            }

            // no
            return null;
        }

        
        public AdminShell.Submodel FindSubmodelWithinAas(string aasid, string smid, System.Collections.Specialized.NameValueCollection queryStrings = null, string rawUrl = null)
        {
            AdminShell.AdministrationShell aas = null;
            int iPackage = -1;

            if (Packages == null)
                return null;

            if (Regex.IsMatch(aasid, @"^\d+$")) // only number, i.e. index
            {
                // Index
                int i = Convert.ToInt32(aasid);

                if (i > Packages.Length)
                    return null;

                if (Packages[i] == null || Packages[i].AasEnv == null || Packages[i].AasEnv.AdministrationShells == null
                    || Packages[i].AasEnv.AdministrationShells.Count < 1)
                    return null;

                aas = Packages[i].AasEnv.AdministrationShells[0];
                iPackage = i;
            }
            else
            {
                // Name
                if (aasid == "id")
                { 
                    aas = Packages[0].AasEnv.AdministrationShells[0];
                    iPackage = 0;
                }
                else
                {
                    for (int i = 0; i < Packages.Length; i++)
                    {
                        if (Packages[i] != null)
                        {
                            if (Packages[i].AasEnv.AdministrationShells[0].idShort == aasid)
                            {
                                aas = Packages[i].AasEnv.AdministrationShells[0];
                                iPackage = i;
                                break;
                            }
                        }
                    }
                }
            }

            if (aas == null)
                return null;

            // via handle
            // var specialHandles = this.CreateHandlesFromQueryString(queryStrings);
            /*
            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(smid, specialHandles);
            if (handleId != null && handleId.identification != null)
                return Packages[0].AasEnv.FindSubmodel(handleId.identification);
            */
            // no, iterate & find

            foreach (var smref in aas.submodelRefs)
            {
                var sm = this.Packages[iPackage].AasEnv.FindSubmodel(smref);
                if (sm != null && sm.idShort != null && sm.idShort.Trim().ToLower() == smid.Trim().ToLower())
                    return sm;
            }

            // no
            return null;
        }



        public AdminShell.Submodel FindSubmodelWithoutAas(string smid, System.Collections.Specialized.NameValueCollection queryStrings = null, string rawUrl = null)
        {
            // trivial
            if (Packages[0] == null || Packages[0].AasEnv == null || smid == null || smid.Trim() == "")
                return null;

            // via handle
            // var specialHandles = this.CreateHandlesFromQueryString(queryStrings);
            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(smid, specialHandles);
            if (handleId != null && handleId.identification != null)
                return Packages[0].AasEnv.FindSubmodel(handleId.identification);

            // no, iterate & find
            foreach (var sm in this.Packages[0].AasEnv.Submodels)
            {
                if (sm != null && sm.idShort != null && sm.idShort.Trim().ToLower() == smid.Trim().ToLower())
                    return sm;
            }

            // no
            return null;
        }

        public AdminShell.ConceptDescription FindCdWithoutAas(AdminShell.AdministrationShell aas, string cdid, System.Collections.Specialized.NameValueCollection queryStrings = null, string rawUrl = null)
        {
            // trivial
            if (Packages[0] == null || Packages[0].AasEnv == null || aas == null || cdid == null || cdid.Trim() == "")
                return null;

            // via handle
            // var specialHandles = this.CreateHandlesFromQueryString(queryStrings);
            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(cdid, specialHandles);
            if (handleId != null && handleId.identification != null)
                return Packages[0].AasEnv.FindConceptDescription(handleId.identification);

            // no, iterate & find
            foreach (var cd in Packages[0].AasEnv.ConceptDescriptions)
            {
                if (cd.idShort != null && cd.idShort.Trim().ToLower() == cdid.Trim().ToLower())
                    return cd;
            }

            // no
            return null;
        }


        public class FindSubmodelElementResult
        {
            public AdminShell.Referable elem = null;
            public AdminShell.SubmodelElementWrapper wrapper = null;
            public AdminShell.Referable parent = null;

            public FindSubmodelElementResult (AdminShell.Referable elem = null, AdminShell.SubmodelElementWrapper wrapper = null, AdminShell.Referable parent = null)
            {
                this.elem = elem;
                this.wrapper = wrapper;
                this.parent = parent;
            }
        }

        public FindSubmodelElementResult FindSubmodelElement(AdminShell.Referable parent, List<AdminShell.SubmodelElementWrapper> wrappers, string[] elemids, int elemNdx = 0)
        {
            // trivial
            if (wrappers == null || elemids == null || elemNdx >= elemids.Length)
                return null;

            // dive into each
            foreach (var smw in wrappers)
                if (smw.submodelElement != null)
                {
                    // idShort need to match
                    if (smw.submodelElement.idShort.Trim().ToLower() != elemids[elemNdx].Trim().ToLower())
                        continue;

                    // leaf
                    if (elemNdx == elemids.Length - 1)
                    {
                        return new FindSubmodelElementResult(elem: smw.submodelElement, wrapper: smw, parent: parent);
                    }
                    else
                    {
                        // recurse into?
                        var xsmc = smw.submodelElement as AdminShell.SubmodelElementCollection;
                        if (xsmc != null)
                        {
                            var r = FindSubmodelElement(xsmc, xsmc.value, elemids, elemNdx + 1);
                            if (r != null)
                                return r;
                        }

                        var xop = smw.submodelElement as AdminShell.Operation;
                        if (xop != null)
                        {
                            var w2 = new List<AdminShell.SubmodelElementWrapper>();
                            for (int i = 0; i < 2; i++)
                                foreach (var opv in xop[i])
                                    if (opv.value != null)
                                        w2.Add(opv.value);

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

        public class AdaptiveFilterContractResolver : DefaultContractResolver
        {
            public bool AasHasViews = true;
            public bool BlobHasValue = true;
            public bool SubmodelHasElements = true;
            public bool SmcHasValue = true;
            public bool OpHasVariables = true;

            public AdaptiveFilterContractResolver() { }

            public AdaptiveFilterContractResolver(bool deep = true, bool complete = true)
            {
                if (!deep)
                {
                    this.SubmodelHasElements = false;
                    this.SmcHasValue = false;
                    this.OpHasVariables = false;
                }
                if (!complete)
                {
                    this.AasHasViews = false;
                    this.BlobHasValue = false;
                }

            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                if (!BlobHasValue && property.DeclaringType == typeof(AdminShellV10.Blob) && property.PropertyName == "value")
                    property.ShouldSerialize = instance => { return false; };

                if (!SubmodelHasElements && property.DeclaringType == typeof(AdminShellV10.Submodel) && property.PropertyName == "submodelElements")
                    property.ShouldSerialize = instance => { return false; };

                if (!SmcHasValue && property.DeclaringType == typeof(AdminShellV10.SubmodelElementCollection) && property.PropertyName == "value")
                    property.ShouldSerialize = instance => { return false; };

                if (!OpHasVariables && property.DeclaringType == typeof(AdminShellV10.Operation) && (property.PropertyName == "in" || property.PropertyName == "out"))
                    property.ShouldSerialize = instance => { return false; };

                if (!AasHasViews && property.DeclaringType == typeof(AdminShellV10.AdministrationShell) && property.PropertyName == "views")
                    property.ShouldSerialize = instance => { return false; };

                return property;
            }
        }

        protected static void SendJsonResponse(Grapevine.Interfaces.Server.IHttpContext context, object obj, IContractResolver contractResolver = null)
        {
            var settings = new JsonSerializerSettings();
            if (contractResolver != null)
                settings.ContractResolver = contractResolver; 
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);
            var buffer = context.Request.ContentEncoding.GetBytes(json);
            var length = buffer.Length;

            context.Response.ContentType = ContentType.JSON;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = length;
            context.Response.SendResponse(buffer);
        }

        protected static void SendTextResponse(Grapevine.Interfaces.Server.IHttpContext context, string txt, string mimeType = null)
        {
            context.Response.ContentType = ContentType.TEXT;
            if (mimeType != null)
                context.Response.Advanced.ContentType = mimeType;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = txt.Length;
            context.Response.SendResponse(txt);
        }

        protected static void SendStreamResponse(IHttpContext context, Stream stream, string headerAttachmentFileName = null)
        {
            context.Response.ContentType = ContentType.APPLICATION;
            context.Response.ContentLength64 = stream.Length;
            context.Response.SendChunked = true;

            // context.Response.Advanced.ContentType = "application/pdf";
            if (headerAttachmentFileName != null)
                context.Response.AddHeader("Content-Disposition", $"attachment; filename={headerAttachmentFileName}");

            stream.CopyTo(context.Response.Advanced.OutputStream);
            context.Response.Advanced.Close();
        }

        #endregion

        #region AAS and Asset

        public void EvalGetAasAndAsset(IHttpContext context, string aasid, bool deep = false, bool complete = false)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access the first AAS
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with id '{aasid}' found.");
                return;
            }

            // try to get the asset as well
            var asset = this.Packages[0].AasEnv.FindAsset(aas.assetRef);

            // result
            res.AAS = aas;
            res.Asset = asset;

            // return as JSON
            var cr = new AdaptiveFilterContractResolver(deep: deep, complete: complete);
            SendJsonResponse(context, res, cr);
        }

        public void EvalGetAasEnv(IHttpContext context, string aasid)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
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
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with id '{aasid}' found.");
                return;
            }

            // OZ
            // return as JSON
            /*
            dynamic res = new ExpandoObject();
            res.AAS = aas;
            var cr = new AdaptiveFilterContractResolver(deep: true, complete: true);
            SendJsonResponse(context, res, cr);

            return;
            */

            // create a new, filtered AasEnv
            var copyenv = AdminShell.AdministrationShellEnv.CreateFromExistingEnv(this.Packages[0].AasEnv, filterForAas: new List<AdminShell.AdministrationShell>(new AdminShell.AdministrationShell[] { aas }));

            // return as FILE
            try
            {
                using (var ms = new MemoryStream())
                {
                    // build a file name
                    var fn = "aasenv.json";
                    if (aas.idShort != null)
                        fn = aas.idShort + "." + fn;
                    // serialize via helper
                    var jsonwriter = copyenv.SerialiazeJsonToStream(new StreamWriter(ms), leaveJsonWriterOpen: true);
                    // write out again
                    ms.Position = 0;
                    SendStreamResponse(context, ms, Path.GetFileName(fn));
                    // bit ugly
                    jsonwriter.Close();
                }
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot serialize and send aas envioronment: {ex.Message}.");
                return;
            }
        }


        public void EvalGetAasThumbnail(IHttpContext context, string aasid)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
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
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with id '{aasid}' found.");
                return;
            }

            // access the thumbnail
            // Note: in this version, the thumbnail is not specific to the AAS, but maybe in later versions ;-)
            Uri thumbUri = null;
            var thumbStream = this.Packages[0].GetLocalThumbnailStream(ref thumbUri);
            if (thumbStream == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No thumbnail available in package.");
                return;
            }

            // return as FILE
            SendStreamResponse(context, thumbStream, Path.GetFileName(thumbUri.ToString() ?? ""));
            thumbStream.Close();
        }

        public void EvalPutAas(IHttpContext context)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
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
            AdminShell.AdministrationShell aas = null;
            try
            {
                aas = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.AdministrationShell>(context.Request.Payload);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                return;
            }

            // need id for idempotent behaviour
            if (aas.identification == null)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Identification of entity is (null); PUT cannot be performed.");
                return;
            }

            // datastructure update
            if (this.Packages[0] == null || this.Packages[0].AasEnv == null || this.Packages[0].AasEnv.AdministrationShells == null)
            {
                context.Response.SendResponse(HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }
            context.Server.Logger.Debug($"Putting AdministrationShell with idShort {aas.idShort ?? "--"} and id {aas.identification?.ToString() ?? "--"}");
            var existingAas = this.Packages[0].AasEnv.FindAAS(aas.identification);
            if (existingAas != null)
                this.Packages[0].AasEnv.AdministrationShells.Remove(existingAas);
            this.Packages[0].AasEnv.AdministrationShells.Add(aas);

            // simple OK
            SendTextResponse(context, "OK" + ((existingAas != null) ? " (updated)" : " (new)"));
        }

        public void EvalDeleteAasAndAsset(IHttpContext context, string aasid, bool deleteAsset = false)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // datastructure update
            if (this.Packages[0] == null || this.Packages[0].AasEnv == null || this.Packages[0].AasEnv.AdministrationShells == null)
            {
                context.Response.SendResponse(HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }

            // access the AAS
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with idShort '{aasid}' found.");
                return;
            }

            // find the asset
            var asset = this.Packages[0].AasEnv.FindAsset(aas.assetRef);

            // delete
            context.Server.Logger.Debug($"Deleting AdministrationShell with idShort {aas.idShort ?? "--"} and id {aas.identification?.ToString() ?? "--"}");
            this.Packages[0].AasEnv.AdministrationShells.Remove(aas);

            if (deleteAsset && asset != null)
            {
                context.Server.Logger.Debug($"Deleting Asset with idShort {asset.idShort ?? "--"} and id {asset.identification?.ToString() ?? "--"}");
                this.Packages[0].AasEnv.Assets.Remove(asset);
            }

            // simple OK
            SendTextResponse(context, "OK");
        }

        #endregion

        #region // Asset links

        public void EvalGetAssetLinks(IHttpContext context, string assetid)
        {
            dynamic res1 = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res1.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res1);
                    return;
                }

                res1.confirm = "Authorization = " + accessrights;
            }

            // trivial
            if (assetid == null)
                return;

            // do a manual search
            var res = new List<ExpandoObject>();
            var specialHandles = this.CreateHandlesFromQueryString(context.Request.QueryString);
            var handle = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(assetid, specialHandles);
            if (handle != null && handle.identification != null)
            {
                foreach (var aas in this.Packages[0].AasEnv.AdministrationShells)
                    if (aas.assetRef != null && aas.assetRef.MatchesTo(handle.identification))
                    {
                        dynamic o = new ExpandoObject();
                        o.identification = aas.identification;
                        o.idShort = aas.idShort;
                        res.Add(o);
                    }
            }
            else
            {
                foreach (var aas in this.Packages[0].AasEnv.AdministrationShells)
                    if (aas.idShort != null && aas.idShort.Trim() != "" && aas.idShort.Trim().ToLower() == assetid.Trim().ToLower())
                    {
                        dynamic o = new ExpandoObject();
                        o.identification = aas.identification;
                        o.idShort = aas.idShort;
                        res.Add(o);
                    }
            }
                        
            // return as JSON
            SendJsonResponse(context, res);
        }

        public void EvalPutAsset(IHttpContext context)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
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
            AdminShell.Asset asset = null;
            try
            {
                asset = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.Asset>(context.Request.Payload);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                return;
            }

            // need id for idempotent behaviour
            if (asset.identification == null)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Identification of entity is (null); PUT cannot be performed.");
                return;
            }

            // datastructure update
            if (this.Packages[0] == null || this.Packages[0].AasEnv == null || this.Packages[0].AasEnv.Assets == null)
            {
                context.Response.SendResponse(HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }
            context.Server.Logger.Debug($"Adding Asset with idShort {asset.idShort ?? "--"}");
            var existingAsset = this.Packages[0].AasEnv.FindAsset(asset.identification);
            if (existingAsset != null)
                this.Packages[0].AasEnv.Assets.Remove(existingAsset);
            this.Packages[0].AasEnv.Assets.Add(asset);

            // simple OK
            SendTextResponse(context, "OK" + ((existingAsset != null) ? " (updated)" : " (new)"));
        }
        #endregion

        #region // List of Submodels

        public class GetSubmodelsItem
        {
            public AdminShell.Identification id = new AdminShellV10.Identification();
            public string idShort = "";
            public string kind = "";

            public GetSubmodelsItem() { }

            public GetSubmodelsItem(AdminShell.Identification id, string idShort, string kind)
            {
                this.id = id;
                this.idShort = idShort;
                this.kind = kind;
            }

            public GetSubmodelsItem(AdminShell.Identifiable idi, string kind)
            {
                this.id = idi.identification;
                this.idShort = idi.idShort;
                this.kind = kind;
            }
        }

        public void EvalGetSubmodels(IHttpContext context, string aasid)
        {
            dynamic res1 = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res1.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res1);
                    return;
                }

                res1.confirm = "Authorization = " + accessrights;
            }

            // access the AAS
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with idShort '{aasid}' found.");
                return;
            }

            // build a list of results
            var res = new List<GetSubmodelsItem>();

            // get all submodels
            foreach (var smref in aas.submodelRefs)
            {
                var sm = this.Packages[0].AasEnv.FindSubmodel(smref);
                if (sm != null)
                {
                    res.Add(new GetSubmodelsItem(sm, sm.kind.kind));
                }
            }

            // return as JSON
            SendJsonResponse(context, res);
        }

        static long countPut = 0;
        public void EvalPutSubmodel(IHttpContext context, string aasid)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
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
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with idShort '{aasid}' found.");
                Console.WriteLine("ERROR PUT: No AAS with idShort '{0}' found.", aasid);
                return;
            }

            // de-serialize Submodel
            AdminShell.Submodel submodel = null;
            try
            {
                // submodel = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.Submodel>(context.Request.Payload);
                using (TextReader reader = new StringReader(context.Request.Payload))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Converters.Add(new AdminShell.JsonAasxConverter("modelType", "name"));
                    submodel = (AdminShell.Submodel)serializer.Deserialize(reader, typeof(AdminShell.Submodel));
                }
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                Console.WriteLine("ERROR PUT: Cannot deserialize payload.");
                return;
            }

            // need id for idempotent behaviour
            if (submodel.identification == null)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Identification of entity is (null); PUT cannot be performed.");
                Console.WriteLine("ERROR PUT: Identification of entity is (null); PUT cannot be performed.");
                return;
            }

            // datastructure update
            if (this.Packages[0] == null || this.Packages[0].AasEnv == null || this.Packages[0].AasEnv.Assets == null)
            {
                context.Response.SendResponse(HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }

            // add Submodel
            context.Server.Logger.Debug($"Adding Submodel with idShort {submodel.idShort ?? "--"} and id {submodel.identification?.ToString() ?? "--"}");
            var existingSm = this.Packages[0].AasEnv.FindSubmodel(submodel.identification);
            if (existingSm != null)
                this.Packages[0].AasEnv.Submodels.Remove(existingSm);
            this.Packages[0].AasEnv.Submodels.Add(submodel);

            // add SubmodelRef to AAS            
            var newsmr = AdminShell.SubmodelRef.CreateNew("Submodel", true, submodel.identification.idType, submodel.identification.id);
            var existsmr = aas.HasSubmodelRef(newsmr);
            if (!existsmr)
            {
                context.Server.Logger.Debug($"Adding SubmodelRef to AAS with idShort {aas.idShort ?? "--"} and id {aas.identification?.ToString() ?? "--"}");
                aas.AddSubmodelRef(newsmr);
            }

            Console.WriteLine("{0} Received PUT Submodel {1}", countPut++, submodel.idShort);

            // simple OK
            SendTextResponse(context, "OK" + ((existingSm != null) ? " (updated)" : " (new)"));
        }

        public void EvalDeleteSubmodel(IHttpContext context, string aasid, string smid)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
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
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with idShort '{aasid}' found.");
                return;
            }

            // delete SubmodelRef 1st
            var smref = this.FindSubmodelRefWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            if (smref != null)
            {
                context.Server.Logger.Debug($"Removing SubmodelRef {smid} from AAS with idShort {aas.idShort ?? "--"} and id {aas.identification?.ToString() ?? "--"}");
                aas.submodelRefs.Remove(smref);
            }

            // delete Submodel 2nd
            var sm = this.FindSubmodelWithoutAas(smid, context.Request.QueryString, context.Request.RawUrl);
            if (sm != null)
            {
                context.Server.Logger.Debug($"Removing Submodel {smid} from data structures.");
                this.Packages[0].AasEnv.Submodels.Remove(sm);
            }

            // simple OK
            var cmt = "";
            if (smref == null && sm == null)
                cmt += " (nothing deleted)";
            cmt += ((smref != null) ? " (SubmodelRef deleted)" : "") + ((sm != null) ? " (Submodel deleted)" : "");
            SendTextResponse(context, "OK" + cmt);
        }

        #endregion

        #region // Submodel Complete
        static long countGet = 0;

        public void EvalGetSubmodelContents(IHttpContext context, string aasid, string smid, bool deep = false, bool complete = false)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            // var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            // var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);
            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
                return;
            }

            Console.WriteLine("{0} Received GET Submodel {1}", countGet++, sm.idShort);

            // return as JSON
            var cr = new AdaptiveFilterContractResolver(deep: deep, complete: complete);
            SendJsonResponse(context, sm, cr);
        }

        public void EvalGetSubmodelContentsAsTable(IHttpContext context, string aasid, string smid)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            // var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            // var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
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
                row.idShorts = "";
                row.typeName = "";
                row.semIdType = "";
                row.semId = "";
                row.shortName = "";
                row.unit = "";
                row.value = "";

                // idShort is a concatenation
                var path = "";
                foreach (var p in parents)
                    path += p.idShort + "/";

                // SubnmodelElement general
                row.idShorts = path + sme.idShort ?? "(-)";
                row.typeName = sme.GetElementName();
                if (sme.semanticId == null || sme.semanticId.Keys == null)
                { }
                else if (sme.semanticId.Keys.Count > 1)
                {
                    row.semId = "(complex)";
                }
                else
                {
                    row.semIdType = sme.semanticId.Keys[0].idType;
                    row.semId = sme.semanticId.Keys[0].value;
                }

                // try find a concept description
                if (sme.semanticId != null)
                {
                    var cd = this.Packages[0].AasEnv.FindConceptDescription(sme.semanticId.Keys);
                    if (cd != null)
                    {
                        var ds = cd.GetIEC61360();
                        if (ds != null)
                        {
                            row.shortName = ds.shortName ?? "";
                            row.unit = ds.unit ?? "";
                        }
                    }
                }

                // try add a value
                if (sme is AdminShell.Property)
                {
                    var p = sme as AdminShell.Property;
                    row.value = "" + (p.value ?? "") + ((p.valueId != null) ? p.valueId.ToString() : "");
                }

                if (sme is AdminShell.File)
                {
                    var p = sme as AdminShell.File;
                    row.value = "" + p.value;
                }

                if (sme is AdminShell.Blob)
                {
                    var p = sme as AdminShell.Blob;
                    if (p.value.Length < 128)
                        row.value = "" + p.value;
                    else
                        row.value = "(" + p.value.Length + " bytes)";
                }

                if (sme is AdminShell.ReferenceElement)
                {
                    var p = sme as AdminShell.ReferenceElement;
                    row.value = "" + p.value.ToString();
                }

                if (sme is AdminShell.RelationshipElement)
                {
                    var p = sme as AdminShell.RelationshipElement;
                    row.value = "" + (p.first?.ToString() ?? "(-)") + " <-> " + (p.second?.ToString() ?? "(-)");
                }

                // now, add the row
                table.Add(row);

            });

            // return as JSON
            SendJsonResponse(context, table);
        }

        #endregion

        #region // Submodel Elements

        public void EvalGetSubmodelElementContents(IHttpContext context, string aasid, string smid, string[] elemids, bool deep = false, bool complete = false)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            // var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            // var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
                return;
            }

            // find the right SubmodelElement
            var sme = this.FindSubmodelElement(sm, sm.submodelElements, elemids);
            if (sme == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No matching element in Submodel found.");
                return;
            }

            // return as JSON
            var cr = new AdaptiveFilterContractResolver( deep: deep, complete: complete);
            SendJsonResponse(context, sme, cr);
        }

        public void EvalGetSubmodelElementsBlob(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            // var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            // var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
                return;
            }

            // find the right SubmodelElement
            var fse = this.FindSubmodelElement(sm, sm.submodelElements, elemids);
            var smeb = fse?.elem as AdminShell.Blob;
            if (smeb == null || smeb.value == null || smeb.value == "")
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No matching Blob element in Submodel found.");
                return;
            }

            // return as TEXT
            SendTextResponse(context, smeb.value, mimeType: smeb.mimeType);
        }

        public void EvalGetSubmodelElementsProperty(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            // var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            // var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
                return;
            }

            // find the right SubmodelElement
            var fse = this.FindSubmodelElement(sm, sm.submodelElements, elemids);
            var smep = fse?.elem as AdminShell.Property;
            if (smep == null || smep.value == null || smep.value == "")
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No matching Property element in Submodel found.");
                return;
            }

            // return as little dynamic object
            // dynamic res = new ExpandoObject();
            res.value = smep.value;
            if (smep.valueId != null)
                res.valueId = smep.valueId;
            SendJsonResponse(context, res);
        }

        public void EvalGetSubmodelElementsFile(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            // var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            // var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
                return;
            }

            // find the right SubmodelElement
            var fse = this.FindSubmodelElement(sm, sm.submodelElements, elemids);
            var smef = fse?.elem as AdminShell.File;
            if (smef == null || smef.value == null || smef.value == "")
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No matching File element in Submodel found.");
                return;
            }

            // access
            var packageStream = this.Packages[0].GetLocalStreamFromPackage(smef.value);
            if (packageStream == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No file contents available in package.");
                return;
            }

            // return as FILE
            SendStreamResponse(context, packageStream, Path.GetFileName(smef.value));
            packageStream.Close();
        }

        public void EvalPutSubmodelElementContents(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
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
            AdminShell.SubmodelElement sme = null;
            try
            {
                // JsonSerializer serializer = new JsonSerializer();
                // serializer.Converters.Add(new AdminShell.JsonAasxConverter("modelType", "name"));
                // this.aasenv = (AdministrationShellEnv)serializer.Deserialize(file, typeof(AdministrationShellEnv));
                sme = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.SubmodelElement>(context.Request.Payload, new AdminShell.JsonAasxConverter("modelType", "name"));
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                return;
            }

            // need id for idempotent behaviour
            if (sme.idShort == null)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"idShort of entity is (null); PUT cannot be performed.");
                return;
            }

            // access AAS and Submodel
            // var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            // var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
                return;
            }

            // special case: parent is Submodel itself
            var updated = false;
            if (elemids == null || elemids.Length < 1)
            {
                var existsmw = sm.FindSubmodelElementWrapper(sme.idShort);
                if (existsmw != null)
                {
                    updated = true;
                    context.Server.Logger.Debug($"Removing old SubmodelElement {sme.idShort} from Submodel {smid}.");
                    sm.submodelElements.Remove(existsmw);
                }

                context.Server.Logger.Debug($"Adding new SubmodelElement {sme.idShort} to Submodel {smid}.");
                sm.Add(sme);
            }
            else
            {
                // find the right SubmodelElement
                var parent = this.FindSubmodelElement(sm, sm.submodelElements, elemids);
                if (parent == null)
                {
                    context.Response.SendResponse(HttpStatusCode.NotFound, $"No matching element in Submodel found.");
                    return;
                }

                if (parent.elem != null && parent.elem is AdminShell.SubmodelElementCollection)
                {
                    var parentsmc = parent.elem as AdminShell.SubmodelElementCollection;
                    var existsmw = parentsmc.FindSubmodelElementWrapper(sme.idShort);
                    if (existsmw != null)
                    {
                        updated = true;
                        context.Server.Logger.Debug($"Removing old SubmodelElement {sme.idShort} from SubmodelCollection.");
                        parentsmc.value.Remove(existsmw);
                    }

                    context.Server.Logger.Debug($"Adding new SubmodelElement {sme.idShort} to SubmodelCollection.");
                    parentsmc.Add(sme);
                }
                else
                {
                    context.Response.SendResponse(HttpStatusCode.BadRequest, $"Matching SubmodelElement in Submodel {smid} is not suitable to add childs.");
                    return;
                }

            }

            // simple OK
            SendTextResponse(context, "OK" + (updated ? " (with updates)" : ""));
        }

        public void EvalDeleteSubmodelElementContents(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            // var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            // var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null || elemids == null || elemids.Length < 1)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found or no elements to delete specified.");
                return;
            }

            // OK, Submodel and Element existing
            var fse = this.FindSubmodelElement(sm, sm.submodelElements, elemids);
            if (fse == null || fse.elem == null || fse.parent == null || fse.wrapper == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No matching element in Submodel found.");
                return;
            }

            // where to delete?
            var deleted = false;
            var elinfo = string.Join(".", elemids);
            if (fse.parent == sm)
            {
                context.Server.Logger.Debug($"Deleting specified SubmodelElement {elinfo} from Submodel {smid}.");
                sm.submodelElements.Remove(fse.wrapper);
                deleted = true;
            }

            if (fse.parent is AdminShell.SubmodelElementCollection)
            {
                var smc = fse.parent as AdminShell.SubmodelElementCollection;
                context.Server.Logger.Debug($"Deleting specified SubmodelElement {elinfo} from SubmodelElementCollection {smc.idShort}.");
                smc.value.Remove(fse.wrapper);
                deleted = true;
            }

            // simple OK
            SendTextResponse(context, "OK" + (!deleted ? " (but nothing deleted)" : ""));
        }

        public void EvalInvokeSubmodelElementOperation(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and Submodel
            // var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            // var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aasid, smid, context.Request.QueryString, context.Request.RawUrl);

            if (sm == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
                return;
            }

            // find the right SubmodelElement
            var fse = this.FindSubmodelElement(sm, sm.submodelElements, elemids);
            var smep = fse?.elem as AdminShell.Operation;
            if (smep == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No matching Operation element in Submodel found.");
                return;
            }

            // make 1st expectation
            int numExpectedInputArgs = smep.valueIn?.Count ?? 0;
            int numGivenInputArgs = 0;
            int numExpectedOutputArgs = smep.valueOut?.Count ?? 0;
            var inputArguments = (new int[numExpectedInputArgs]).Select(x => "").ToList();
            var outputArguments = (new int[numExpectedOutputArgs]).Select(x => "my value").ToList();

            // is a payload required? Always, if at least one input argument required

            if (smep.valueIn != null && smep.valueIn.Count > 0)
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
                    var input = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(context.Request.Payload);

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
            if (true)
            {
                for (int i = 0; i < Math.Min(numExpectedInputArgs, numExpectedOutputArgs); i++)
                    outputArguments[i] = "CALC on " + inputArguments[i];
            }

            // return as little dynamic object
            SendJsonResponse(context, outputArguments);
        }

        public void EvalGetAllCds(IHttpContext context, string aasid)
        {
            dynamic res1 = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res1.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res1);
                    return;
                }

                res1.confirm = "Authorization = " + accessrights;
            }

            // access the AAS
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with idShort '{aasid}' found.");
                return;
            }

            // build a list of results
            var res = new List<ExpandoObject>();

            // create a new, filtered AasEnv
            // (this is expensive, but delivers us with a list of CDs which are in relation to the respective AAS)
            var copyenv = AdminShell.AdministrationShellEnv.CreateFromExistingEnv(this.Packages[0].AasEnv, filterForAas: new List<AdminShell.AdministrationShell>(new AdminShell.AdministrationShell[] { aas }));

            // get all CDs and describe them
            foreach (var cd in copyenv.ConceptDescriptions)
            {
                // describe
                dynamic o = new ExpandoObject();
                o.idShort = cd.idShort;
                o.shortName = cd.GetShortName();
                o.identification = cd.identification;
                o.isCaseOf = cd.IsCaseOf;

                // add
                res.Add(o);
            }

            // return as JSON
            SendJsonResponse(context, res);
        }

        public void EvalGetCdContents(IHttpContext context, string aasid, string cdid)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and CD
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            var cd = this.FindCdWithoutAas(aas, cdid, context.Request.QueryString, context.Request.RawUrl);
            if (cd == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no ConceptDescription with id '{cdid}' found.");
                return;
            }

            // return as JSON
            SendJsonResponse(context, cd);
        }

        public void EvalDeleteSpecificCd(IHttpContext context, string aasid, string cdid)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // access AAS and CD
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            var cd = this.FindCdWithoutAas(aas, cdid, context.Request.QueryString, context.Request.RawUrl);
            if (cd == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS '{aasid}' or no ConceptDescription with id '{cdid}' found.");
                return;
            }

            // delete ?!
            var deleted = false;
            if (this.Packages[0] != null && this.Packages[0].AasEnv != null && this.Packages[0].AasEnv.ConceptDescriptions.Contains(cd))
            {
                this.Packages[0].AasEnv.ConceptDescriptions.Remove(cd);
                deleted = true;
            }

            // return as JSON
            SendTextResponse(context, "OK" + (!deleted ? " (but nothing deleted)" : ""));
        }

        #endregion

        #region // GET + POST handles/identification

        public void EvalGetHandlesIdentification(IHttpContext context)
        {
            dynamic res1 = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res1.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res1);
                    return;
                }

                res1.confirm = "Authorization = " + accessrights;
            }

            // get the list
            var res = IdRefHandleStore.FindAll<AasxHttpHandleIdentification>();

            // return this list
            SendJsonResponse(context, res);
        }

        public void EvalPostHandlesIdentification(IHttpContext context)
        {
            dynamic res1 = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res1.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res1);
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
            List<AdminShell.Identification> ids = null;
            try
            {
                ids = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AdminShell.Identification>>(context.Request.Payload);
            } catch (Exception ex)
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
            /*
            if (ids[0].idType == "AASX")
            {
                SwitchToAASX = ids[0].id;
                Net46ConsoleServer.MySampleServer.quitEvent.Set();
                SendTextResponse(context, "switching done");
                return;
            }
            */

            // turn these list into a list of Handles
            var res = new List<AasxHttpHandleIdentification>();
            foreach (var id in ids)
            {
                var h = new AasxHttpHandleIdentification(id);
                IdRefHandleStore.Add(h);
                res.Add(h);
            }

            // return this list
            SendJsonResponse(context, res);
        }

        #endregion

        #region // Server profile ..

        public void EvalGetServerProfile(IHttpContext context)
        {
            dynamic res1 = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res1.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res1);
                    return;
                }

                res1.confirm = "Authorization = " + accessrights;
            }

            // get the list
            dynamic res = new ExpandoObject();
            var capabilities = new List<ulong>(new ulong[]{
                80,81,82,10,11,12,13,15,16,20,21,30,31,40,41,42,43,50,51,52,53,54,55,56,57,58,59,60,61,70,71,72,73
            });
            res.apiversion = 1;
            res.capabilities = capabilities;

            // return this list
            SendJsonResponse(context, res);
        }

        public static bool withAuthentification = false;

        public static string GuestToken = null;

        public static string secretString = "Industrie4.0-Asset-Administration-Shell";

        public void EvalPostAuthenticateGuest(IHttpContext context)
        {
            Console.WriteLine();
            Console.WriteLine("AuthenticateUser Guest");

            string payload0 = context.Request.Payload;

            dynamic res = new ExpandoObject();

            var payload = new Dictionary<string, object>()
            {
                { "user", "guest" },
                { "exp", 2023 }
            };

            if (GuestToken == null)
            {
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                GuestToken = Jose.JWT.Encode(payload, enc.GetBytes(secretString), JwsAlgorithm.HS256);
            }
            else
            {
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                GuestToken = Jose.JWT.Encode(payload, enc.GetBytes(GuestToken), JwsAlgorithm.HS256);
            }

            Console.WriteLine("JWT: " + GuestToken);

            withAuthentification = true;

            res.user = "guest";
            res.token = GuestToken;

            SendJsonResponse(context, res);
        }

        public void EvalPostAuthenticateUser(IHttpContext context)
        {
            string token = null;
            string user = null;
            string publicKey = null;
            dynamic res = new ExpandoObject();
            bool JWTfound = false;

            var parsed = JObject.Parse(context.Request.Payload);

            try
            {
                token = parsed.SelectToken("token").Value<string>();
            }
            catch
            {
                // no token in request = user/password
                token = null;
            }

            if (token != null && token != "")
            {
                Console.WriteLine();
                Console.WriteLine("AuthenticateUser token");
                var parsed2 = JObject.Parse(Jose.JWT.Payload(token));

                user = parsed2.SelectToken("user").Value<string>();

                X509Store store = new X509Store("My");
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection collection = store.Certificates;
                X509Certificate2Collection fcollection = (X509Certificate2Collection)collection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

                foreach (X509Certificate2 x509 in fcollection)
                {
                    try
                    {
                        string simpleName = x509.GetNameInfo(X509NameType.SimpleName, true);
                        if (simpleName == user)
                        {
                            publicKey = x509.PublicKey.Key.ToXmlString(false);
                            x509.Reset();
                            break;
                        }
                    }
                    catch (CryptographicException)
                    {
                        res.error = "User not authorized!";
                        SendJsonResponse(context, res);
                        return;
                    }
                }
                store.Close();

                if (publicKey != null)
                {
                    string json = null;

                    try
                    {
                        System.Text.ASCIIEncoding enc1 = new System.Text.ASCIIEncoding();
                        json = Jose.JWT.Decode(token, enc1.GetBytes(publicKey));
                    }
                    catch (Exception ex)
                    {
                        res.error = "User not authorized!";
                        SendJsonResponse(context, res);
                        return;
                    }

                    JWTfound = true;
                }
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("AuthenticateUser Password");

                user = parsed.SelectToken("user").Value<string>();
                string password = parsed.SelectToken("password").Value<string>();

                int userCount = securityUserName.Length;

                for (int i = 0; i < userCount; i++)
                {
                    if (user == securityUserName[i] && password == securityUserPassword[i])
                    {
                        JWTfound = true;
                        break;
                    }
                }
            }

            if (!JWTfound)
            {
                res.error = "User not authorized!";
                SendJsonResponse(context, res);
                return;
            }

            var payload = new Dictionary<string, object>()
            {
                { "user", user },
                { "exp", 2023 }
            };

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            token = Jose.JWT.Encode(payload, enc.GetBytes(secretString), JwsAlgorithm.HS256);

            withAuthentification = true;

            res.user = user;
            res.token = token;

            SendJsonResponse(context, res);
        }

        public string SecurityCheck(IHttpContext context)

        {
            string accessrights = null;

            // return access right: null = no rights
            if (GuestToken != null || withAuthentification)
            {
                string[] split = null;
                string receivedToken = null;
                Boolean JWTfound = false;

                Console.WriteLine();
                Console.WriteLine("Check Authorization");
                string headers = context.Request.Headers.ToString();
                // Console.WriteLine("Headers = " + headers);
                var token = context.Request.Headers.Get("Authorization");
                if (token != null)
                {
                    split = token.Split(new Char[] { ' ', '\t' });
                    if (split[0] != null)
                    {
                        if (split[0].ToLower() == "bearer")
                        {
                            Console.WriteLine("Received bearer token = " + split[1]);
                            receivedToken = split[1];
                        }
                    }
                }

                if (receivedToken == null)
                {
                    split = context.Request.Url.ToString().Split(new char[] { '?' });
                    if (split != null && split.Length > 1 && split[1] != null)
                    {
                        Console.WriteLine("Received query string = " + split[1]);
                        receivedToken = split[1];
                    }
                }

                if (receivedToken != null)
                {
                    if (receivedToken == GuestToken)
                    {
                        accessrights = "GUEST";
                        JWTfound = true;
                    }
                    else
                    {
                        bool integrityExc = false;
                        string json = null;

                        try
                        {
                            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                            json = Jose.JWT.Decode(receivedToken, enc.GetBytes(secretString));
                        }
                        catch (Exception ex)
                        {
                            integrityExc = true;
                        }

                        if (!integrityExc && json != null)
                        {
                            var parsed = JObject.Parse(json);
                            string user = parsed.SelectToken("user").Value<string>();
                            string exp = parsed.SelectToken("exp").Value<string>();
                            string domain = null;

                            split = user.Split(new char[] { '@' });
                            if (split != null && split.Length > 1 && split[1] != null)
                            {
                                domain = split[1];
                            }

                            if (user == "guest")
                            {
                                if (receivedToken == GuestToken)
                                {
                                    accessrights = "GUEST";
                                    JWTfound = true;
                                }
                            }
                            else // Check user authorization
                            {
                                int rightsCount = securityRightsName.Length;

                                for (int i = 0; i < rightsCount; i++)
                                {
                                    if (user == securityRightsName[i])
                                    {
                                        accessrights = securityRightsValue[i];
                                        JWTfound = true;
                                        break;
                                    }
                                    if (domain == securityRightsName[i])
                                    {
                                        accessrights = securityRightsValue[i];
                                        JWTfound = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return accessrights;
        }

        public void EvalGetListAAS(IHttpContext context)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // get the list
            var aaslist = new List<string>();

            int aascount = Net46ConsoleServer.Program.env.Length;

            for (int i = 0; i < aascount; i++)
            {
                if (Net46ConsoleServer.Program.env[i] != null)
                {
                    aaslist.Add(i.ToString() + " : "
                        + Net46ConsoleServer.Program.env[i].AasEnv.AdministrationShells[0].idShort + " : "
                        + Net46ConsoleServer.Program.env[i].AasEnv.AdministrationShells[0].identification);
                }
            }

            res.aaslist = aaslist;

            // return this list
            SendJsonResponse(context, res);
        }

        public void EvalGetAASX(IHttpContext context, int fileIndex)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
                    return;
                }

                res.confirm = "Authorization = " + accessrights;
            }

            // return as FILE
            FileStream packageStream = File.OpenRead(Net46ConsoleServer.Program.envFileName[fileIndex]);

            SendStreamResponse(context, packageStream, Path.GetFileName(Net46ConsoleServer.Program.envFileName[fileIndex]));
            packageStream.Close();
        }
        #endregion

        public static string[] securityUserName;
        public static string[] securityUserPassword;

        public static string[] securityRightsName;
        public static string[] securityRightsValue;

        public static void securityInit()
        {
            int aascount = Net46ConsoleServer.Program.env.Length;

            for (int i = 0; i < aascount; i++)
            {
                if (Net46ConsoleServer.Program.env[i] != null)
                {
                    if (Net46ConsoleServer.Program.env[i].AasEnv.AdministrationShells[0].idShort == "Security")
                    {
                        foreach (var sm in Net46ConsoleServer.Program.env[i].AasEnv.Submodels)
                        {
                            if (sm != null && sm.idShort != null)
                            {
                                int j;
                                int count;

                                if (sm.idShort == "User")
                                {
                                    Console.WriteLine("User");
                                    count = sm.submodelElements.Count;

                                    securityUserName = new string[count];
                                    securityUserPassword = new string[count];

                                    for (j = 0; j < count; j++)
                                    {
                                        var sme = sm.submodelElements[j].submodelElement;
                                        var p = sme as AdminShell.Property;
                                        // Console.WriteLine(p.idShort + " : " + p.value);
                                        securityUserName[j] = p.idShort;
                                        securityUserPassword[j] = p.value;
                                    }
                                }
                                if (sm.idShort == "SecurityRights")
                                {
                                    Console.WriteLine("SecurityRights");
                                    count = sm.submodelElements.Count;

                                    securityRightsName = new string[count];
                                    securityRightsValue = new string[count];

                                    for (j = 0; j < count; j++)
                                    {
                                        var sme = sm.submodelElements[j].submodelElement;
                                        var p = sme as AdminShell.Property;
                                        // Console.WriteLine(p.idShort + " : " + p.value);
                                        securityRightsName[j] = p.idShort;
                                        securityRightsValue[j] = p.value;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #region // Concept Descriptions

        public void EvalPutCd(IHttpContext context, string aasid)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            if (withAuthentification)
            {
                string accessrights = SecurityCheck(context);

                if (accessrights == null)
                {
                    res.error = "You are not authorized for this operation!";
                    SendJsonResponse(context, res);
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
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(HttpStatusCode.NotFound, $"No AAS with idShort '{aasid}' found.");
                return;
            }

            // de-serialize CD
            AdminShell.ConceptDescription cd = null;
            try
            {
                cd = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.ConceptDescription>(context.Request.Payload);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                return;
            }

            // need id for idempotent behaviour
            if (cd.identification == null)
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest, $"Identification of entity is (null); PUT cannot be performed.");
                return;
            }

            // datastructure update
            if (this.Packages[0] == null || this.Packages[0].AasEnv == null || this.Packages[0].AasEnv.Assets == null)
            {
                context.Response.SendResponse(HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }

            // add Submodel
            context.Server.Logger.Debug($"Adding ConceptDescription with idShort {cd.idShort ?? "--"} and id {cd.identification?.ToString() ?? "--"}");
            var existingCd = this.Packages[0].AasEnv.FindConceptDescription(cd.identification);
            if (existingCd != null)
                this.Packages[0].AasEnv.ConceptDescriptions.Remove(existingCd);
            this.Packages[0].AasEnv.ConceptDescriptions.Add(cd);

            // simple OK
            SendTextResponse(context, "OK" + ((existingCd != null) ? " (updated)" : " (new)"));
        }

        #endregion
    }
}
