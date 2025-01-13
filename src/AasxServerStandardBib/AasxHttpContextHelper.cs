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

    }
}
