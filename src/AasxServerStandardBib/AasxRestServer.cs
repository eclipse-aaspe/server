#define MICHA

using AasxMqttClient;
using AasxServerDB;
using AasxServer;
using AdminShellEvents;
using AdminShellNS;
using Extensions;
using Grapevine.Interfaces.Server;
using Grapevine.Server;
using Grapevine.Server.Attributes;
using Grapevine.Shared;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HttpMethod = Grapevine.Shared.HttpMethod;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/


/* Please notice: the API and REST routes implemented in this version of the source code are not specified and standardised by the
specification Details of the Administration Shell. The hereby stated approach is solely the opinion of its author(s). */

namespace AasxRestServerLibrary
{
    using System.Text.Json;

    public class AasxRestServer
    {
        static string translateURL(string url)
        {
            // get from environment
            if (url.Substring(0, 1) == "$")
            {
                string envVar = url.Substring(1);
                url = System.Environment.GetEnvironmentVariable(envVar);
                if (url != null)
                {
                    url = url.Replace("\r", "");
                    url = url.Replace("\n", "");
                }
            }

            return url;
        }

        [ RestResource ]
        public class TestResource
        {
            public static List<string> listofRepositories = new List<string>();
            private static bool firstListOfRepositories = true;

            public static void initListOfRepositories()
            {
                if (firstListOfRepositories)
                {
                    firstListOfRepositories = false;

                    int aascount = AasxServer.Program.env.Length;
                    for (int i = 0; i < aascount; i++)
                    {
                        var env = AasxServer.Program.env[ i ];
                        if (env?.AasEnv?.AssetAdministrationShells == null)
                            continue;

                        foreach (var aas in env.AasEnv.AssetAdministrationShells)
                        {
                            if (aas.IdShort != "REGISTRY")
                                continue;

                            foreach (var smr in aas.Submodels)
                            {
                                // find Submodel
                                var sm = env.AasEnv.FindSubmodel(smr);
                                if (sm == null)
                                    continue;

                                if (sm.IdShort != "REPOSITORIES")
                                    continue;

                                foreach (var sme in sm.SubmodelElements)
                                {
                                    if (sme is Property p)
                                    {
                                        string repositoryURL = translateURL(p.Value);
                                        listofRepositories.Add(repositoryURL);
                                        Console.WriteLine("listofRepositories.Add " + repositoryURL);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // query
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/queryregistry/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/queryregistry/(/|)$") ]
            public IHttpContext Queryregistry(IHttpContext context)
            {
                allowCORS(context);

                string result = "";
                string restPath = context.Request.PathInfo;
                string query = restPath.Replace("/queryregistry/", "");

                var handler = new HttpClientHandler();
                var proxy = AasxServer.AasxTask.proxy;
                if (proxy != null)
                    handler.Proxy = proxy;
                else
                    handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                var client = new HttpClient(handler);

                initListOfRepositories();
                bool error = false;
                Task task = null;
                HttpResponseMessage response = null;
                foreach (var r in listofRepositories)
                {
                    if (r == "this")
                    {
                        if (query != "")
                            result += runQuery(query, context.Request.Payload);
                        continue;
                    }

                    string requestPath = r + "/query/";
                    if (query != "")
                        requestPath += query;
                    try
                    {
                        if (query != "")
                        {
                            task = Task.Run(async () => { response = await client.GetAsync(requestPath, HttpCompletionOption.ResponseHeadersRead); }
                            );
                        }
                        else
                        {
                            task = Task.Run(async () => { response = await client.PostAsync(requestPath, new StringContent(context.Request.Payload)); }
                            );
                        }

                        task.Wait();
                        if (response.IsSuccessStatusCode)
                        {
                            result += response.Content.ReadAsStringAsync().Result;
                        }
                        else
                            error = true;
                    }
                    catch
                    {
                        error = true;
                    }

                    if (error)
                        result += "\nerror " + requestPath + "\n\n";
                }

                context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.Ok, result);

                return context;
            }

            private static bool comp(string op, string left, string right)
            {
                int leftlen = left.Length;
                int rightlen = right.Length;

                if (rightlen > 2 && right.Substring(0, 1) == "<" && right.Substring(rightlen - 1, 1) == ">")
                {
                    // Filter parameter
                    right = right.Substring(1, rightlen - 2);
                    right = right.Replace("><", " ");
                    var split = right.Split(' ');
                    if (split.Count() == 3 && split[ 0 ] == "right")
                    {
                        if (left.Contains(split[ 1 ]))
                        {
                            right = split[ 2 ];
                            rightlen = right.Length;
                            int found = left.IndexOf(split[ 1 ]);
                            found += split[ 1 ].Length;
                            string l = "";
                            while (char.IsWhiteSpace(left[ found ]) && found < leftlen - 1)
                                found++;
                            while (char.IsDigit(left[ found ]) && found < leftlen - 1)
                            {
                                l += left[ found ];
                                found++;
                            }

                            left = l;
                            leftlen = left.Length;
                        }
                        else
                            return false;
                    }
                }

                switch (op)
                {
                    case "==":
                    case "!=":
                        if (rightlen > 0)
                        {
                            string check = "";
                            int checkLen = 0;
                            switch (right.Substring(0, 1))
                            {
                                case "*":
                                    // check right string
                                    check = right.Substring(1, rightlen - 1);
                                    checkLen = check.Length;
                                    if (leftlen >= checkLen)
                                    {
                                        left = left.Substring(leftlen - checkLen, checkLen);
                                        right = check;
                                    }

                                    break;
                                default:
                                    if (right.Substring(rightlen - 1, 1) == "*")
                                    {
                                        // check left string
                                        check = right.Substring(0, rightlen - 1);
                                        checkLen = check.Length;
                                        if (leftlen >= checkLen)
                                        {
                                            left = left.Substring(0, checkLen);
                                            right = check;
                                        }
                                    }

                                    break;
                            }
                        }

                        if (op == "==")
                            return left == right;
                        return left != right;
                    case "==num":
                    case "!=num":
                    case ">":
                    case "<":
                    case ">=":
                    case "<=":
                        if (left == "" || right == "")
                            return false;

                        if (left.Contains(".") || right.Contains(".")) // double
                        {
                            try
                            {
                                string legal = "012345679.";

                                foreach (var c in left + right)
                                {
                                    if (Char.IsDigit(c))
                                        continue;
                                    if (c == '.')
                                        continue;
                                    if (!legal.Contains(c))
                                        return false;
                                }

                                var decSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                                // Console.WriteLine("seperator = " + decSep);
                                left = left.Replace(".", decSep);
                                left = left.Replace(",", decSep);
                                right = right.Replace(".", decSep);
                                right = right.Replace(",", decSep);
                                double l = Convert.ToDouble(left);
                                double r = Convert.ToDouble(right);
                                switch (op)
                                {
                                    case "==num":
                                        return l == r;
                                    case "!=num":
                                        return l != r;
                                    case ">":
                                        return l > r;
                                    case "<":
                                        return l < r;
                                    case ">=":
                                        return l >= r;
                                    case "<=":
                                        return l <= r;
                                }
                            }
                            catch
                            {
                                return false;
                            }

                            return (false);
                        }

                        if (!left.All(char.IsDigit))
                            return false;
                        if (!right.All(char.IsDigit))
                            return false;
                        try
                        {
                            int l = Convert.ToInt32(left);
                            int r = Convert.ToInt32(right);

                            switch (op)
                            {
                                case "==num":
                                    return l == r;
                                case "!=num":
                                    return l != r;
                                case ">":
                                    return l > r;
                                case "<":
                                    return l < r;
                                case ">=":
                                    return l >= r;
                                case "<=":
                                    return l <= r;
                            }
                        }
                        catch
                        {
                            return false;
                        }

                        return (false);
                    case "contains":
                        return left.Contains(right);
                    case "!contains":
                        return !left.Contains(right);
                }

                return false;
            }

            public static string runQueryRegistryOnly(string query, string restPayload, Submodel aasRegistry, AasCore.Aas3_0.Environment envRegistry)
            {
                string result = "";

                if (aasRegistry != null)
                {
                    query = Base64UrlEncoder.Decode(query);
                    var split = query.Split('\n');
                    foreach (var s in split)
                        result += "# " + s + "\n";
                    result += "#\n";

                    bool error = false;
                    if (split.Length <= 7)
                        error = true;
                    else
                    {
                        if (split[ 0 ] != "SELECT:")
                            error = true;
                        // if (split[1] != "aas" && split[1] != "submodel")
                        //    error = true;
                        if (split[ 2 ] != "FROM:")
                            error = true;
                        if (split[ 3 ] != "registry")
                            error = true;
                        if (split[ 4 ] != "WHERE:")
                            error = true;
                        if (split[ 5 ] != "submodelelement" && split[ 5 ] != "submodel" && split[ 5 ] != "aas")
                            error = true;
                        if (split[ 6 ] != "AND" && split[ 6 ] != "OR")
                            error = true;
                    }

                    if (error)
                    {
                        result += "ERROR\n";
                        result += "For registry query only allowed:\n";
                        result += "SELECT:\n";
                        result += "aas | submodel\n";
                        result += "FROM:\n";
                        result += "registry\n";
                        result += "WHERE:\n";
                        result += "aas | submodel | submodelelement\n";
                        result += "OR | AND\n";
                        result += "%id | %assetid | %idshort | %value | %semanticid | <space> == | != | > | >= | < | <= | contains | !contains <space> \"value\"\n";
                        return result;
                    }

                    result += "registry endpoint " + AasxServer.Program.externalBlazor + "\n";

                    var sSplit = split[ 1 ].Split(' ');
                    string select = sSplit[ 0 ];
                    string selectParameters = split[ 1 ].Substring(select.Length);
                    string whereAasCondition = "";
                    string whereSmCondition = "";
                    string whereSmeCondition = "";
                    List<string> whereAasOperations = new List<string>();
                    List<string> whereSmOperations = new List<string>();
                    List<string> whereSmeOperations = new List<string>();

                    if (split[ 5 ] == "aas")
                    {
                        whereAasCondition = split[ 6 ].ToLower();
                        for (int i = 7; i < split.Length; i++)
                            whereAasOperations.Add(split[ i ]);
                    }

                    if (split[ 5 ] == "submodel")
                    {
                        whereSmCondition = split[ 6 ].ToLower();
                        for (int i = 7; i < split.Length; i++)
                            whereSmOperations.Add(split[ i ]);
                    }

                    if (split[ 5 ] == "submodelelement")
                    {
                        whereSmeCondition = split[ 6 ].ToLower();
                        for (int i = 7; i < split.Length; i++)
                            whereSmeOperations.Add(split[ i ]);
                    }

                    int totalFound = 0;
                    foreach (var smer in aasRegistry.SubmodelElements)
                    {
                        if (smer is SubmodelElementCollection smc
                            && smer.IdShort.Contains("Descriptor"))
                        {
                            string aasIdShort = "";
                            string aasID = "";
                            string assetID = "";
                            string aasEndpoint = "";
                            string descriptorJSON = "";
                            List<SubmodelElementCollection> smDescriptors = new List<SubmodelElementCollection>();
                            foreach (var sme2 in smc.Value)
                            {
                                if (sme2 is Property p)
                                {
                                    switch (p.IdShort)
                                    {
                                        case "idShort":
                                            aasIdShort = p.Value;
                                            break;
                                        case "aasID":
                                            aasID = p.Value;
                                            break;
                                        case "assetID":
                                            assetID = p.Value;
                                            break;
                                        case "endpoint":
                                            aasEndpoint = p.Value;
                                            break;
                                        case "descriptorJSON":
                                            descriptorJSON = p.Value;
                                            break;
                                    }
                                }

                                if (sme2 is ReferenceElement r)
                                {
                                    if (r.IdShort.Substring(0, 4).ToLower() == "ref_")
                                    {
                                        var aasObject = envRegistry.FindReferableByReference(r.Value);
                                        if (aasObject is SubmodelElementCollection c)
                                        {
                                            smDescriptors.Add(c);
                                        }
                                    }
                                }
                            }

                            int foundInAas = 0;
                            if (whereAasCondition != "")
                            {
                                int conditionsTrue = 0;
                                foreach (var wo in whereAasOperations)
                                {
                                    string attr = "";
                                    string attrValue = "";
                                    string op = "";
                                    split = wo.Split(' ');
                                    if (split.Length == 3)
                                    {
                                        attr = split[ 0 ];
                                        op = split[ 1 ];
                                        attrValue = split[ 2 ].Replace("\"", "");
                                    }

                                    string compare = "";
                                    switch (attr)
                                    {
                                        case "%idshort":
                                            compare = aasIdShort;
                                            break;
                                        case "%id":
                                            compare = aasID;
                                            break;
                                        case "%assetid":
                                            compare = assetID;
                                            break;
                                    }

                                    if (comp(op, compare, attrValue))
                                    {
                                        conditionsTrue++;
                                        if (whereAasCondition == "or")
                                            break;
                                    }
                                    else
                                    {
                                        if (whereAasCondition == "and")
                                            break;
                                    }
                                }

                                if ((whereAasCondition == "and" && conditionsTrue == whereAasOperations.Count)
                                    || (whereAasCondition == "or" && conditionsTrue != 0))
                                {
                                    foundInAas++;
                                    totalFound++;
                                }
                            }

                            foreach (var smd in smDescriptors)
                            {
                                string submodelIdShort = "";
                                string submodelID = "";
                                string semanticID = "";
                                string submodelEndpoint = "";
                                descriptorJSON = "";
                                SubmodelElementCollection federatedElements = new SubmodelElementCollection();
                                foreach (var sme2 in smd.Value)
                                {
                                    if (sme2 is Property p)
                                    {
                                        switch (p.IdShort)
                                        {
                                            case "idShort":
                                                submodelIdShort = p.Value;
                                                break;
                                            case "submodelID":
                                                submodelID = p.Value;
                                                break;
                                            case "semanticID":
                                                semanticID = p.Value;
                                                break;
                                            case "endpoint":
                                                submodelEndpoint = p.Value;
                                                break;
                                            case "descriptorJSON":
                                                descriptorJSON = p.Value;
                                                break;
                                        }
                                    }

                                    if (sme2 is SubmodelElementCollection smc2)
                                    {
                                        switch (smc2.IdShort)
                                        {
                                            case "federatedElements":
                                                federatedElements = smc2;
                                                break;
                                        }
                                    }
                                }

                                // check, if access to submodel is allowed
                                var accessSubmodel = !AasxRestServerLibrary.AasxHttpContextHelper.withAuthentification ||
                                                     AasxRestServerLibrary.AasxHttpContextHelper.checkAccessLevel(
                                                         null, "/submodels", "READ",
                                                         submodelIdShort, "semanticid", semanticID);

                                if (!accessSubmodel)
                                    continue;

                                int foundInSubmodel = 0;
                                if (whereSmCondition != "")
                                {
                                    int conditionsTrue = 0;
                                    foreach (var wo in whereSmOperations)
                                    {
                                        string attr = "";
                                        string attrValue = "";
                                        string op = "";
                                        split = wo.Split(' ');
                                        if (split.Length == 3)
                                        {
                                            attr = split[ 0 ];
                                            op = split[ 1 ];
                                            attrValue = split[ 2 ].Replace("\"", "");
                                        }

                                        string compare = "";
                                        switch (attr)
                                        {
                                            case "%idshort":
                                                compare = submodelIdShort;
                                                break;
                                            case "%id":
                                                compare = submodelID;
                                                break;
                                            case "%semanticid":
                                                compare = semanticID;
                                                break;
                                        }

                                        if (comp(op, compare, attrValue))
                                        {
                                            conditionsTrue++;
                                            if (whereAasCondition == "or")
                                                break;
                                        }
                                        else
                                        {
                                            if (whereAasCondition == "and")
                                                break;
                                        }
                                    }

                                    if ((whereAasCondition == "and" && conditionsTrue == whereAasOperations.Count)
                                        || (whereAasCondition == "or" && conditionsTrue != 0))
                                    {
                                        foundInSubmodel++;
                                        totalFound++;
                                    }
                                }

                                if (whereSmeCondition != "" &&
                                    federatedElements.Value != null && federatedElements.Value.Count > 0)
                                {
                                    List<List<ISubmodelElement>> stack = new List<List<ISubmodelElement>>();
                                    List<int> iStack = new List<int>();
                                    int depth = 0;
                                    List<ISubmodelElement> level = new List<ISubmodelElement>();
                                    level.Add(federatedElements);
                                    int iLevel = 0;
                                    while (depth >= 0)
                                    {
                                        while (iLevel < level.Count)
                                        {
                                            var sme = level[ iLevel ];

                                            int conditionsTrue = 0;
                                            foreach (var wo in whereSmeOperations)
                                            {
                                                string attr = "";
                                                string attrValue = "";
                                                string op = "";
                                                split = wo.Split(' ');
                                                if (split.Length == 3)
                                                {
                                                    attr = split[ 0 ];
                                                    op = split[ 1 ];
                                                    attrValue = split[ 2 ].Replace("\"", "");
                                                }

                                                string compare = "";
                                                switch (attr)
                                                {
                                                    case "%idshort":
                                                        compare = sme.IdShort;
                                                        break;
                                                    case "%value":
                                                        if (sme is Property p)
                                                            compare = p.Value;
                                                        break;
                                                    case "%semanticid":
                                                        if (sme.SemanticId != null && sme.SemanticId.Keys != null && sme.SemanticId.Keys.Count != 0)
                                                            compare = sme.SemanticId.Keys[ 0 ].Value;
                                                        break;
                                                }

                                                if (comp(op, compare, attrValue))
                                                {
                                                    conditionsTrue++;
                                                    if (whereSmeCondition == "or")
                                                        break;
                                                }
                                                else
                                                {
                                                    if (whereSmeCondition == "and")
                                                        break;
                                                }
                                            }

                                            if ((whereSmeCondition == "and" && conditionsTrue == whereSmeOperations.Count)
                                                || (whereSmeCondition == "or" && conditionsTrue != 0))
                                            {
                                                foundInAas++;
                                                foundInSubmodel++;
                                                totalFound++;

                                                if (selectParameters.Contains("%value"))
                                                {
                                                    result += "submodelelement found 1 value ";
                                                    if (sme is Property p)
                                                        result += " " + p.Value;
                                                    if (sme is AasCore.Aas3_0.File f)
                                                        result += " " + f.Value;
                                                    if (sme is MultiLanguageProperty mlp)
                                                    {
                                                        if (mlp.Value != null && mlp.Value != null)
                                                        {
                                                            for (int iMlp = 0; iMlp < mlp.Value.Count; iMlp++)
                                                            {
                                                                result += " [" + mlp.Value[ iMlp ].Language + "]" +
                                                                          mlp.Value[ iMlp ].Text;
                                                            }
                                                        }
                                                    }

                                                    result += "\n";
                                                }
                                            }

                                            if (sme is SubmodelElementCollection smc2)
                                            {
                                                stack.Add(level);
                                                iStack.Add(iLevel + 1);
                                                depth++;
                                                string smcSemanticId = "";
                                                if (smc2.SemanticId != null && smc2.SemanticId.Keys != null && smc2.SemanticId.Keys.Count != 0)
                                                    smcSemanticId = smc2.SemanticId.Keys[ 0 ].Value;
                                                level = smc2.Value;
                                                iLevel = 0;
                                                continue;
                                            }

                                            iLevel++;
                                        }

                                        depth--;
                                        if (depth >= 0)
                                        {
                                            level = stack[ depth ];
                                            stack.RemoveAt(depth);
                                            iLevel = iStack[ depth ];
                                            iStack.RemoveAt(depth);
                                        }
                                    }
                                }

                                if (select == "submodel")
                                {
                                    if (foundInSubmodel > 0)
                                    {
                                        result += "submodel" + " found " + foundInSubmodel;
                                        if (!selectParameters.Contains("!endpoint"))
                                        {
                                            result += " " + "<a href=\"" + submodelEndpoint + "\" target=\"_blank\">" + submodelEndpoint + "</a>";
                                        }

                                        if (selectParameters.Contains("%idshort"))
                                        {
                                            result += " " + submodelIdShort;
                                        }

                                        if (selectParameters.Contains("%id"))
                                        {
                                            result += " " + submodelID;
                                        }

                                        if (selectParameters.Contains("%semanticid"))
                                        {
                                            result += " " + semanticID;
                                        }

                                        result += "\n";
                                    }
                                }
                            }

                            if (select == "aas")
                            {
                                if (foundInAas > 0)
                                {
                                    result += "aas" + " found " + foundInAas;
                                    if (!selectParameters.Contains("!endpoint"))
                                    {
                                        result += " " + "<a href=\"" + aasEndpoint + "\" target=\"_blank\">" + aasEndpoint + "</a>";
                                    }

                                    if (selectParameters.Contains("%idshort"))
                                    {
                                        result += " " + aasIdShort;
                                    }

                                    if (selectParameters.Contains("%id"))
                                    {
                                        result += " " + aasID;
                                    }

                                    if (selectParameters.Contains("%aasetid"))
                                    {
                                        result += " " + assetID;
                                    }

                                    result += "\n";
                                }
                            }
                        }
                    }

                    result += "registry totalfound " + totalFound + " " + AasxServer.Program.externalBlazor + "\n";
                }

                return result;
            }

            public static string runQuery(string query, string restPayload)
            {
                string result = "";

                if (query == "help")
                {
                    result = "Please use POST or add BASE64URL coded query to /query/, e.g. use https://www.base64url.com/<br><br>";
                    result += "[ STORE: ] (result of query will be used to search inside by directly following query)<br>";
                    result += "SELECT:<br>";
                    result += "repository | aas | aasid | submodel | submodelid | submodelelement (what will be returned)<br>";
                    result += "FROM:<br>";
                    result += "repository | aas \"aasid\" | submodel \"submodelid\" (what will be searched)<br>";
                    result += "WHERE:<br>";
                    result += "aas | submodel | submodelelement (element to search for)<br>";
                    result += "OR | AND<br>";
                    result +=
                        "%id | %assetid | %idshort | %value | %semanticid | %path | %semanticidpath <space> == | != | > | >= | < | <= | contains | !contains <space> \"value\"<br>";
                    result += "(last line may be repeated after OR and AND)<br>";
                    result += "(options after SELECT: aas [ %id | %idshort | %assetid | !endpoint ])<br>";
                    result += "(options after SELECT: submodel [ %id | %idshort | %semanticid | !endpoint ])<br>";
                    result += "(options after SELECT: submodelelement [ %idshort | %semanticid | %value | !endpoint ])<br>";
                    result += "(WHERE: aas, WHERE: submodel, WHERE: submodelelement may be combined)<br>";
                    result += "<br>";

                    result += "EXAMPLE:<br><br>";
                    result += "SELECT:<br>";
                    result += "submodel<br>";
                    result += "FROM:<br>";
                    result += "repository<br>";
                    result += "WHERE:<br>";
                    result += "submodelelement<br>";
                    result += "OR<br>";
                    result += "%idshort contains \"ManufacturerName\"<br>";
                    result += "%idshort contains \"Weight\"<br>";

                    return result;
                }

                string[] split = new string[0];
                if (query == "") // POST
                {
                    if (restPayload == "")
                        return result;

                    query = restPayload.Replace("\r", "");
                    split = query.Split('\n');
                }
                else // GET with BASE64URL encoded string
                {
                    try
                    {
                        query = Base64UrlEncoder.Decode(query);
                        split = query.Split('\n');
                    }
                    catch
                    {
                    }
                }

                string last = "";
                // extract separate queries by STORE: and SELECT:
                List<string> nested = new List<string>();
                string next = "";
                foreach (string sp in split)
                {
                    if (sp == "")
                        continue;

                    string s = sp.ToLower();
                    if ((s == "store:" || s == "select:") && last != "store:" && last != "")
                    {
                        nested.Add(next);
                        next = "";
                    }

                    next += sp + "\n";
                    last = s;
                }

                if (next != "")
                    nested.Add(next);

                bool storeResult = false;
                List<IAssetAdministrationShell> storeAas = new List<IAssetAdministrationShell>();
                List<ISubmodel> storeSubmodels = new List<ISubmodel>();
                List<ISubmodelElement> storeSmes = new List<ISubmodelElement>();
                List<IAssetAdministrationShell> storeAasLast = new List<IAssetAdministrationShell>();
                List<ISubmodel> storeSubmodelsLast = new List<ISubmodel>();
                List<ISubmodelElement> storeSmesLast = new List<ISubmodelElement>();

                foreach (string n in nested)
                {
                    string select = "";
                    string selectParameters = "";
                    string from = "";
                    string fromId = "";
                    string whereElement = "";
                    string whereAasCondition = "";
                    string whereSmCondition = "";
                    string whereSmeCondition = "";
                    List<string> whereAasOperations = new List<string>();
                    List<string> whereSmOperations = new List<string>();
                    List<string> whereSmeOperations = new List<string>();
                    int countLines = 0;
                    last = "";

                    split = n.Split('\n');

                    foreach (var sp in split)
                    {
                        result += "# " + sp + "\n";

                        if (sp == "")
                            continue;

                        string s = sp.ToLower();

                        switch (s)
                        {
                            case "store:":
                                storeResult = true;
                                break;
                            case "where:":
                                last = "";
                                countLines = 0;
                                break;
                        }

                        switch (last)
                        {
                            case "select:":
                                var sSplit = s.Split(' ');
                                select = sSplit[ 0 ];
                                selectParameters = s.Substring(select.Length);
                                break;
                            case "from:":
                                var fromsplit = sp.Split(' ');
                                from = fromsplit[ 0 ].ToLower();
                                if (from == "aas" || from == "submodel")
                                {
                                    if (fromsplit.Length == 2)
                                        fromId = fromsplit[ 1 ].Replace("\"", "");
                                }

                                break;
                            case "where:":
                                switch (countLines)
                                {
                                    case 0:
                                        whereElement = sp.Replace("\"", "").ToLower();
                                        break;
                                    case 1:
                                        switch (whereElement)
                                        {
                                            case "aas":
                                                whereAasCondition = sp.Replace("\"", "").ToLower();
                                                break;
                                            case "submodel":
                                                whereSmCondition = sp.Replace("\"", "").ToLower();
                                                break;
                                            case "submodelelement":
                                                whereSmeCondition = sp.Replace("\"", "").ToLower();
                                                break;
                                        }

                                        break;
                                    default:
                                        switch (whereElement)
                                        {
                                            case "aas":
                                                whereAasOperations.Add(sp.Replace("\"", ""));
                                                break;
                                            case "submodel":
                                                whereSmOperations.Add(sp.Replace("\"", ""));
                                                break;
                                            case "submodelelement":
                                                whereSmeOperations.Add(sp.Replace("\"", ""));
                                                break;
                                        }

                                        break;
                                }

                                countLines++;
                                break;
                        }

                        if (last != "where:")
                            last = s;
                    }

                    result += "repository endpoint " + AasxServer.Program.externalBlazor + "\n";
                    /*
                    if (storeResult)
                        result += "store result\n";
                    result += "select = \"" + select + selectParameters + "\n";
                    result += "from = \"" + from + "\"\n";
                    if (fromId != "")
                        result += "fromId = \"" + fromId + "\"\n";
                    if (whereAasCondition != "")
                    {
                        result += "whereAasCondition = \"" + whereAasCondition + "\"\n";
                        foreach (var wo in whereAasOperations)
                            result += "whereAasOperation = \"" + wo + "\"\n";
                    }
                    if (whereSmCondition != "")
                    {
                        result += "whereSmCondition = \"" + whereSmCondition + "\"\n";
                        foreach (var wo in whereSmOperations)
                            result += "whereSmOperation = \"" + wo + "\"\n";
                    }
                    if (whereSmeCondition != "")
                    {
                        result += "whereSmeCondition = \"" + whereSmeCondition + "\"\n";
                        foreach (var wo in whereSmeOperations)
                            result += "whereSmeOperation = \"" + wo + "\"\n";
                    }
                    result += "\n";
                    */

                    if (!Program.withDb)
                    {
                        // search in memory
                        int totalFound = 0;
                        int foundInRepository = 0;
                        int aascount = AasxServer.Program.env.Length;
                        for (int i = 0; i < aascount; i++)
                        {
                            int foundInAas = 0;
                            var env = AasxServer.Program.env[ i ];
                            if (env?.AasEnv?.AssetAdministrationShells == null)
                                continue;

                            foreach (var aas in env.AasEnv.AssetAdministrationShells)
                            {
                                if (aas.IdShort == "REGISTRY")
                                    continue;

                                if (storeAasLast.Count != 0)
                                {
                                    if (!storeAasLast.Contains(aas))
                                        continue;
                                }

                                if (from == "aas")
                                {
                                    if (aas.Id != fromId)
                                        continue;
                                }

                                if (whereAasCondition != "")
                                {
                                    int conditionsTrue = 0;
                                    foreach (var wo in whereAasOperations)
                                    {
                                        string attr = "";
                                        string attrValue = "";
                                        string op = "";
                                        split = wo.Split(' ');
                                        if (split.Length == 3)
                                        {
                                            attr = split[ 0 ];
                                            op = split[ 1 ];
                                            attrValue = split[ 2 ];
                                        }

                                        string compare = "";
                                        switch (attr)
                                        {
                                            case "%id":
                                                compare = aas.Id;
                                                break;
                                            case "%idshort":
                                                compare = aas.IdShort;
                                                break;
                                            case "%assetid":
                                                if (aas.AssetInformation.GlobalAssetId != null)
                                                    compare = aas.AssetInformation.GlobalAssetId;
                                                break;
                                        }

                                        if (comp(op, compare, attrValue))
                                        {
                                            conditionsTrue++;
                                            if (whereAasCondition == "or")
                                                break;
                                        }
                                        else
                                        {
                                            if (whereAasCondition == "and")
                                                break;
                                        }
                                    }

                                    if ((whereAasCondition == "and" && conditionsTrue == whereAasOperations.Count)
                                        || (whereAasCondition == "or" && conditionsTrue != 0))
                                    {
                                        if (whereSmCondition == "")
                                        {
                                            if (select == "aas" || select == "aasid")
                                            {
                                                foundInAas++;
                                                totalFound++;
                                                if (storeResult)
                                                {
                                                    if (!storeAas.Contains(aas))
                                                        storeAas.Add(aas);
                                                }
                                            }

                                            if (select == "repository")
                                            {
                                                foundInRepository++;
                                                totalFound++;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                if (aas?.Submodels == null)
                                    continue;

                                foreach (var smr in aas.Submodels)
                                {
                                    // find Submodel
                                    var sm = env.AasEnv.FindSubmodel(smr);
                                    if (sm == null)
                                        continue;

                                    if (from == "submodel")
                                    {
                                        if (sm.Id != fromId)
                                            continue;
                                    }

                                    if (storeSubmodelsLast.Count != 0)
                                    {
                                        if (!storeSubmodelsLast.Contains(sm))
                                            continue;
                                    }

                                    // check, if access to submodel is allowed
                                    var accessSubmodel = !AasxRestServerLibrary.AasxHttpContextHelper.withAuthentification ||
                                                         AasxRestServerLibrary.AasxHttpContextHelper.checkAccessLevel(
                                                             null, "/submodels", "READ",
                                                             sm.IdShort, "sm", sm);

                                    string smSemanticId = "";
                                    if (sm.SemanticId != null && sm.SemanticId.Keys != null && sm.SemanticId.Keys.Count != 0)
                                        smSemanticId = sm.SemanticId.Keys[ 0 ].Value;

                                    int foundInSubmodel = 0;
                                    if (whereSmCondition != "" && accessSubmodel)
                                    {
                                        int conditionsTrue = 0;
                                        foreach (var wo in whereSmOperations)
                                        {
                                            string attr = "";
                                            string attrValue = "";
                                            string op = "";
                                            split = wo.Split(' ');
                                            if (split.Length == 3)
                                            {
                                                attr = split[ 0 ];
                                                op = split[ 1 ];
                                                attrValue = split[ 2 ];

                                                string compare = "";
                                                switch (attr)
                                                {
                                                    case "%id":
                                                        compare = sm.Id;
                                                        break;
                                                    case "%idshort":
                                                        compare = sm.IdShort;
                                                        break;
                                                    case "%semanticid":
                                                        compare = "";
                                                        if (sm.SemanticId != null && sm.SemanticId.Keys != null && sm.SemanticId.Keys.Count != 0)
                                                            compare = sm.SemanticId.Keys[ 0 ].Value;
                                                        break;
                                                }

                                                if (comp(op, compare, attrValue))
                                                {
                                                    conditionsTrue++;
                                                    if (whereSmCondition == "or")
                                                        break;
                                                }
                                                else
                                                {
                                                    if (whereSmCondition == "and")
                                                        break;
                                                }
                                            }
                                        }

                                        if ((whereSmCondition == "and" && conditionsTrue == whereSmOperations.Count)
                                            || (whereSmCondition == "or" && conditionsTrue != 0))
                                        {
                                            if (whereSmeCondition == "")
                                            {
                                                if (select == "submodel" || select == "submodelid")
                                                {
                                                    if (storeResult)
                                                    {
                                                        if (!storeSubmodels.Contains(sm))
                                                            storeSubmodels.Add(sm);
                                                    }

                                                    foundInSubmodel++;
                                                    totalFound++;
                                                }

                                                if (select == "aas" || select == "aasid")
                                                {
                                                    if (storeResult)
                                                    {
                                                        if (!storeAas.Contains(aas))
                                                            storeAas.Add(aas);
                                                    }

                                                    foundInAas++;
                                                    totalFound++;
                                                }

                                                if (select == "repository")
                                                {
                                                    foundInRepository++;
                                                    totalFound++;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }

                                    if (whereSmeCondition != "")
                                    {
                                        List<List<ISubmodelElement>> stack = new List<List<ISubmodelElement>>();
                                        List<int> iStack = new List<int>();
                                        List<string> pathStack = new List<string>();
                                        List<string> semanticIdPathStack = new List<string>();
                                        int depth = 0;
                                        List<ISubmodelElement> level = sm.SubmodelElements;
                                        int iLevel = 0;
                                        string path = "";
                                        string semanticIdPath = "";
                                        while (depth >= 0)
                                        {
                                            while (iLevel < level.Count)
                                            {
                                                var sme = level[ iLevel ];

                                                // check, if access to submodelelement is allowed
                                                string _path = sm.IdShort + ".";
                                                if (path != "")
                                                    _path += path;
                                                _path = _path.Replace("/", ".");
                                                var accessSme = !AasxRestServerLibrary.AasxHttpContextHelper.withAuthentification ||
                                                                AasxRestServerLibrary.AasxHttpContextHelper.checkAccessLevel(
                                                                    null, "/submodelelements", "READ",
                                                                    _path + sme.IdShort, "", sm);

                                                if (accessSme)
                                                {
                                                    int conditionsTrue = 0;
                                                    foreach (var wo in whereSmeOperations)
                                                    {
                                                        string attr = "";
                                                        string attrValue = "";
                                                        string op = "";
                                                        split = wo.Split(' ');
                                                        if (split.Length == 3)
                                                        {
                                                            attr = split[ 0 ];
                                                            op = split[ 1 ];
                                                            attrValue = split[ 2 ];
                                                        }

                                                        string compare = "";
                                                        switch (attr)
                                                        {
                                                            case "%idshort":
                                                                compare = sme.IdShort;
                                                                break;
                                                            case "%value":
                                                                if (sme is Property p)
                                                                    compare = p.Value;
                                                                break;
                                                            case "%semanticid":
                                                                if (sme.SemanticId != null && sme.SemanticId.Keys != null && sme.SemanticId.Keys.Count != 0)
                                                                    compare = sme.SemanticId.Keys[ 0 ].Value;
                                                                break;
                                                            case "%path":
                                                                attrValue = attrValue.Replace(".", "/");
                                                                compare = aas.IdShort + "/" + sm.IdShort + "/" + path;
                                                                break;
                                                            case "%semanticidpath":
                                                                compare = smSemanticId + "." + semanticIdPath;
                                                                break;
                                                        }

                                                        if (comp(op, compare, attrValue))
                                                        {
                                                            conditionsTrue++;
                                                            if (whereSmeCondition == "or")
                                                                break;
                                                        }
                                                        else
                                                        {
                                                            if (whereSmeCondition == "and")
                                                                break;
                                                        }
                                                    }

                                                    if ((whereSmeCondition == "and" && conditionsTrue == whereSmeOperations.Count)
                                                        || (whereSmeCondition == "or" && conditionsTrue != 0))
                                                    {
                                                        if (storeSmesLast.Count == 0 || (storeSmesLast.Count != 0 && storeSmesLast.Contains(sme)))
                                                        {
                                                            if (select == "submodelelement")
                                                            {
                                                                if (storeResult)
                                                                {
                                                                    if (!storeSmes.Contains(sme))
                                                                        storeSmes.Add(sme);
                                                                }

                                                                result += "submodelelement found" + " 1";
                                                                if (!selectParameters.Contains("!endpoint"))
                                                                {
                                                                    string link = AasxServer.Program.externalBlazor +
                                                                                  "/submodels/" + Base64UrlEncoder.Encode(sm.Id) +
                                                                                  "/submodelelements/" + path.Replace("/", ".") + sme.IdShort;
                                                                    result += " " + "<a href=\"" + link + "\" target=\"_blank\">" + link + "</a>";
                                                                }

                                                                if (selectParameters.Contains("%idshort"))
                                                                {
                                                                    result += " " + sme.IdShort;
                                                                }

                                                                if (selectParameters.Contains("%value"))
                                                                {
                                                                    if (sme is Property p)
                                                                        result += " " + p.Value;
                                                                    if (sme is AasCore.Aas3_0.File f)
                                                                        result += " " + f.Value;
                                                                    if (sme is MultiLanguageProperty mlp)
                                                                    {
                                                                        if (mlp.Value != null && mlp.Value != null)
                                                                        {
                                                                            for (int iMlp = 0; iMlp < mlp.Value.Count; iMlp++)
                                                                            {
                                                                                result += " [" + mlp.Value[ iMlp ].Language + "]" +
                                                                                          mlp.Value[ iMlp ].Text;
                                                                            }
                                                                        }
                                                                    }
                                                                }

                                                                if (selectParameters.Contains("%semanticid"))
                                                                {
                                                                    if (sme.SemanticId != null && sme.SemanticId.Keys != null && sme.SemanticId.Keys.Count != 0)
                                                                        result += " " + sme.SemanticId.Keys[ 0 ].Value;
                                                                }

                                                                if (selectParameters.Contains("%path"))
                                                                {
                                                                    result += " " + aas.IdShort + "/" + sm.IdShort + "/" + path + sme.IdShort;
                                                                }

                                                                result += "\n";
                                                                totalFound++;
                                                            }

                                                            if (select == "submodel" || select == "submodelid")
                                                            {
                                                                if (storeResult)
                                                                {
                                                                    if (!storeSubmodels.Contains(sm))
                                                                        storeSubmodels.Add(sm);
                                                                }

                                                                foundInSubmodel++;
                                                                totalFound++;
                                                            }

                                                            if (select == "aas" || select == "aasid")
                                                            {
                                                                if (storeResult)
                                                                {
                                                                    if (!storeAas.Contains(aas))
                                                                        storeAas.Add(aas);
                                                                }

                                                                foundInAas++;
                                                                totalFound++;
                                                            }

                                                            if (select == "repository")
                                                            {
                                                                foundInRepository++;
                                                                totalFound++;
                                                            }
                                                        }
                                                    }
                                                }

                                                if (sme is SubmodelElementCollection smc)
                                                {
                                                    stack.Add(level);
                                                    iStack.Add(iLevel + 1);
                                                    pathStack.Add(path);
                                                    semanticIdPathStack.Add(semanticIdPath);
                                                    depth++;
                                                    path += smc.IdShort + "/";
                                                    string smcSemanticId = "";
                                                    if (smc.SemanticId != null && smc.SemanticId.Keys != null && smc.SemanticId.Keys.Count != 0)
                                                        smcSemanticId = smc.SemanticId.Keys[ 0 ].Value;
                                                    semanticIdPath += smcSemanticId + ".";
                                                    level = smc.Value;
                                                    iLevel = 0;
                                                    continue;
                                                }

                                                iLevel++;
                                            }

                                            depth--;
                                            if (depth >= 0)
                                            {
                                                level = stack[ depth ];
                                                stack.RemoveAt(depth);
                                                iLevel = iStack[ depth ];
                                                iStack.RemoveAt(depth);
                                                path = pathStack[ depth ];
                                                pathStack.RemoveAt(depth);
                                                semanticIdPath = semanticIdPathStack[ depth ];
                                                semanticIdPathStack.RemoveAt(depth);
                                            }
                                        }
                                    }

                                    if (select == "submodel" && foundInSubmodel != 0 && accessSubmodel)
                                    {
                                        result += select + " found " + foundInSubmodel;

                                        if (!selectParameters.Contains("!endpoint"))
                                        {
                                            string link = AasxServer.Program.externalBlazor +
                                                          "/submodels/" + Base64UrlEncoder.Encode(sm.Id);
                                            result += " " + "<a href=\"" + link + "\" target=\"_blank\">" + link + "</a>";
                                        }

                                        if (selectParameters.Contains("%id "))
                                        {
                                            result += " " + sm.Id;
                                        }

                                        if (selectParameters.Contains("%idshort"))
                                        {
                                            result += " " + sm.IdShort;
                                        }

                                        if (selectParameters.Contains("%semanticid"))
                                        {
                                            if (sm.SemanticId != null && sm.SemanticId.Keys != null && sm.SemanticId.Keys.Count != 0)
                                                result += " " + sm.SemanticId.Keys[ 0 ].Value;
                                        }

                                        result += "\n";
                                    }

                                    if (select == "submodelid" && foundInSubmodel != 0 && accessSubmodel)
                                        result += "%id == \"" + sm.Id + "\"\n";
                                } // submodels

                                if (select == "aas" && foundInAas != 0)
                                {
                                    result += select + " found " + foundInAas;

                                    if (!selectParameters.Contains("!endpoint"))
                                    {
                                        string link = AasxServer.Program.externalBlazor +
                                                      "/shells/" + Base64UrlEncoder.Encode(aas.Id);
                                        result += " " + "<a href=\"" + link + "\" target=\"_blank\">" + link + "</a>";
                                    }

                                    if (selectParameters.Contains("%id "))
                                    {
                                        result += " " + aas.Id;
                                    }

                                    if (selectParameters.Contains("%idshort"))
                                    {
                                        result += " " + aas.IdShort;
                                    }

                                    if (selectParameters.Contains("%assetid"))
                                    {
                                        if (aas.AssetInformation.GlobalAssetId != null)
                                            result += " " + aas.AssetInformation.GlobalAssetId;
                                    }

                                    result += "\n";
                                }

                                if (select == "aasid" && foundInAas != 0)
                                    result += "%id == \"" + aas.Id + "\"\n";
                            } // AAS
                        } // AAS-ENV

                        if (select == "repository" && foundInRepository != 0)
                            result += select + " found " + foundInRepository + " " + AasxServer.Program.externalBlazor + "\n";
                        else
                            result += "repository totalfound " + totalFound + " " + AasxServer.Program.externalBlazor + "\n";
                        result += "\n";

                        if (storeResult)
                        {
                            storeResult = false;
                            storeAasLast = storeAas;
                            storeAas = new List<IAssetAdministrationShell>();
                            storeSubmodelsLast = storeSubmodels;
                            storeSubmodels = new List<ISubmodel>();
                            storeSmesLast = storeSmes;
                            storeSmes = new List<ISubmodelElement>();
                            totalFound = 0;
                            foundInRepository = 0;
                            result += "\n";
                        }
                    }

                    if (Program.withDb)
                    {
                        // search in database
                        int totalFound = 0;
                        int foundInRepository = 0;
                        using (AasContext db = new AasContext())
                        {
                            var aasDBList = db.AASSets.ToList();
                            int aascount = aasDBList.Count();

                            for (int i = 0; i < aascount; i++)
                            {
                                int foundInAas = 0;

                                var aasDB = aasDBList[ i ];
                                AssetAdministrationShell aas = new AssetAdministrationShell(
                                    id: aasDB.Identifier,
                                    idShort: aasDB.IdShort,
                                    assetInformation: new AssetInformation(AssetKind.Type, aasDB.GlobalAssetId)
                                    );

                                {
                                    if (storeAasLast.Count != 0)
                                    {
                                        if (!storeAasLast.Contains(aas))
                                            continue;
                                    }

                                    if (from == "aas")
                                    {
                                        if (aas.Id != fromId)
                                            continue;
                                    }

                                    if (whereAasCondition != "")
                                    {
                                        int conditionsTrue = 0;
                                        foreach (var wo in whereAasOperations)
                                        {
                                            string attr = "";
                                            string attrValue = "";
                                            string op = "";
                                            split = wo.Split(' ');
                                            if (split.Length == 3)
                                            {
                                                attr = split[ 0 ];
                                                op = split[ 1 ];
                                                attrValue = split[ 2 ];
                                            }

                                            string compare = "";
                                            switch (attr)
                                            {
                                                case "%id":
                                                    compare = aas.Id;
                                                    break;
                                                case "%idshort":
                                                    compare = aas.IdShort;
                                                    break;
                                                case "%assetid":
                                                    if (aas.AssetInformation.GlobalAssetId != null)
                                                        compare = aas.AssetInformation.GlobalAssetId;
                                                    break;
                                            }

                                            if (comp(op, compare, attrValue))
                                            {
                                                conditionsTrue++;
                                                if (whereAasCondition == "or")
                                                    break;
                                            }
                                            else
                                            {
                                                if (whereAasCondition == "and")
                                                    break;
                                            }
                                        }

                                        if ((whereAasCondition == "and" && conditionsTrue == whereAasOperations.Count)
                                            || (whereAasCondition == "or" && conditionsTrue != 0))
                                        {
                                            if (whereSmCondition == "")
                                            {
                                                if (select == "aas" || select == "aasid")
                                                {
                                                    foundInAas++;
                                                    totalFound++;
                                                    if (storeResult)
                                                    {
                                                        if (!storeAas.Contains(aas))
                                                            storeAas.Add(aas);
                                                    }
                                                }

                                                if (select == "repository")
                                                {
                                                    foundInRepository++;
                                                    totalFound++;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }

                                    var submodelDBList = db.SMSets
                                        .OrderBy(sm => sm.Id)
                                        .Where(sm => sm.AASId == aasDB.Id)
                                        .ToList();

                                    foreach (var submodelDB in submodelDBList)
                                    {
                                        var sm = Converter.GetSubmodel(smDB: submodelDB);

                                        if (from == "submodel")
                                        {
                                            if (sm.Id != fromId)
                                                continue;
                                        }

                                        if (storeSubmodelsLast.Count != 0)
                                        {
                                            if (!storeSubmodelsLast.Contains(sm))
                                                continue;
                                        }

                                        // check, if access to submodel is allowed
                                        var accessSubmodel = !AasxRestServerLibrary.AasxHttpContextHelper.withAuthentification ||
                                                             AasxRestServerLibrary.AasxHttpContextHelper.checkAccessLevel(
                                                                 null, "/submodels", "READ",
                                                                 sm.IdShort, "sm", sm);

                                        string smSemanticId = "";
                                        if (sm.SemanticId != null && sm.SemanticId.Keys != null && sm.SemanticId.Keys.Count != 0)
                                            smSemanticId = sm.SemanticId.Keys[ 0 ].Value;

                                        int foundInSubmodel = 0;
                                        if (whereSmCondition != "" && accessSubmodel)
                                        {
                                            int conditionsTrue = 0;
                                            foreach (var wo in whereSmOperations)
                                            {
                                                string attr = "";
                                                string attrValue = "";
                                                string op = "";
                                                split = wo.Split(' ');
                                                if (split.Length == 3)
                                                {
                                                    attr = split[ 0 ];
                                                    op = split[ 1 ];
                                                    attrValue = split[ 2 ];

                                                    string compare = "";
                                                    switch (attr)
                                                    {
                                                        case "%id":
                                                            compare = sm.Id;
                                                            break;
                                                        case "%idshort":
                                                            compare = sm.IdShort;
                                                            break;
                                                        case "%semanticid":
                                                            compare = "";
                                                            if (sm.SemanticId != null && sm.SemanticId.Keys != null && sm.SemanticId.Keys.Count != 0)
                                                                compare = sm.SemanticId.Keys[ 0 ].Value;
                                                            break;
                                                    }

                                                    if (comp(op, compare, attrValue))
                                                    {
                                                        conditionsTrue++;
                                                        if (whereSmCondition == "or")
                                                            break;
                                                    }
                                                    else
                                                    {
                                                        if (whereSmCondition == "and")
                                                            break;
                                                    }
                                                }
                                            }

                                            if ((whereSmCondition == "and" && conditionsTrue == whereSmOperations.Count)
                                                || (whereSmCondition == "or" && conditionsTrue != 0))
                                            {
                                                if (whereSmeCondition == "")
                                                {
                                                    if (select == "submodel" || select == "submodelid")
                                                    {
                                                        if (storeResult)
                                                        {
                                                            if (!storeSubmodels.Contains(sm))
                                                                storeSubmodels.Add(sm);
                                                        }

                                                        foundInSubmodel++;
                                                        totalFound++;
                                                    }

                                                    if (select == "aas" || select == "aasid")
                                                    {
                                                        if (storeResult)
                                                        {
                                                            if (!storeAas.Contains(aas))
                                                                storeAas.Add(aas);
                                                        }

                                                        foundInAas++;
                                                        totalFound++;
                                                    }

                                                    if (select == "repository")
                                                    {
                                                        foundInRepository++;
                                                        totalFound++;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }

                                        if (sm.SubmodelElements != null && whereSmeCondition != "")
                                        {
                                            List<List<ISubmodelElement>> stack = new List<List<ISubmodelElement>>();
                                            List<int> iStack = new List<int>();
                                            List<string> pathStack = new List<string>();
                                            List<string> semanticIdPathStack = new List<string>();
                                            int depth = 0;
                                            List<ISubmodelElement> level = sm.SubmodelElements;
                                            int iLevel = 0;
                                            string path = "";
                                            string semanticIdPath = "";
                                            while (depth >= 0)
                                            {
                                                while (iLevel < level.Count)
                                                {
                                                    var sme = level[ iLevel ];

                                                    // check, if access to submodelelement is allowed
                                                    string _path = sm.IdShort + ".";
                                                    if (path != "")
                                                        _path += path;
                                                    _path = _path.Replace("/", ".");
                                                    var accessSme = !AasxRestServerLibrary.AasxHttpContextHelper.withAuthentification ||
                                                                    AasxRestServerLibrary.AasxHttpContextHelper.checkAccessLevel(
                                                                        null, "/submodelelements", "READ",
                                                                        _path + sme.IdShort, "", sm);

                                                    if (accessSme)
                                                    {
                                                        int conditionsTrue = 0;
                                                        foreach (var wo in whereSmeOperations)
                                                        {
                                                            string attr = "";
                                                            string attrValue = "";
                                                            string op = "";
                                                            split = wo.Split(' ');
                                                            if (split.Length == 3)
                                                            {
                                                                attr = split[ 0 ];
                                                                op = split[ 1 ];
                                                                attrValue = split[ 2 ];
                                                            }

                                                            string compare = "";
                                                            switch (attr)
                                                            {
                                                                case "%idshort":
                                                                    compare = sme.IdShort;
                                                                    break;
                                                                case "%value":
                                                                    if (sme is Property p)
                                                                        compare = p.Value;
                                                                    break;
                                                                case "%semanticid":
                                                                    if (sme.SemanticId != null && sme.SemanticId.Keys != null && sme.SemanticId.Keys.Count != 0)
                                                                        compare = sme.SemanticId.Keys[ 0 ].Value;
                                                                    break;
                                                                case "%path":
                                                                    attrValue = attrValue.Replace(".", "/");
                                                                    compare = aas.IdShort + "/" + sm.IdShort + "/" + path;
                                                                    break;
                                                                case "%semanticidpath":
                                                                    compare = smSemanticId + "." + semanticIdPath;
                                                                    break;
                                                            }

                                                            if (comp(op, compare, attrValue))
                                                            {
                                                                conditionsTrue++;
                                                                if (whereSmeCondition == "or")
                                                                    break;
                                                            }
                                                            else
                                                            {
                                                                if (whereSmeCondition == "and")
                                                                    break;
                                                            }
                                                        }

                                                        if ((whereSmeCondition == "and" && conditionsTrue == whereSmeOperations.Count)
                                                            || (whereSmeCondition == "or" && conditionsTrue != 0))
                                                        {
                                                            if (storeSmesLast.Count == 0 || (storeSmesLast.Count != 0 && storeSmesLast.Contains(sme)))
                                                            {
                                                                if (select == "submodelelement")
                                                                {
                                                                    if (storeResult)
                                                                    {
                                                                        if (!storeSmes.Contains(sme))
                                                                            storeSmes.Add(sme);
                                                                    }

                                                                    result += "submodelelement found" + " 1";
                                                                    if (!selectParameters.Contains("!endpoint"))
                                                                    {
                                                                        string link = AasxServer.Program.externalBlazor +
                                                                                      "/submodels/" + Base64UrlEncoder.Encode(sm.Id) +
                                                                                      "/submodelelements/" + path.Replace("/", ".") + sme.IdShort;
                                                                        result += " " + "<a href=\"" + link + "\" target=\"_blank\">" + link + "</a>";
                                                                    }

                                                                    if (selectParameters.Contains("%idshort"))
                                                                    {
                                                                        result += " " + sme.IdShort;
                                                                    }

                                                                    if (selectParameters.Contains("%value"))
                                                                    {
                                                                        if (sme is Property p)
                                                                            result += " " + p.Value;
                                                                        if (sme is AasCore.Aas3_0.File f)
                                                                            result += " " + f.Value;
                                                                        if (sme is MultiLanguageProperty mlp)
                                                                        {
                                                                            if (mlp.Value != null && mlp.Value != null)
                                                                            {
                                                                                for (int iMlp = 0; iMlp < mlp.Value.Count; iMlp++)
                                                                                {
                                                                                    result += " [" + mlp.Value[ iMlp ].Language + "]" +
                                                                                              mlp.Value[ iMlp ].Text;
                                                                                }
                                                                            }
                                                                        }
                                                                    }

                                                                    if (selectParameters.Contains("%semanticid"))
                                                                    {
                                                                        if (sme.SemanticId != null && sme.SemanticId.Keys != null && sme.SemanticId.Keys.Count != 0)
                                                                            result += " " + sme.SemanticId.Keys[ 0 ].Value;
                                                                    }

                                                                    if (selectParameters.Contains("%path"))
                                                                    {
                                                                        result += " " + aas.IdShort + "/" + sm.IdShort + "/" + path + sme.IdShort;
                                                                    }

                                                                    result += "\n";
                                                                    totalFound++;
                                                                }

                                                                if (select == "submodel" || select == "submodelid")
                                                                {
                                                                    if (storeResult)
                                                                    {
                                                                        if (!storeSubmodels.Contains(sm))
                                                                            storeSubmodels.Add(sm);
                                                                    }

                                                                    foundInSubmodel++;
                                                                    totalFound++;
                                                                }

                                                                if (select == "aas" || select == "aasid")
                                                                {
                                                                    if (storeResult)
                                                                    {
                                                                        if (!storeAas.Contains(aas))
                                                                            storeAas.Add(aas);
                                                                    }

                                                                    foundInAas++;
                                                                    totalFound++;
                                                                }

                                                                if (select == "repository")
                                                                {
                                                                    foundInRepository++;
                                                                    totalFound++;
                                                                }
                                                            }
                                                        }
                                                    }

                                                    if (sme is SubmodelElementCollection smc)
                                                    {
                                                        stack.Add(level);
                                                        iStack.Add(iLevel + 1);
                                                        pathStack.Add(path);
                                                        semanticIdPathStack.Add(semanticIdPath);
                                                        depth++;
                                                        path += smc.IdShort + "/";
                                                        string smcSemanticId = "";
                                                        if (smc.SemanticId != null && smc.SemanticId.Keys != null && smc.SemanticId.Keys.Count != 0)
                                                            smcSemanticId = smc.SemanticId.Keys[ 0 ].Value;
                                                        semanticIdPath += smcSemanticId + ".";
                                                        level = smc.Value;
                                                        iLevel = 0;
                                                        continue;
                                                    }

                                                    iLevel++;
                                                }

                                                depth--;
                                                if (depth >= 0)
                                                {
                                                    level = stack[ depth ];
                                                    stack.RemoveAt(depth);
                                                    iLevel = iStack[ depth ];
                                                    iStack.RemoveAt(depth);
                                                    path = pathStack[ depth ];
                                                    pathStack.RemoveAt(depth);
                                                    semanticIdPath = semanticIdPathStack[ depth ];
                                                    semanticIdPathStack.RemoveAt(depth);
                                                }
                                            }
                                        }

                                        if (select == "submodel" && foundInSubmodel != 0 && accessSubmodel)
                                        {
                                            result += select + " found " + foundInSubmodel;

                                            if (!selectParameters.Contains("!endpoint"))
                                            {
                                                string link = AasxServer.Program.externalBlazor +
                                                              "/submodels/" + Base64UrlEncoder.Encode(sm.Id);
                                                result += " " + "<a href=\"" + link + "\" target=\"_blank\">" + link + "</a>";
                                            }

                                            if (selectParameters.Contains("%id "))
                                            {
                                                result += " " + sm.Id;
                                            }

                                            if (selectParameters.Contains("%idshort"))
                                            {
                                                result += " " + sm.IdShort;
                                            }

                                            if (selectParameters.Contains("%semanticid"))
                                            {
                                                if (sm.SemanticId != null && sm.SemanticId.Keys != null && sm.SemanticId.Keys.Count != 0)
                                                    result += " " + sm.SemanticId.Keys[ 0 ].Value;
                                            }

                                            result += "\n";
                                        }

                                        if (select == "submodelid" && foundInSubmodel != 0 && accessSubmodel)
                                            result += "%id == \"" + sm.Id + "\"\n";
                                    } // submodels

                                    if (select == "aas" && foundInAas != 0)
                                    {
                                        result += select + " found " + foundInAas;

                                        if (!selectParameters.Contains("!endpoint"))
                                        {
                                            string link = AasxServer.Program.externalBlazor +
                                                          "/shells/" + Base64UrlEncoder.Encode(aas.Id);
                                            result += " " + "<a href=\"" + link + "\" target=\"_blank\">" + link + "</a>";
                                        }

                                        if (selectParameters.Contains("%id "))
                                        {
                                            result += " " + aas.Id;
                                        }

                                        if (selectParameters.Contains("%idshort"))
                                        {
                                            result += " " + aas.IdShort;
                                        }

                                        if (selectParameters.Contains("%assetid"))
                                        {
                                            if (aas.AssetInformation.GlobalAssetId != null)
                                                result += " " + aas.AssetInformation.GlobalAssetId;
                                        }

                                        result += "\n";
                                    }

                                    if (select == "aasid" && foundInAas != 0)
                                        result += "%id == \"" + aas.Id + "\"\n";
                                } // AAS
                            } // AAS-ENV
                        }

                        if (select == "repository" && foundInRepository != 0)
                            result += select + " found " + foundInRepository + " " + AasxServer.Program.externalBlazor + "\n";
                        else
                            result += "repository totalfound " + totalFound + " " + AasxServer.Program.externalBlazor + "\n";
                        result += "\n";

                        if (storeResult)
                        {
                            storeResult = false;
                            storeAasLast = storeAas;
                            storeAas = new List<IAssetAdministrationShell>();
                            storeSubmodelsLast = storeSubmodels;
                            storeSubmodels = new List<ISubmodel>();
                            storeSmesLast = storeSmes;
                            storeSmes = new List<ISubmodelElement>();
                            totalFound = 0;
                            foundInRepository = 0;
                            result += "\n";
                        }
                    }
                }

                return result;
            }

            // query
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/query/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/query/(/|)$") ]
            public IHttpContext Query(IHttpContext context)
            {
                allowCORS(context);

                string result = "";

                string restPath = context.Request.PathInfo;
                string query = restPath.Replace("/query/", "");

                result = runQuery(query, context.Request.Payload);

                context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.Ok, result);

                // context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.NotFound, $"Operation not allowed!");
                return context;
            }

            // exit application
            [ RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/exit/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/exit/([^/]+)(/|)$") ]
            public IHttpContext Exit(IHttpContext context)
            {
                string restPath = context.Request.PathInfo;
                string requestSecret1 = restPath.Replace("/exit/", "");
                string secret1 = null;
                bool error = false;

                if (System.IO.File.Exists("SECRET.DAT"))
                {
                    try
                    {
                        using (StreamReader sr = new StreamReader("SECRET.DAT"))
                        {
                            secret1 = sr.ReadLine();
                        }
                    }
                    catch
                    {
                        error = true;
                    }

                    ;
                }

                if (secret1 == null || requestSecret1 != secret1)
                    error = true;

                if (!error)
                {
                    System.Environment.Exit(1);
                }

                allowCORS(context);
                context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.NotFound, $"Operation not allowed!");
                return context;
            }

            // set secret string to write to API
            [ RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/secret/([^/]+)/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/secret/([^/]+)/([^/]+)(/|)$") ]
            public IHttpContext Secret(IHttpContext context)
            {
                string requestSecret1 = null;
                string requestSecret2 = null;
                string secret1 = null;
                bool error = false;

                string restPath = context.Request.PathInfo;
                restPath = restPath.Replace("/secret/", "");
                string[] split = restPath.Split('/');
                if (split.Count() == 2)
                {
                    requestSecret1 = split[ 0 ];
                    requestSecret2 = split[ 1 ];
                }

                if (System.IO.File.Exists("SECRET.DAT"))
                {
                    try
                    {
                        using (StreamReader sr = new StreamReader("SECRET.DAT"))
                        {
                            secret1 = sr.ReadLine();
                        }
                    }
                    catch
                    {
                        error = true;
                    }

                    ;
                }

                if (secret1 == null || requestSecret1 != secret1)
                    error = true;

                if (!error)
                {
                    AasxServer.Program.secretStringAPI = requestSecret2;
                    context.Response.StatusCode = Grapevine.Shared.HttpStatusCode.Ok;
                    string txt = "OK";
                    allowCORS(context);
                    context.Response.ContentType = ContentType.TEXT;
                    context.Response.ContentEncoding = Encoding.UTF8;
                    context.Response.ContentLength64 = txt.Length;
                    context.Response.SendResponse(txt);
                    return context;
                }

                context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.NotFound, $"Operation not allowed!");
                return context;
            }

            // test data server

            public static int varInt1 = -100; // -100..100
            public static int varInt2 = 0; // 0..10
            public static double varFloat3 = 0; // sin(varInt1/30);

            class testData
            {
                public int varInt1;
                public int varInt2;
                public float varFloat3;
            }

            void sendJson(IHttpContext context, object o)
            {
                // Serialize the object to JSON
                var options = new JsonSerializerOptions
                              {
                                  WriteIndented = true // Formatting.Indented equivalent
                              };
                var json = JsonSerializer.Serialize(o, options);

                // Convert JSON string to byte array
                var buffer = Encoding.UTF8.GetBytes(json);
                var length = buffer.Length;

                context.Response.ContentType = ContentType.JSON;
                context.Response.ContentEncoding = Encoding.UTF8;
                context.Response.ContentLength64 = length;

                // Send the response
                context.Response.SendResponse(buffer);
            }

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/data4(/|)$") ]
            public IHttpContext GetData4(IHttpContext context)
            {
                varInt1++;
                if (varInt1 > 100)
                    varInt1 = -100;
                varInt2++;
                if (varInt2 > 10)
                    varInt2 = 0;
                // varFloat3 = Math.Sin(varInt1 * 180 / 100);
                varFloat3 = 100 * Math.Sin((1.0 * varInt1 / 360.0) * 10);

                testData td = new testData();
                td.varInt1 = varInt1;
                td.varInt2 = varInt2;
                td.varFloat3 = (float) varFloat3;

                sendJson(context, td);
                return context;
            }

            public class DeletedListItem
            {
                public ISubmodel sm;
                public IReferable rf;
            }

            public static List<DeletedListItem> deletedList = new List<DeletedListItem>();
            public static DateTime olderDeletedTimeStamp = new DateTime();

            // get event messages
            public class eventMessage
            {
                public DateTime dt;
                public string operation = "";
                public string obj = "";
                public string data = "";

                public static void add(IReferable o, string op, ISubmodel rootSubmodel, ulong changeCount)
                {
                    // if (o is SubmodelElementCollection smec)
                    if (o is ISubmodelElement smec)
                    {
                        string json = "";

                        AasPayloadStructuralChangeItem.ChangeReason reason = AasPayloadStructuralChangeItem.ChangeReason.Create;
                        switch (op)
                        {
                            case "Add":
                                reason = AasPayloadStructuralChangeItem.ChangeReason.Create;
                                json = JsonSerializer.Serialize(smec, new JsonSerializerOptions
                                                                      {
                                                                          WriteIndented    = true,
                                                                          IgnoreNullValues = true
                                                                      });
                                break;
                            case "Remove":
                                reason = AasPayloadStructuralChangeItem.ChangeReason.Delete;
                                break;
                        }

                        rootSubmodel.SetAllParents();
                        List<IKey> keys = new();

#if MICHA
                        // keys were in the reverse order
                        keys = smec.GetModelReference()?.Keys;
                        if (keys.Count != 0)
                            keys.Remove(keys.Last());
#else
                        while (smec != null)
                        {
                            keys.Add(AdminShellV20.Key.CreateNew("SMEC", false, "SMEC", smec.IdShort));
                            smec = (smec.parent as SubmodelElementCollection);
                        }
                        keys.Add(AdminShellV20.Key.CreateNew("SM", false, "SM", rootSubmodel.IdShort));
#endif


                        AasPayloadStructuralChangeItem change = new AasPayloadStructuralChangeItem(
                            changeCount, o.TimeStamp, reason, keys, json);
                        changeClass.Changes.Add(change);
                        if (changeClass.Changes.Count > 100)
                            changeClass.Changes.RemoveAt(0);

                        if (op == "Remove")
                        {
                            o.TimeStamp = DateTime.UtcNow;
                            IReferable x = o;
                            /*
                            string path = x.IdShort;
                            while (x.parent != null && x != x.parent)
                            {
                                x = x.parent;
                                path = x.IdShort + "." + path;
                            }
                            o.IdShort = path;
                            */
                            deletedList.Add(new DeletedListItem() {sm = rootSubmodel, rf = o});
                            if (deletedList.Count > 1000 && deletedList[ 0 ].rf != null)
                            {
                                olderDeletedTimeStamp = deletedList[ 0 ].rf.TimeStamp;
                                deletedList.RemoveAt(0);
                            }
                        }
                    }
                }
            }

            public static string posttimeseriesPayload = "";

            [ RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/posttimeseries(/|)$") ]
            public IHttpContext posttimeseries(IHttpContext context)
            {
                Console.WriteLine("posttimeseries:");
                posttimeseriesPayload = context.Request.Payload;
                Console.WriteLine(posttimeseriesPayload);

                context.Response.ContentType = ContentType.HTML;
                context.Response.ContentEncoding = System.Text.Encoding.UTF8;
                context.Response.SendResponse("OK");
                context.Response.StatusCode = Grapevine.Shared.HttpStatusCode.Ok;
                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/calculatecfp/aas/([^/]+)(/|)$") ]
            public IHttpContext calculatecfp(IHttpContext context)
            {
                string restPath = context.Request.PathInfo;
                int aasIndex = -1;
                string result = "NONE";

                if (restPath.Contains("/aas/"))
                {
                    // specific AAS
                    string[] split = restPath.Split('/');
                    if (split[ 2 ] == "aas")
                    {
                        try
                        {
                            if (!int.TryParse(split[ 3 ], out aasIndex))
                                aasIndex = -1;
                            if (aasIndex >= 0)
                            {
                                AasxServer.AasxTask.operation_calculate_cfp(null, aasIndex, DateTime.UtcNow);
                                result = "OK";
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                context.Response.ContentType = ContentType.HTML;
                context.Response.ContentEncoding = System.Text.Encoding.UTF8;
                context.Response.SendResponse(result);
                context.Response.StatusCode = Grapevine.Shared.HttpStatusCode.Ok;
                return context;
            }

            public static AasPayloadStructuralChange changeClass = new AasPayloadStructuralChange();
            // public static int eventsCount = 0;

            private static bool _setAllParentsExecuted = false;

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages/values(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages/time/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages/deltasecs/(\\d+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages/aas/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages/aas/([^/]+)/values(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages/aas/([^/]+)/time/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages/aas/([^/]+)/deltasecs/(\\d+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/([^/]+)/geteventmessages(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/([^/]+)/geteventmessages/values(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/([^/]+)/geteventmessages/time/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/([^/]+)/geteventmessages/deltasecs/(\\d+)(/|)$") ]
            public IHttpContext GetEventMessages(IHttpContext context)
            {
                // 
                // Configuration of operation mode
                //

                DateTime minimumDate = new DateTime();
                bool doUpdate = true;
                bool doCreateDelete = true;
                string restPath = context.Request.PathInfo;
                int aasIndex = -1;

                if (restPath.Contains("/aas/"))
                {
                    // specific AAS
                    string[] split = restPath.Split('/');
                    if (split[ 2 ] == "aas")
                    {
                        try
                        {
                            if (!int.TryParse(split[ 3 ], out aasIndex))
                                aasIndex = -1;
                            if (aasIndex >= 0)
                            {
                                restPath = "";
                                for (int i = 1; i < split.Length; i++)
                                {
                                    if (i != 2 && i != 3)
                                    {
                                        restPath += "/" + split[ i ];
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                    }

                    if (split[ 1 ] == "aas")
                    {
                        try
                        {
                            if (!int.TryParse(split[ 2 ], out aasIndex))
                                aasIndex = -1;
                            if (aasIndex >= 0)
                            {
                                restPath = "";
                                for (int i = 1; i < split.Length; i++)
                                {
                                    if (i != 1 && i != 2)
                                    {
                                        restPath += "/" + split[ i ];
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                if (restPath.Contains("/values"))
                {
                    doCreateDelete = false;
                }
                else
                {
                    if (restPath.StartsWith("/geteventmessages/time/"))
                    {
                        try
                        {
                            minimumDate = DateTime.Parse(restPath.Substring("/geteventmessages/time/".Length)).ToUniversalTime();
                        }
                        catch
                        {
                        }
                    }

                    if (restPath.StartsWith("/geteventmessages/deltasecs/"))
                    {
                        try
                        {
                            var secs = restPath.Substring("/geteventmessages/deltasecs/".Length);
                            if (int.TryParse(secs, out int i))
                                minimumDate = DateTime.UtcNow.AddSeconds(-1.0 * i);
                        }
                        catch
                        {
                        }
                    }
                }

                //
                // Set parents for all childs.
                // Note: this has to be done only once for AASX Server, therefore a better place than
                // here could be figured out
                //

                if (!_setAllParentsExecuted)
                {
                    _setAllParentsExecuted = true;

                    if (AasxServer.Program.env != null)
                        foreach (var e in AasxServer.Program.env)
                            if (e?.AasEnv?.Submodels != null)
                                foreach (var sm in e.AasEnv.Submodels)
                                    if (sm != null)
                                        sm.SetAllParents();
                }

                //
                // Restructuring of sourece code
                // * outer loop is over all AAS-Env and Submodels
                // * find event elements in Submodels
                // * send deletes
                // * send creates & updates
                //

                var envelopes = new List<AasEventMsgEnvelope>();

                int aascount = AasxServer.Program.env.Length;
                for (int i = 0; i < aascount; i++)
                {
                    if (aasIndex >= 0 && i != aasIndex)
                        continue;

                    var env = AasxServer.Program.env[ i ];
                    if (env?.AasEnv?.AssetAdministrationShells == null)
                        continue;

                    foreach (var aas in env.AasEnv.AssetAdministrationShells)
                    {
                        if (aas?.Submodels == null)
                            continue;

                        foreach (var smr in aas.Submodels)
                        {
                            // find Submodel
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm == null)
                                continue;

                            // find a matching event element
                            foreach (var bev in sm.FindDeep<BasicEventElement>())
                            {
                                // find interesting event?
                                //if (true == bev.SemanticId?.MatchesExactlyOneId(
                                //    id: "https://admin-shell.io/tmp/AAS/Events/UpdateValueOutwards",
                                //    matchMode: Key.MatchMode.Relaxed))
                                if (true == bev.SemanticId?.Matches(id: "https://admin-shell.io/tmp/AAS/Events/UpdateValueOutwards"))
                                {
                                    doUpdate = true;
                                    doCreateDelete = false;
                                }
                                else if (true == bev.SemanticId?.Matches(id: "https://admin-shell.io/tmp/AAS/Events/StructureChangeOutwards"))
                                {
                                    doUpdate = false;
                                    doCreateDelete = true;
                                }
                                else
                                    continue;

                                // find obseverved as well
                                if (bev.Observed == null && bev.Observed.Keys.Count < 1)
                                    continue;
                                var obs = env.AasEnv.FindReferableByReference(bev.Observed);
                                if (obs == null)
                                    continue;

                                // obseverved semantic id is pain in the ..
                                IReference obsSemId = null;
                                if (obs is Submodel obssm)
                                    obsSemId = obssm.SemanticId;
                                if (obs is ISubmodelElement obssme)
                                    obsSemId = obssme.SemanticId;

                                //
                                // Create event outer message
                                //

                                var eventsOuter = new AasEventMsgEnvelope(
                                    DateTime.UtcNow,
                                    source: bev.GetModelReference(),
                                    sourceSemanticId: bev.SemanticId,
                                    observableReference: bev.Observed,
                                    observableSemanticId: obsSemId);

                                // directly create lists of update value and structural change events

                                var plStruct = new AasPayloadStructuralChange();
                                var plUpdate = new AasPayloadUpdateValue();

                                string[] modes = {"CREATE", "UPDATE"};

                                //
                                // Check for deletes
                                //
                                if (doCreateDelete)
                                {
                                    foreach (var d in deletedList)
                                    {
                                        if (d.rf == null || d.sm != sm)
                                            continue;
                                        if (d.rf.TimeStamp > minimumDate)
                                        {
                                            // get the path
                                            List<IKey> p2 = null;
                                            if (d.rf is Submodel delsm)
                                                p2 = delsm?.GetModelReference()?.Keys;
                                            if (d.rf is ISubmodelElement delsme)
                                                p2 = delsme?.GetModelReference()?.Keys;
                                            if (p2 == null)
                                                continue;

                                            // prepare p2 to be relative path to observable
                                            if (true == p2?.StartsWith(bev.Observed?.Keys))
                                                p2.RemoveRange(0, bev.Observed.Keys.Count);

                                            // make payload
                                            var pliDel = new AasPayloadStructuralChangeItem(
                                                count: 1,
                                                timeStamp: d.rf.TimeStamp,
                                                AasPayloadStructuralChangeItem.ChangeReason.Delete,
                                                path: p2);

                                            // add
                                            plStruct.Changes.Add(pliDel);
                                        }
                                    }
                                }
                                else
                                {
                                }

                                //
                                // Create & update
                                //

                                //for (int imode = 0; imode < modes.Length; imode++)
                                //{
                                if ((doCreateDelete || doUpdate) == false)
                                    throw new Exception("invalid flags");

                                DateTime? diffTimeStamp = sm.TimeStamp;
                                var strMode = "";
                                if (doCreateDelete)
                                    strMode = "CREATE";
                                if (doUpdate)
                                    strMode = "UPDATE";
                                if (strMode != "")
                                    if (diffTimeStamp > minimumDate)
                                    {
                                        ;
                                        foreach (var sme in sm.SubmodelElements)
                                            GetEventMsgRecurseDiff(
                                                strMode,
                                                plStruct, plUpdate,
                                                sme,
                                                minimumDate, doUpdate, doCreateDelete,
                                                bev.Observed?.Keys);
                                    }
                                //}

                                // prepare message envelope and remember

                                if (plStruct.Changes.Count > 0)
                                    eventsOuter.Payloads.Add(plStruct);

                                if (plUpdate.Values.Count > 0)
                                    eventsOuter.Payloads.Add(plUpdate);

                                if (eventsOuter.Payloads.Count > 0)
                                    envelopes.Add(eventsOuter);
                            } // matching events
                        } // submodels
                    } // AAS
                } // AAS-ENV

                //
                // Serialize event message and send
                //

                SendJsonResponse(context, envelopes.ToArray());

                return context;
            }

            static void GetEventMsgRecurseDiff(
                string mode,
                AasPayloadStructuralChange plStruct,
                AasPayloadUpdateValue plUpdate,
                ISubmodelElement sme, DateTime minimumDate,
                bool doUpdate, bool doCreateDelete,
                List<IKey> observablePath = null)
            {
                if (!(sme is SubmodelElementCollection))
                {
                    if ((mode == "CREATE" && sme.TimeStampCreate > minimumDate) ||
                        (mode != "CREATE" && sme.TimeStamp > minimumDate && sme.TimeStamp != sme.TimeStampCreate))
                    {
                        // prepare p2 to be relative path to observable
                        var p2 = sme.GetModelReference()?.Keys;
                        //if (true == p2?.StartsWith(observablePath, matchMode: AdminShell.Key.MatchMode.Relaxed))
                        if (true /*== p2?.StartsWith(observablePath)*/)
                            p2.RemoveRange(0, observablePath.Count);

                        if (mode == "CREATE")
                        {
                            if ( /* doCreateDelete && */ plStruct != null)
                                plStruct.Changes.Add(new AasPayloadStructuralChangeItem(
                                    count: 1,
                                    timeStamp: sme.TimeStamp,
                                    AasPayloadStructuralChangeItem.ChangeReason.Create,
                                    path: p2,
                                    // Assumption: models will be serialized correctly
                                    data: JsonSerializer.Serialize(sme)));
                        }
                        else
                        {
                            if ( /* doUpdate && */ plUpdate != null)
                            {
                                var val = sme.ValueAsText();
                                if (sme is Blob blob)
                                    // take BLOB as "large" text
                                    val = Encoding.UTF8.GetString(blob.Value);
                                plUpdate.Values.Add(new AasPayloadUpdateValueItem(
                                    path: p2,
                                    val));
                            }
                        }
                    }

                    return;
                }

                var smec = sme as SubmodelElementCollection;
                if (mode == "CREATE" || smec.TimeStamp > minimumDate)
                {
                    bool deeper = false;
                    //
                    if (doUpdate)
                    {
                        deeper = true;
                    }
                    else
                        //
                    {
                        if (smec.Value.Count == 1)
                        {
                            deeper = true;
                        }
                        else
                        {
                            // replace foreach by explicit loop for multiple threads
                            int i = 0;
                            while (i < smec.Value.Count)
                            {
                                var sme2 = smec.Value[ i ];
                                if (sme2.TimeStamp != smec.TimeStamp)
                                {
                                    deeper = true;
                                    break;
                                }

                                i++;
                            }
                        }
                    }

                    if (deeper)
                    {
                        // replace foreach by explicit loop for multiple threads
                        int i = 0;
                        while (i < smec.Value.Count)
                        {
                            var sme2 = smec.Value[ i ];
                            GetEventMsgRecurseDiff(
                                mode,
                                plStruct, plUpdate,
                                sme2, minimumDate, doUpdate, doCreateDelete, observablePath);
                            i++;
                        }

                        return;
                    }

                    // prepare p2 to be relative path to observable
                    var p2 = sme.GetModelReference()?.Keys;
                    if (true /*== p2?.StartsWith(observablePath)*/)
                        p2.RemoveRange(0, observablePath.Count);

                    if (mode == "CREATE")
                    {
                        if (sme.TimeStampCreate > minimumDate)
                        {
                            if ( /* doCreateDelete && */ plStruct != null)
                                plStruct.Changes.Add(new AasPayloadStructuralChangeItem(
                                    count: 1,
                                    timeStamp: sme.TimeStamp,
                                    AasPayloadStructuralChangeItem.ChangeReason.Create,
                                    path: p2,
                                    // Assumption: models will be serialized correctly
                                    data: JsonSerializer.Serialize(sme)));
                        }
                    }
                    else if (sme.TimeStamp > minimumDate && sme.TimeStamp != sme.TimeStampCreate)
                    {
                        if ( /* doUpdate && */ plUpdate != null)
                            plUpdate.Values.Add(new AasPayloadUpdateValueItem(
                                path: p2,
                                sme.ValueAsText()));
                    }
                }
            }

            public static void allowCORS(Grapevine.Interfaces.Server.IHttpContext context)
            {
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "origin, content-type, accept, authorization");
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS, HEAD");
            }

            public static void SendJsonResponse(IHttpContext context, object obj)
            {
                // Configure JSON serialization options
                var options = AasxIntegrationBase.AasxPluginOptionSerialization.GetDefaultJsonOptions(
                                                                                                      new[] { typeof(AdminShellEvents.AasEventMsgEnvelope) });

                // Additional options settings (not directly translatable to System.Text.Json)
                // options.TypeNameHandling = TypeNameHandling.Auto; // No direct equivalent
                // options.Formatting = Formatting.Indented; // Handled by JsonSerializerOptions

                // Serialize the object to JSON
                var json = JsonSerializer.Serialize(obj, options);

                // Convert JSON string to byte array
                var buffer = Encoding.UTF8.GetBytes(json);
                var length = buffer.Length;

                // Handle 'refresh' query parameter
                var queryString = context.Request.QueryString;
                string refresh = queryString[ "refresh" ];
                if (refresh != null && refresh != "")
                {
                    context.Response.Headers.Remove("Refresh");
                    context.Response.Headers.Add("Refresh", refresh);
                }

                // Set CORS headers (assuming allowCORS(context) handles this)
                allowCORS(context);

                context.Response.ContentType = ContentType.JSON;
                context.Response.ContentEncoding = Encoding.UTF8;
                context.Response.ContentLength64 = length;

                // Send the response
                context.Response.SendResponse(buffer);
            }
            
            public class diffEntry
            {
                public string mode = "";
                public string path = "";
                public string type = "";
                public DateTime timeStamp = new DateTime();
                public string value = "";
            }

            private static void addEntry(bool diffJson, ref string diffText, ref List<diffEntry> diffList,
                string mode = "", string path = "", string type = "", DateTime timeStamp = new DateTime(), string value = "")
            {
                if (!diffJson)
                {
                    switch (mode)
                    {
                        case "OPEN":
                            diffText += "<table border=1 cellpadding=4><tbody>";
                            break;
                        case "CLOSE":
                            diffText += "</tbody></table>";
                            break;
                        case "DELETE":
                            if (type == "")
                            {
                                diffText += "<tr><td>DELETE</td><td><b>***Deleted_items_before***</b></td><td>ERROR</td><td>" +
                                            timeStamp.ToString("yy-MM-dd HH:mm:ss.fff") + "</td></tr>";
                            }
                            else
                            {
                                diffText += "<tr><td>DELETE</td><td><b>" + path + "</b></td><td>" + type + "</td><td>" +
                                            timeStamp.ToString("yy-MM-dd HH:mm:ss.fff") + "</td></tr>";
                            }

                            break;
                        case "CREATE":
                        case "UPDATE":
                            diffText += "<tr><td>" + mode + "</td><td><b>" + path +
                                        "</b></td><td>" + type + "</td><td>" +
                                        timeStamp.ToString("yy-MM-dd HH:mm:ss.fff") + "</td>";
                            if (value != "")
                                diffText += "<td>" + value + "</td>";
                            diffText += "</tr>";
                            break;
                    }
                }
                else
                {
                    switch (mode)
                    {
                        case "OPEN":
                        case "CLOSE":
                            break;
                        case "DELETE":
                        case "CREATE":
                        case "UPDATE":
                            var entry = new diffEntry();
                            entry.mode = mode;
                            entry.path = path;
                            entry.type = type;
                            entry.timeStamp = timeStamp;
                            entry.value = value;
                            diffList.Add(entry);
                            break;
                    }
                }
            }

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff/update(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff/update/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff/aas/([^/]+)/time/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff/aas/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff/aas/([^/]+)/update(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff/aas/([^/]+)/update/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson/update(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson/update/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson/aas/([^/]+)/time/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson/aas/([^/]+)(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson/aas/([^/]+)/update(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson/aas/([^/]+)/update/([^/]+)(/|)$") ]
            public IHttpContext GetDiff(IHttpContext context)
            {
                string[] modes = {"DELETE", "CREATE", "UPDATE"};
                DateTime minimumDate = new DateTime();
                bool deep = false;
                int seconds = 0;
                string searchPath = "";
                int searchPathLen = 0;
                bool diffJson = false;

                var queryString = context.Request.QueryString;
                string refresh = queryString[ "refresh" ];
                if (refresh != null && refresh != "")
                {
                    context.Response.Headers.Remove("Refresh");
                    context.Response.Headers.Add("Refresh", refresh);
                }

                string m = queryString[ "mode" ];
                if (m != null && m != "")
                {
                    try
                    {
                        modes = m.Split(',');
                    }
                    catch
                    {
                    }
                }

                string time = queryString[ "time" ];
                if (time != null && time != "")
                {
                    try
                    {
                        minimumDate = DateTime.Parse(time).ToUniversalTime();
                    }
                    catch
                    {
                    }
                }

                string auto = queryString[ "auto" ];
                if (auto != null && auto != "")
                {
                    try
                    {
                        seconds = Convert.ToInt32(auto);
                        minimumDate = DateTime.UtcNow - new TimeSpan(0, 0, seconds);
                    }
                    catch
                    {
                    }
                }

                string dd = queryString[ "deep" ];
                if (dd != null && dd != "")
                {
                    deep = true;
                }

                {
                    string path = queryString[ "path" ];
                    if (path != null && path != "")
                    {
                        searchPath = path;
                        searchPathLen = searchPath.Length;
                    }
                }

                string restPath = context.Request.PathInfo;
                if (restPath.Contains("/diffjson/"))
                {
                    diffJson = true;
                    restPath = restPath.Replace("/diffjson/", "/diff/");
                }

                int aasIndex = -1;

                if (restPath.Contains("/aas/"))
                {
                    // specific AAS
                    string[] split = restPath.Split('/');
                    if (split[ 2 ] == "aas")
                    {
                        try
                        {
                            if (!int.TryParse(split[ 3 ], out aasIndex))
                                aasIndex = -1;
                            if (aasIndex >= 0)
                            {
                                restPath = "";
                                for (int i = 1; i < split.Length; i++)
                                {
                                    if (i != 2 && i != 3)
                                    {
                                        restPath += "/" + split[ i ];
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                string diffText = "";
                List<diffEntry> diffList = new List<diffEntry>();

                addEntry(diffJson, ref diffText, ref diffList, "OPEN");

                int aascount = AasxServer.Program.env.Length;

                for (int imode = 0; imode < modes.Length; imode++)
                {
                    string mode = modes[ imode ];

                    if (mode == "DELETE")
                    {
                        if (olderDeletedTimeStamp > minimumDate)
                        {
                            addEntry(diffJson, ref diffText, ref diffList,
                                "DELETE", "***Deleted_items_before***", "", olderDeletedTimeStamp);
                        }

                        foreach (var d in deletedList)
                        {
                            if (d.rf == null)
                                continue;
                            if (d.rf.TimeStamp > minimumDate)
                            {
                                var x = d.rf;
                                string path = x.IdShort;
                                while (x.Parent != null && x != x.Parent)
                                {
                                    x = (IReferable) x.Parent;
                                    path = x.IdShort + "." + path;
                                }

                                if (searchPath == "" ||
                                    (searchPath.Length <= path.Length && searchPath == path.Substring(0, searchPath.Length)))
                                {
                                    addEntry(diffJson, ref diffText, ref diffList,
                                        "DELETE", path, "SMEC", d.rf.TimeStamp);
                                }
                            }
                        }

                        continue;
                    }

                    for (int i = 0; i < aascount; i++)
                    {
                        if (aasIndex >= 0 && i != aasIndex)
                            continue;

                        var env = AasxServer.Program.env[ i ];
                        if (env != null)
                        {
                            var aas = env.AasEnv.AssetAdministrationShells[ 0 ];
                            if (aas.Submodels != null && aas.Submodels.Count > 0)
                            {
                                DateTime diffTimeStamp = new();
                                diffTimeStamp = aas.TimeStampCreate;
                                if (diffTimeStamp > minimumDate)
                                {
                                    if (mode == "CREATE" || aas.TimeStamp != aas.TimeStampCreate)
                                    {
                                        string p = aas.IdShort;
                                        if (searchPath == "" || (p.Length <= searchPathLen && p == searchPath.Substring(0, p.Length)))
                                        {
                                            addEntry(diffJson, ref diffText, ref diffList,
                                                mode, aas.IdShort, "AAS", aas.TimeStamp);
                                        }
                                    }
                                }

                                foreach (var smr in aas.Submodels)
                                {
                                    var sm = env.AasEnv.FindSubmodel(smr);
                                    if (sm != null && sm.IdShort != null)
                                    {
                                        if (sm.TimeStamp > minimumDate)
                                        {
                                            string p = sm.IdShort;
                                            if (mode == "CREATE" && sm.TimeStampCreate > minimumDate)
                                            {
                                                // string p = aas.IdShort + "." + sm.IdShort;
                                                if (searchPath == "" || (p.Length <= searchPathLen && p == searchPath.Substring(0, p.Length)))
                                                {
                                                    addEntry(diffJson, ref diffText, ref diffList,
                                                        mode, p, "SM", (DateTime) sm.TimeStamp);
                                                }
                                            }

                                            foreach (var sme in sm.SubmodelElements)
                                                checkDiff(diffJson, ref diffText, ref diffList,
                                                    mode, p + ".", sme,
                                                    minimumDate, deep, searchPath);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                addEntry(diffJson, ref diffText, ref diffList, "CLOSE");

                if (!diffJson)
                {
                    context.Response.ContentType = ContentType.HTML;
                    context.Response.ContentEncoding = Encoding.UTF8;
                    context.Response.ContentLength64 = diffText.Length;
                    context.Response.SendResponse(diffText);
                }
                else
                {
                    SendJsonResponse(context, diffList);
                }

                return context;
            }

            static void checkDiff(bool diffJson, ref string diffText, ref List<diffEntry> diffList,
                string mode, string path, ISubmodelElement sme,
                DateTime minimumDate, bool deep, string searchPath)
            {
                if (!(sme is SubmodelElementCollection))
                {
                    if ((mode == "CREATE" && sme.TimeStampCreate > minimumDate) ||
                        (mode != "CREATE" && sme.TimeStamp > minimumDate && sme.TimeStamp != sme.TimeStampCreate))
                    {
                        if (searchPath != "")
                        {
                            string p = path + sme.IdShort;
                            if (!(searchPath.Length <= p.Length && searchPath == p.Substring(0, searchPath.Length)))
                                return;
                        }

                        string value = "";
                        if (mode != "CREATE")
                            value = sme.ValueAsText();
                        addEntry(diffJson, ref diffText, ref diffList,
                            mode, path + sme.IdShort, "SME", sme.TimeStamp, value);
                        return;
                    }

                    return;
                }

                var smec = sme as SubmodelElementCollection;
                if (mode == "CREATE" || sme.TimeStamp > minimumDate)
                {
                    bool deeper = false;
                    if (deep)
                    {
                        deeper = true;
                    }
                    else
                    {
                        if (smec.Value.Count == 1)
                        {
                            deeper = true;
                        }
                        else
                        {
                            foreach (var sme2 in smec.Value)
                                if (sme2.TimeStamp != smec.TimeStamp)
                                {
                                    deeper = true;
                                    break;
                                }
                        }
                    }

                    if (deeper)
                    {
                        foreach (var sme2 in smec.Value)
                            checkDiff(diffJson, ref diffText, ref diffList,
                                mode, path + sme.IdShort + ".", sme2,
                                minimumDate, deep, searchPath);
                        return;
                    }

                    if ((mode == "CREATE" && sme.TimeStampCreate > minimumDate) ||
                        (mode != "CREATE" && sme.TimeStamp > minimumDate && sme.TimeStamp != sme.TimeStampCreate))
                    {
                        if (searchPath != "")
                        {
                            string p = path + sme.IdShort;
                            if (!(searchPath.Length <= p.Length && searchPath == p.Substring(0, searchPath.Length)))
                                return;
                        }

                        addEntry(diffJson, ref diffText, ref diffList,
                            mode, path + smec.IdShort, "SMEC", smec.TimeStamp);
                    }
                }

                return;
            }

            public static AasxHttpContextHelper helper = null;

            // get authserver

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/authserver(/|)$") ]
            public IHttpContext GetAuthserver(IHttpContext context)
            {
                var txt = AasxServer.Program.redirectServer;

                context.Response.ContentType = ContentType.TEXT;
                context.Response.ContentEncoding = Encoding.UTF8;
                context.Response.ContentLength64 = txt.Length;
                context.Response.SendResponse(txt);

                return context;
            }

            // Basic AAS + Asset 
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))(|/core|/complete|/thumbnail|/aasenv|/aasenvjson)(/|)$") ]
            public IHttpContext GetAasAndAsset(IHttpContext context)
            {
                if (context.Request.PathInfo.Contains("geteventmessages"))
                {
                    return GetEventMessages(context);
                }

                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    if (helper.PathEndsWith(context, "thumbnail"))
                    {
                        helper.EvalGetAasThumbnail(context, m.Groups[ 1 ].ToString());
                    }
                    else if (helper.PathEndsWith(context, "aasenv") || helper.PathEndsWith(context, "aasenvjson"))
                    {
                        helper.EvalGetAasEnv(context, m.Groups[ 1 ].ToString());
                    }
                    else
                    {
                        var complete = helper.PathEndsWith(context, "complete");
                        helper.EvalGetAasAndAsset(context, m.Groups[ 1 ].ToString(), complete: complete);
                    }
                }

                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/aas(/|)$") ]
            public IHttpContext PutAas(IHttpContext context)
            {
                helper.EvalPutAas(context);
                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/aasx/server(/|)$") ]
            public IHttpContext PutAasxOnServer(IHttpContext context)
            {
                helper.EvalPutAasxOnServer(context);
                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/aasx/filesystem/([^/]+)(/|)$") ]
            public IHttpContext PutAasxToFileSystem(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalPutAasxToFilesystem(context, m.Groups[ 1 ].ToString());
                }

                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.DELETE, PathInfo = "^/aas/([^/]+)(/|)$") ]
            public IHttpContext DeleteAasAndAsset(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalDeleteAasAndAsset(context, m.Groups[ 1 ].ToString(), deleteAsset: true);
                }

                return context;
            }

            // Handles

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/handles/identification(/|)$") ]
            public IHttpContext GetHandlesIdentification(IHttpContext context)
            {
                helper.EvalGetHandlesIdentification(context);
                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/handles/identification(/|)$") ]
            public IHttpContext PostHandlesIdentification(IHttpContext context)
            {
                helper.EvalPostHandlesIdentification(context);
                return context;
            }

            // Authenticate

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/authenticateGuest(/|)$") ]
            public IHttpContext GetAuthenticate(IHttpContext context)
            {
                helper.EvalGetAuthenticateGuest(context);
                return context;
            }

            // Authenticate User

            [ RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/authenticateUser(/|)$") ]
            public IHttpContext PostAuthenticateUser(IHttpContext context)
            {
                helper.EvalPostAuthenticateUser(context);
                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/authenticateCert1(/|)$") ]
            public IHttpContext PostAuthenticateCert1(IHttpContext context)
            {
                helper.EvalPostAuthenticateCert1(context);
                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/authenticateCert2(/|)$") ]
            public IHttpContext PostAuthenticateCert2(IHttpContext context)
            {
                helper.EvalPostAuthenticateCert2(context);
                return context;
            }

            // Server

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/server/profile(/|)$") ]
            public IHttpContext GetServerProfile(IHttpContext context)
            {
                helper.EvalGetServerProfile(context);
                return context;
            }

            // OZ
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/server/listaas(/|)$") ]
            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/server/listasset(/|)$") ]
            public IHttpContext GetServerAASX(IHttpContext context)
            {
                if (context.Request.PathInfo.Contains("listasset"))
                {
                    helper.EvalGetListAAS(context, true);
                }
                else
                {
                    helper.EvalGetListAAS(context);
                }

                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = @"^/assetid/(\d+)(/|)$") ]
            public IHttpContext GlobalAssetId(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalAssetId(context, Int32.Parse(m.Groups[ 1 ].ToString()));
                }

                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = @"^/server/getaasx/(\d+)(/|)$") ]
            public IHttpContext GetAASX(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalGetAASX(context, Int32.Parse(m.Groups[ 1 ].ToString()));
                    return context;
                }

                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = @"^/server/getaasx/(\d+)(/|)$") ]
            public IHttpContext PutAASX(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    // TODO (MIHO/AO, 2021-01-07): enable productive code instead of dump test code
                    // bool test = true;
                    bool test = false;
                    if (test)
                    {
                        // very dump test code
                        var req = context.Request;
                        if (req.ContentLength64 > 0
                            && req.Payload != null)
                        {
                            var ba = Convert.FromBase64String(req.Payload);
                            System.IO.File.WriteAllBytes("test.aasx", ba);
                        }
                    }
                    else
                    {
                        // here goes the official code
                        helper.EvalPutAasxReplacePackage(context, m.Groups[ 1 ].ToString());
                    }

                    return context;
                }

                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = @"^/server/getaasxbyassetid/([^/]+)(/|)$") ]
            public IHttpContext GetAASX2ByAssetId(IHttpContext context)
            {
                helper.EvalGetAasxByAssetId(context);
                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = @"^/server/getaasx2/(\d+)(/|)$") ]
            public IHttpContext GetAASX2(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalGetAASX2(context, Int32.Parse(m.Groups[ 1 ].ToString()));
                    return context;
                }

                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = @"^/server/getfile/(\d+)/(([^/]+)/){0,99}([^/]+)$") ]
            public IHttpContext GetFile(IHttpContext context)
            {
                int index = -1;
                string path = "";

                string[] split = context.Request.PathInfo.Split(new Char[] {'/'});
                if (split[ 1 ].ToLower() == "server" && split[ 2 ].ToLower() == "getfile")
                {
                    index = Int32.Parse(split[ 3 ]);
                    int i = 4;
                    while (i < split.Length)
                    {
                        path += "/" + split[ i ];
                        i++;
                    }
                }

                helper.EvalGetFile(context, index, path);

                return context;
            }

            // Assets

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/assets/([^/]+)(/|)$") ]
            public IHttpContext GetAssets(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalGetAssetLinks(context, m.Groups[ 1 ].ToString());
                }

                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/assets(/|)$") ]
            public IHttpContext PutAssets(IHttpContext context)
            {
                helper.EvalPutAsset(context);
                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/aas/(id|([^/]+))/asset(/|)$") ]
            public IHttpContext PutAssetsToAas(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalPutAssetToAas(context, m.Groups[ 1 ].ToString());
                }

                return context;
            }

            // List of Submodels

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))/submodels(/|)$") ]
            public IHttpContext GetSubmodels(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalGetSubmodels(context, m.Groups[ 1 ].ToString());
                }

                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/aas/(id|([^/]+))/submodels(/|)$") ]
            public IHttpContext PutSubmodel(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalPutSubmodel(context, m.Groups[ 1 ].ToString());
                }

                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.DELETE, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)(/|)$") ]
            public IHttpContext DeleteSubmodel(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 4)
                {
                    helper.EvalDeleteSubmodel(context, m.Groups[ 1 ].ToString(), m.Groups[ 3 ].ToString());
                }

                return context;
            }

            // Contents of a Submodel

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)(|/core|/deep|/complete|/values)(/|)$") ]
            public IHttpContext GetSubmodelContents(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 4)
                {
                    var aasid = m.Groups[ 1 ].ToString();
                    var smid = m.Groups[ 3 ].ToString();

                    if (helper.PathEndsWith(context, "values"))
                    {
                        helper.EvalGetSubmodelAllElementsProperty(context, aasid, smid, elemids: null);
                    }
                    else
                    {
                        var deep = helper.PathEndsWith(context, "deep");
                        var complete = helper.PathEndsWith(context, "complete");
                        helper.EvalGetSubmodelContents(context, aasid, smid, deep: deep || complete, complete: complete);
                    }
                }

                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)/table(/|)$") ]
            public IHttpContext GetSubmodelContentsAsTable(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 4)
                {
                    helper.EvalGetSubmodelContentsAsTable(context, m.Groups[ 1 ].ToString(), m.Groups[ 3 ].ToString());
                }

                return context;
            }

            // Contents of SubmodelElements

            [ RestRoute(HttpMethod = HttpMethod.GET,
                PathInfo =
                    "^/aas/(id|([^/]+))/submodels/([^/]+)/submodel/submodelElements(/([^/]+)){1,99}?(|/core|/complete|/deep|/file|/blob|/events|/values/value)(/|)$") ] // BaSyx-Style
            [ RestRoute(HttpMethod = HttpMethod.GET,
                PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)/elements(/([^/]+)){1,99}?(|/core|/complete|/deep|/file|/blob|/events|/values|/value)(/|)$") ]
            public IHttpContext GetSubmodelElementsContents(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo.Replace("submodel/submodelElements", "elements"));
                if (m.Success && m.Groups.Count >= 6 && m.Groups[ 5 ].Captures != null && m.Groups[ 5 ].Captures.Count >= 1)
                {
                    var aasid = m.Groups[ 1 ].ToString();
                    var smid = m.Groups[ 3 ].ToString();
                    var elemids = new List<string>();
                    for (int i = 0; i < m.Groups[ 5 ].Captures.Count; i++)
                        elemids.Add(m.Groups[ 5 ].Captures[ i ].ToString());

                    // special case??
                    if (helper.PathEndsWith(context, "file"))
                    {
                        helper.EvalGetSubmodelElementsFile(context, aasid, smid, elemids.ToArray());
                    }
                    else if (helper.PathEndsWith(context, "blob"))
                    {
                        helper.EvalGetSubmodelElementsBlob(context, aasid, smid, elemids.ToArray());
                    }
                    else if (helper.PathEndsWith(context, "values") || helper.PathEndsWith(context, "value"))
                    {
                        helper.EvalGetSubmodelAllElementsProperty(context, aasid, smid, elemids.ToArray());
                    }
                    else if (helper.PathEndsWith(context, "events"))
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

            [ RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)/elements(/([^/]+)){1,99}?/invoke(/|)$") ]
            public IHttpContext PostSubmodelElementsContents(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 6 && m.Groups[ 5 ].Captures != null && m.Groups[ 5 ].Captures.Count >= 1)
                {
                    var aasid = m.Groups[ 1 ].ToString();
                    var smid = m.Groups[ 3 ].ToString();
                    var elemids = new List<string>();
                    for (int i = 0; i < m.Groups[ 5 ].Captures.Count; i++)
                        elemids.Add(m.Groups[ 5 ].Captures[ i ].ToString());

                    // special case??
                    if (helper.PathEndsWith(context, "invoke"))
                    {
                        helper.EvalInvokeSubmodelElementOperation(context, aasid, smid, elemids.ToArray());
                    }
                }

                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)/elements(/([^/]+)){0,99}?(/|)$") ]
            public IHttpContext PutSubmodelElementsContents(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 6)
                {
                    var aasid = m.Groups[ 1 ].ToString();
                    var smid = m.Groups[ 3 ].ToString();
                    var elemids = new List<string>();
                    if (m.Groups[ 5 ].Captures != null)
                        for (int i = 0; i < m.Groups[ 5 ].Captures.Count; i++)
                            elemids.Add(m.Groups[ 5 ].Captures[ i ].ToString());

                    helper.EvalPutSubmodelElementContents(context, aasid, smid, elemids.ToArray());
                }

                return context;
            }

            //An OPTIONS preflight call is made by browser before calling actual PUT
            [ RestRoute(HttpMethod = HttpMethod.OPTIONS, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)/elements(/([^/]+)){0,99}?(/|)$") ]
            public IHttpContext OptionsSubmodelElementsContents(IHttpContext context)
            {
                SendJsonResponse(context, new Object()); //returning just an empty object
                return context;
            }


            [ RestRoute(HttpMethod = HttpMethod.DELETE, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)/elements(/([^/]+)){0,99}?(/|)$") ]
            public IHttpContext DeleteSubmodelElementsContents(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 6)
                {
                    var aasid = m.Groups[ 1 ].ToString();
                    var smid = m.Groups[ 3 ].ToString();
                    var elemids = new List<string>();
                    if (m.Groups[ 5 ].Captures != null)
                        for (int i = 0; i < m.Groups[ 5 ].Captures.Count; i++)
                            elemids.Add(m.Groups[ 5 ].Captures[ i ].ToString());

                    helper.EvalDeleteSubmodelElementContents(context, aasid, smid, elemids.ToArray());
                }

                return context;
            }

            // concept descriptions

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))/cds(/|)$") ]
            public IHttpContext GetCds(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalGetAllCds(context, m.Groups[ 1 ].ToString());
                }

                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = "^/aas/(id|([^/]+))/cds(/|)$") ]
            public IHttpContext PutConceptDescription(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 2)
                {
                    helper.EvalPutCd(context, m.Groups[ 1 ].ToString());
                }

                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))/cds/([^/]+)(/|)$") ]
            public IHttpContext GetSpecificCd(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 4)
                {
                    helper.EvalGetCdContents(context, m.Groups[ 1 ].ToString(), m.Groups[ 3 ].ToString());
                }

                return context;
            }

            [ RestRoute(HttpMethod = HttpMethod.DELETE, PathInfo = "^/aas/(id|([^/]+))/cds/([^/]+)(/|)$") ]
            public IHttpContext DeleteSpecificCd(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 4)
                {
                    helper.EvalDeleteSpecificCd(context, m.Groups[ 1 ].ToString(), m.Groups[ 3 ].ToString());
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
                catch
                {
                }
        }
    }
}