using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasxMqttClient;
using AdminShellNS;
using Grapevine.Interfaces.Server;
using Grapevine.Server;
using Grapevine.Server.Attributes;
using Grapevine.Shared;
using Newtonsoft.Json;


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
    public class AasxRestServer
    {
        [RestResource]
        public class TestResource
        {
            public static AasxHttpContextHelper helper = null;

            // Basic AAS + Asset 

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))(|/core|/complete|/thumbnail|/aasenv)(/|)$")]
            public IHttpContext GetAasAndAsset(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    if (helper.PathEndsWith(context, "thumbnail"))
                    {
                        helper.EvalGetAasThumbnail(context, m.Groups[1].ToString());
                    }
                    else
                    if (helper.PathEndsWith(context, "aasenv"))
                    {
                        helper.EvalGetAasEnv(context, m.Groups[1].ToString());
                    }
                    else
                    {
                        var complete = helper.PathEndsWith(context, "complete");
                        helper.EvalGetAasAndAsset(context, m.Groups[1].ToString(), complete: complete);
                    }
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/aas(/|)$")]
            public IHttpContext PutAas(IHttpContext context)
            {
                helper.EvalPutAas(context);
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/aasx/server(/|)$")]
            public IHttpContext PutAasxOnServer(IHttpContext context)
            {
                helper.EvalPutAasxOnServer(context);
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/aasx/filesystem/([^/]+)(/|)$")]
            public IHttpContext PutAasxToFileSystem(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalPutAasxToFilesystem(context, m.Groups[1].ToString());
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.DELETE, PathInfo = "^/aas/([^/]+)(/|)$")]
            public IHttpContext DeleteAasAndAsset(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalDeleteAasAndAsset(context, m.Groups[1].ToString(), deleteAsset: true);
                }
                return context;
            }

            // Handles

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/handles/identification(/|)$")]
            public IHttpContext GetHandlesIdentification(IHttpContext context)
            {
                helper.EvalGetHandlesIdentification(context);
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/handles/identification(/|)$")]
            public IHttpContext PostHandlesIdentification(IHttpContext context)
            {
                helper.EvalPostHandlesIdentification(context);
                return context;
            }

            // Authenticate

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/authenticateGuest(/|)$")]
            public IHttpContext GetAuthenticate(IHttpContext context)
            {
                helper.EvalGetAuthenticateGuest(context);
                return context;
            }

            // Authenticate User

            [RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/authenticateUser(/|)$")]
            public IHttpContext PostAuthenticateUser(IHttpContext context)
            {
                helper.EvalPostAuthenticateUser(context);
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/authenticateCert1(/|)$")]
            public IHttpContext PostAuthenticateCert1(IHttpContext context)
            {
                helper.EvalPostAuthenticateCert1(context);
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/authenticateCert2(/|)$")]
            public IHttpContext PostAuthenticateCert2(IHttpContext context)
            {
                helper.EvalPostAuthenticateCert2(context);
                return context;
            }

            // Server

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/server/profile(/|)$")]
            public IHttpContext GetServerProfile(IHttpContext context)
            {
                helper.EvalGetServerProfile(context);
                return context;
            }

            // OZ
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/server/listaas(/|)$")]
            public IHttpContext GetServerAASX(IHttpContext context)
            {
                helper.EvalGetListAAS(context);
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = @"^/assetid/(\d+)(/|)$")]
            public IHttpContext AssetId(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalAssetId(context, Int32.Parse(m.Groups[1].ToString()));
                }

                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = @"^/server/getaasx/(\d+)(/|)$")]
            public IHttpContext GetAASX(IHttpContext context)
            {

                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalGetAASX(context, Int32.Parse(m.Groups[1].ToString()));
                    return context;
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = @"^/server/getaasx2/(\d+)(/|)$")]
            public IHttpContext GetAASX2(IHttpContext context)
            {

                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalGetAASX2(context, Int32.Parse(m.Groups[1].ToString()));
                    return context;
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = @"^/server/getfile/(\d+)/aasx/(([^/]+)/){0,99}([^/]+)$")]
            public IHttpContext GetFile(IHttpContext context)
            {
                int index = -1;
                string path = "/aasx";

                string[] split = context.Request.PathInfo.Split(new Char[] { '/' });
                if (split[1].ToLower() == "server" && split[2].ToLower() == "getfile")
                {
                    index = Int32.Parse(split[3]);
                    for (int i = 5; i < split.Length; i++)
                    {
                        path += "/" + split[i];
                    }
                }

                helper.EvalGetFile(context, index, path);

                return context;
            }

            // Assets

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/assets/([^/]+)(/|)$")]
            public IHttpContext GetAssets(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalGetAssetLinks(context, m.Groups[1].ToString());
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/assets(/|)$")]
            public IHttpContext PutAssets(IHttpContext context)
            {
                helper.EvalPutAsset(context);
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/aas/(id|([^/]+))/asset(/|)$")]
            public IHttpContext PutAssetsToAas(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalPutAssetToAas(context, m.Groups[1].ToString());
                }
                return context;
            }

            // List of Submodels

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))/submodels(/|)$")]
            public IHttpContext GetSubmodels(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalGetSubmodels(context, m.Groups[1].ToString());
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/aas/(id|([^/]+))/submodels(/|)$")]
            public IHttpContext PutSubmodel(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalPutSubmodel(context, m.Groups[1].ToString());
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.DELETE, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)(/|)$")]
            public IHttpContext DeleteSubmodel(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 4)
                {
                    helper.EvalDeleteSubmodel(context, m.Groups[1].ToString(), m.Groups[3].ToString());
                }
                return context;
            }

            // Contents of a Submodel

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)(|/core|/deep|/complete)(/|)$")]
            public IHttpContext GetSubmodelContents(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 4)
                {
                    var deep = helper.PathEndsWith(context, "deep");
                    var complete = helper.PathEndsWith(context, "complete");
                    helper.EvalGetSubmodelContents(context, m.Groups[1].ToString(), m.Groups[3].ToString(), deep: deep || complete, complete: complete);
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)/table(/|)$")]
            public IHttpContext GetSubmodelContentsAsTable(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 4)
                {
                    helper.EvalGetSubmodelContentsAsTable(context, m.Groups[1].ToString(), m.Groups[3].ToString());
                }
                return context;
            }

            // Contents of SubmodelElements

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)/elements(/([^/]+)){1,99}?(|/core|/complete|/deep|/file|/blob|/events|/property)(/|)$")]
            public IHttpContext GetSubmodelElementsContents(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 6 && m.Groups[5].Captures != null && m.Groups[5].Captures.Count >= 1)
                {
                    var aasid = m.Groups[1].ToString();
                    var smid = m.Groups[3].ToString();
                    var elemids = new List<string>();
                    for (int i = 0; i < m.Groups[5].Captures.Count; i++)
                        elemids.Add(m.Groups[5].Captures[i].ToString());

                    // special case??
                    if (helper.PathEndsWith(context, "file"))
                    {
                        helper.EvalGetSubmodelElementsFile(context, aasid, smid, elemids.ToArray());
                    }
                    else
                    if (helper.PathEndsWith(context, "blob"))
                    {
                        helper.EvalGetSubmodelElementsBlob(context, aasid, smid, elemids.ToArray());
                    }
                    else
                    if (helper.PathEndsWith(context, "property"))
                    {
                        helper.EvalGetSubmodelElementsProperty(context, aasid, smid, elemids.ToArray());
                    }
                    else
                    if (helper.PathEndsWith(context, "events"))
                    {
                        context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.NotImplemented, $"Events currently not implented.");
                    }
                    else
                    {
                        // more options
                        bool complete = false, deep = false;
                        if (helper.PathEndsWith(context, "deep"))
                            deep = true;
                        if (helper.PathEndsWith(context, "complete"))
                        {
                            deep = true;
                            complete = true;
                        }

                        helper.EvalGetSubmodelElementContents(context, aasid, smid, elemids.ToArray(), deep, complete);
                    }
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)/elements(/([^/]+)){1,99}?/invoke(/|)$")]
            public IHttpContext PostSubmodelElementsContents(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 6 && m.Groups[5].Captures != null && m.Groups[5].Captures.Count >= 1)
                {
                    var aasid = m.Groups[1].ToString();
                    var smid = m.Groups[3].ToString();
                    var elemids = new List<string>();
                    for (int i = 0; i < m.Groups[5].Captures.Count; i++)
                        elemids.Add(m.Groups[5].Captures[i].ToString());

                    // special case??
                    if (helper.PathEndsWith(context, "invoke"))
                    {
                        helper.EvalInvokeSubmodelElementOperation(context, aasid, smid, elemids.ToArray());
                    }
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)/elements(/([^/]+)){0,99}?(/|)$")]
            public IHttpContext PutSubmodelElementsContents(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 6)
                {
                    var aasid = m.Groups[1].ToString();
                    var smid = m.Groups[3].ToString();
                    var elemids = new List<string>();
                    if (m.Groups[5].Captures != null)
                        for (int i = 0; i < m.Groups[5].Captures.Count; i++)
                            elemids.Add(m.Groups[5].Captures[i].ToString());

                    helper.EvalPutSubmodelElementContents(context, aasid, smid, elemids.ToArray());
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.DELETE, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)/elements(/([^/]+)){0,99}?(/|)$")]
            public IHttpContext DeleteSubmodelElementsContents(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 6)
                {
                    var aasid = m.Groups[1].ToString();
                    var smid = m.Groups[3].ToString();
                    var elemids = new List<string>();
                    if (m.Groups[5].Captures != null)
                        for (int i = 0; i < m.Groups[5].Captures.Count; i++)
                            elemids.Add(m.Groups[5].Captures[i].ToString());

                    helper.EvalDeleteSubmodelElementContents(context, aasid, smid, elemids.ToArray());
                }
                return context;
            }

            // concept descriptions

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))/cds(/|)$")]
            public IHttpContext GetCds(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalGetAllCds(context, m.Groups[1].ToString());
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/aas/(id|([^/]+))/cds(/|)$")]
            public IHttpContext PutConceptDescription(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalPutCd(context, m.Groups[1].ToString());
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))/cds/([^/]+)(/|)$")]
            public IHttpContext GetSpecificCd(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 4)
                {
                    helper.EvalGetCdContents(context, m.Groups[1].ToString(), m.Groups[3].ToString());
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.DELETE, PathInfo = "^/aas/(id|([^/]+))/cds/([^/]+)(/|)$")]
            public IHttpContext DeleteSpecificCd(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 4)
                {
                    helper.EvalDeleteSpecificCd(context, m.Groups[1].ToString(), m.Groups[3].ToString());
                }
                return context;
            }

        }

        private static RestServer startedRestServer = null;

        public static void Start(AdminShellPackageEnv[] packages, string host, string port, bool https, GrapevineLoggerSuper logger = null)
        {
            // if running, stop old server
            Stop();

            var helper = new AasxHttpContextHelper();
            helper.Packages = packages;
            TestResource.helper = helper;

            var serverSettings = new ServerSettings();
            serverSettings.Host = host;
            serverSettings.Port = port;
            serverSettings.UseHttps = https;

            if (logger != null)
                logger.Warn("Please notice: the API and REST routes implemented in this version of the source code are not specified and standardised by the" +
                    "specification Details of the Administration Shell. The hereby stated approach is solely the opinion of its author(s).");


            startedRestServer = new RestServer(serverSettings);
            {
                if (logger != null)
                    startedRestServer.Logger = logger;
                startedRestServer.Start();
                Console.WriteLine(startedRestServer.ListenerPrefix);
            }

            // tail of the messages, again
            if (logger != null)
                logger.Warn("Please notice: the API and REST routes implemented in this version of the source code are not specified and standardised by the" +
                    "specification Details of the Administration Shell. The hereby stated approach is solely the opinion of its author(s).");
        }

        public static void Stop()
        {
            if (startedRestServer != null)
                try
                {
                    startedRestServer.Stop();
                    startedRestServer = null;
                }
                catch { }
        }
    }
}
