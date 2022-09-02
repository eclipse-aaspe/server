#define MICHA

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasCore.Aas3_0_RC02;
using AasxMqttClient;
using AdminShellEvents;
using AdminShellNS;
using Extenstions;
using Grapevine.Interfaces.Server;
using Grapevine.Server;
using Grapevine.Server.Attributes;
using Grapevine.Shared;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using static QRCoder.PayloadGenerator;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/


/* Please notice: the API and REST routes implemented in this version of the source code are not specified and standardised by the
specification Details of the Administration Shell. The hereby stated approach is solely the opinion of its author(s). */

namespace AasxRestServerLibrary
{
    public class AasxRestServer
    {
        [RestResource]
        public class TestResource
        {
            // search
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/query/([^/]+)(/|)$")]

            public IHttpContext Query(IHttpContext context)
            {
                allowCORS(context);

                string result = "";
                string restPath = context.Request.PathInfo;
                string query = restPath.Replace("/query/", "");

                if (query == "" || query == "help")
                {
                    result = "Please add BASE64 coded query, e.g. use https://www.base64encode.org/\n\n";
                    result += "SELECT:\n";
                    result += "repository | aas | submodel | submodelelement (what will be returned)\n";
                    result += "FROM:\n";
                    result += "repository | aas \"aas-id\" | submodel \"submodel-id\" (what will be searched)\n";
                    result += "WHERE:\n";
                    result += "aas | submodel | submodelelement (element to search for)\n";
                    result += "OR | AND\n";
                    result += "%id | %idshort | %semanticid <space> == | contains <space> \"value\"\n";
                    result += "(last line may be repeated)\n\n";

                    result += "EXAMPLE:\n\n";
                    result += "SELECT:\n";
                    result += "submodel\n";
                    result += "FROM:\n";
                    result += "submodel \"www.company.com/ids/sm/4343_5072_7091_3242\"\n";
                    result += "WHERE:\n";
                    result += "submodelelement\n";
                    result += "OR\n";
                    result += "%idshort contains \"ManufacturerName\"\n";
                    result += "%idshort contains \"Weight\"\n";

                    result += "\nhttp://localhost:51310/query/U0VMRUNUOgpzdWJtb2RlbApGUk9NOgpzdWJtb2RlbCAid3d3LmNvbXBhbnkuY29tL2lkcy9zbS80MzQzXzUwNzJfNzA5MV8zMjQyIgpXSEVSRToKc3VibW9kZWxlbGVtZW50Ck9SCiVpZHNob3J0IGNvbnRhaW5zICJNYW51ZmFjdHVyZXJOYW1lIgolaWRzaG9ydCBjb250YWlucyAiV2VpZ2h0Igo=\n";

                    result += "\nEXAMPLE result:\n\n";
                    result += "submodel 1 http://localhost:51310/aas/Festo_3S7PM0CP4BD/submodels/Nameplate\n";
                    result += "submodel 1 http://localhost:51310/aas/NutRunner_0608842005_755003377/submodels/Nameplate\n";
                    result += "submodel 1 http://localhost:51310/aas/R901278815_25/submodels/Nameplate\n";
                    result += "submodel 1 http://localhost:51310/aas/R901278815_25xx/submodels/Nameplate\n";
                    result += "totalfound 4 http://localhost:51310\n";
                    result += "(Selected scope <space> how often found in this <space> endpoint to get the data)\n";

                    context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.Ok, result);
                    return context;
                }

                var bytes = Convert.FromBase64String(query);
                query = System.Text.Encoding.UTF8.GetString(bytes);
                var split = query.Split('\n');

                string select = "";
                string from = "";
                string fromId = "";
                string whereElement = "";
                string whereCondition = "";
                List<string> whereOperations = new List<string>();
                int countLines = 0;
                string last = "";
                foreach (var sp in split)
                {
                    if (sp == "")
                        continue;

                    string s = sp.ToLower();
                    switch (last)
                    {
                        case "select:":
                            select = s;
                            break;
                        case "from:":
                            var fromsplit = sp.Split(' ');
                            from = fromsplit[0].ToLower();
                            if (from == "aas" || from == "submodel")
                            {
                                if (fromsplit.Length == 2)
                                    fromId = fromsplit[1].Replace("\"", "");
                            }
                            break;
                        case "where:":
                            switch (countLines)
                            {
                                case 0:
                                    whereElement = sp.Replace("\"", "").ToLower();
                                    break;
                                case 1:
                                    whereCondition = sp.Replace("\"", "").ToLower();
                                    break;
                                default:
                                    whereOperations.Add(sp.Replace("\"", ""));
                                    break;
                            }
                            countLines++;
                            break;
                    }
                    if (last != "where:")
                        last = s;
                }
                result += "select = \"" + select + "\"\n";
                result += "from = \"" + from + "\"\n";
                if (fromId != "")
                    result += "fromId = \"" + fromId + "\"\n";
                result += "whereElement = \"" + whereElement + "\"\n";
                result += "whereCondition = \"" + whereCondition + "\"\n";
                foreach (var wo in whereOperations)
                    result += "whereOperation = \"" + wo + "\"\n";
                result += "\n";

                int totalFound = 0;
                int foundInRepository = 0;
                int aascount = AasxServer.Program.env.Length;
                for (int i = 0; i < aascount; i++)
                {
                    int foundInAas = 0;
                    var env = AasxServer.Program.env[i];
                    if (env?.AasEnv?.AssetAdministrationShells == null)
                        continue;

                    foreach (var aas in env.AasEnv.AssetAdministrationShells)
                    {
                        if (from == "aas")
                        {
                            if (aas.Id != fromId)
                                continue;
                        }

                        if (whereElement == "aas")
                        {
                            int conditionsTrue = 0;
                            foreach (var wo in whereOperations)
                            {
                                string attr = "";
                                string attrValue = "";
                                string op = "";
                                split = wo.Split(' ');
                                if (split.Length == 3)
                                {
                                    attr = split[0];
                                    op = split[1];
                                    attrValue = split[2];
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
                                }
                                if ((op == "==" && compare == attrValue) ||
                                    (op == "contains" && compare.Contains(attrValue)))
                                {
                                    conditionsTrue++;
                                }
                            }
                            if ((whereCondition == "and" && conditionsTrue == whereOperations.Count)
                                    || (whereCondition == "or" && conditionsTrue != 0))
                            {
                                if (select == "aas")
                                {
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

                            int foundInSubmodel = 0;
                            if (whereElement == "submodel")
                            {
                                int conditionsTrue = 0;
                                foreach (var wo in whereOperations)
                                {
                                    string attr = "";
                                    string attrValue = "";
                                    string op = "";
                                    split = wo.Split(' ');
                                    if (split.Length == 3)
                                    {
                                        attr = split[0];
                                        op = split[1];
                                        attrValue = split[2];

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
                                                    compare = sm.SemanticId.Keys[0].Value;
                                                break;
                                        }
                                        if ((op == "==" && compare == attrValue) ||
                                            (op == "contains" && compare.Contains(attrValue)))
                                        {
                                            conditionsTrue++;
                                        }
                                    }
                                }

                                if ((whereCondition == "and" && conditionsTrue == whereOperations.Count)
                                        || (whereCondition == "or" && conditionsTrue != 0))
                                {
                                    if (select == "submodel")
                                    {
                                        foundInSubmodel++;
                                        totalFound++;
                                    }
                                    if (select == "aas")
                                    {
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

                            if (whereElement == "submodelelement")
                            {
                                List<List<ISubmodelElement>> stack = new List<List<ISubmodelElement>>();
                                List<int> iStack = new List<int>();
                                List<string> pathStack = new List<string>();
                                int depth = 0;
                                List<ISubmodelElement> level = sm.SubmodelElements;
                                int iLevel = 0;
                                string path = "";
                                while (depth >= 0)
                                {
                                    while (iLevel < level.Count)
                                    {
                                        var sme = level[iLevel];

                                        int conditionsTrue = 0;
                                        foreach (var wo in whereOperations)
                                        {
                                            string attr = "";
                                            string attrValue = "";
                                            string op = "";
                                            split = wo.Split(' ');
                                            if (split.Length == 3)
                                            {
                                                attr = split[0];
                                                op = split[1];
                                                attrValue = split[2];
                                            }

                                            string compare = "";
                                            switch (attr)
                                            {
                                                case "%idshort":
                                                    compare = sme.IdShort;
                                                    break;
                                                case "%semanticid":
                                                    if (sme.SemanticId != null && sme.SemanticId.Keys != null && sme.SemanticId.Keys.Count != 0)
                                                        compare = sme.SemanticId.Keys[0].Value;
                                                    break;
                                            }
                                            if ((op == "==" && compare == attrValue) ||
                                                (op == "contains" && compare.Contains(attrValue)))
                                            {
                                                conditionsTrue++;
                                            }
                                        }

                                        if ((whereCondition == "and" && conditionsTrue == whereOperations.Count)
                                                || (whereCondition == "or" && conditionsTrue != 0))
                                        {
                                            if (select == "submodelelement")
                                            {
                                                result += select + " 1 " + AasxServer.Program.externalRest +
                                                   "/aas/" + aas.IdShort + "/submodels/" + sm.IdShort +
                                                    "/elements/" + path + sme.IdShort + "\n";
                                                totalFound++;
                                            }
                                            if (select == "submodel")
                                            {
                                                foundInSubmodel++;
                                                totalFound++;
                                            }
                                            if (select == "aas")
                                            {
                                                foundInAas++;
                                                totalFound++;
                                            }
                                            if (select == "repository")
                                            {
                                                foundInRepository++;
                                                totalFound++;
                                            }
                                        }

                                        if (sme is SubmodelElementCollection smc)
                                        {
                                            stack.Add(level);
                                            iStack.Add(iLevel+1);
                                            pathStack.Add(path);
                                            depth++;
                                            path += smc.IdShort + "/";
                                            level = smc.Value;
                                            iLevel = 0;
                                            continue;
                                        }
                                        iLevel++;
                                    }
                                    depth--;
                                    if (depth >= 0)
                                    {
                                        level = stack[depth];
                                        stack.RemoveAt(depth);
                                        iLevel = iStack[depth];
                                        iStack.RemoveAt(depth);
                                        path = pathStack[depth];
                                        pathStack.RemoveAt(depth);
                                    }
                                }
                            }

                            if (select == "submodel" && foundInSubmodel != 0)
                                result += select + " " + foundInSubmodel + " " +  AasxServer.Program.externalRest
                                    + "/aas/" + aas.IdShort + "/submodels/" + sm.IdShort + "\n";

                        } // submodels

                        if (select == "aas" && foundInAas != 0)
                            result += select + " " + foundInAas + " " + AasxServer.Program.externalRest + "/aas/" + aas.IdShort + "\n";
                    } // AAS
                } // AAS-ENV

                if (select == "repository" && foundInRepository != 0)
                    result += select + " " + foundInRepository + " " + AasxServer.Program.externalRest + "\n";
                else
                    result += "totalfound " + totalFound +  " " + AasxServer.Program.externalRest + "\n";

                context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.Ok, result);

                // context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.NotFound, $"Operation not allowed!");
                return context;
            }

            // exit application
            [RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/exit/([^/]+)(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/exit/([^/]+)(/|)$")]

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
                    };
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
            [RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/secret/([^/]+)/([^/]+)(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/secret/([^/]+)/([^/]+)(/|)$")]

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
                    requestSecret1 = split[0];
                    requestSecret2 = split[1];
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
                    };
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
                var json = JsonConvert.SerializeObject(o, Formatting.Indented);
                var buffer = context.Request.ContentEncoding.GetBytes(json);
                var length = buffer.Length;

                context.Response.ContentType = ContentType.JSON;
                context.Response.ContentEncoding = Encoding.UTF8;
                context.Response.ContentLength64 = length;
                context.Response.SendResponse(buffer);
            }

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/data4(/|)$")]

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
                td.varFloat3 = (float)varFloat3;

                sendJson(context, td);
                return context;
            }

            public class DeletedListItem
            {
                public Submodel sm;
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

                public static void add(IReferable o, string op, Submodel rootSubmodel, ulong changeCount)
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
                                json = JsonConvert.SerializeObject(smec, Newtonsoft.Json.Formatting.Indented,
                                    new JsonSerializerSettings
                                    {
                                        NullValueHandling = NullValueHandling.Ignore
                                    });
                                break;
                            case "Remove":
                                reason = AasPayloadStructuralChangeItem.ChangeReason.Delete;
                                break;
                        }

                        rootSubmodel.SetAllParents();
                        List<Key> keys = new();

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
                            deletedList.Add(new DeletedListItem() { sm = rootSubmodel, rf = o });
                            if (deletedList.Count > 1000 && deletedList[0].rf != null)
                            {
                                olderDeletedTimeStamp = deletedList[0].rf.TimeStamp;
                                deletedList.RemoveAt(0);
                            }
                        }
                    }
                }
            }

            public static string posttimeseriesPayload = "";

            [RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/posttimeseries(/|)$")]

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

            [RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "^/calculatecfp/aas/([^/]+)(/|)$")]

            public IHttpContext calculatecfp(IHttpContext context)
            {
                string restPath = context.Request.PathInfo;
                int aasIndex = -1;
                string result = "NONE";

                if (restPath.Contains("/aas/"))
                {
                    // specific AAS
                    string[] split = restPath.Split('/');
                    if (split[2] == "aas")
                    {
                        try
                        {
                            if (!int.TryParse(split[3], out aasIndex))
                                aasIndex = -1;
                            if (aasIndex >= 0)
                            {
                                AasxServer.AasxTask.operation_calculate_cfp(null, aasIndex, DateTime.UtcNow);
                                result = "OK";
                            }
                        }
                        catch { }
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

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages/values(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages/time/([^/]+)(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages/deltasecs/(\\d+)(/|)$")]

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages/aas/([^/]+)(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages/aas/([^/]+)/values(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages/aas/([^/]+)/time/([^/]+)(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/geteventmessages/aas/([^/]+)/deltasecs/(\\d+)(/|)$")]

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/([^/]+)/geteventmessages(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/([^/]+)/geteventmessages/values(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/([^/]+)/geteventmessages/time/([^/]+)(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/([^/]+)/geteventmessages/deltasecs/(\\d+)(/|)$")]

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
                    if (split[2] == "aas")
                    {
                        try
                        {
                            if (!int.TryParse(split[3], out aasIndex))
                                aasIndex = -1;
                            if (aasIndex >= 0)
                            {
                                restPath = "";
                                for (int i = 1; i < split.Length; i++)
                                {
                                    if (i != 2 && i != 3)
                                    {
                                        restPath += "/" + split[i];
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    if (split[1] == "aas")
                    {
                        try
                        {
                            if (!int.TryParse(split[2], out aasIndex))
                                aasIndex = -1;
                            if (aasIndex >= 0)
                            {
                                restPath = "";
                                for (int i = 1; i < split.Length; i++)
                                {
                                    if (i != 1 && i != 2)
                                    {
                                        restPath += "/" + split[i];
                                    }
                                }
                            }
                        }
                        catch { }
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
                        catch { }
                    }
                    if (restPath.StartsWith("/geteventmessages/deltasecs/"))
                    {
                        try
                        {
                            var secs = restPath.Substring("/geteventmessages/deltasecs/".Length);
                            if (int.TryParse(secs, out int i))
                                minimumDate = DateTime.UtcNow.AddSeconds(-1.0 * i);
                        }
                        catch { }
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

                    var env = AasxServer.Program.env[i];
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
                                Reference obsSemId = null;
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

                                string[] modes = { "CREATE", "UPDATE" };

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
                                            List<Key> p2 = null;
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
                List<Key> observablePath = null)
            {
                if (!(sme is SubmodelElementCollection))
                {
                    if ((mode == "CREATE" && sme.TimeStampCreate > minimumDate) ||
                        (mode != "CREATE" && sme.TimeStamp > minimumDate && sme.TimeStamp != sme.TimeStampCreate))
                    {
                        // prepare p2 to be relative path to observable
                        var p2 = sme.GetModelReference()?.Keys;
                        //if (true == p2?.StartsWith(observablePath, matchMode: AdminShell.Key.MatchMode.Relaxed))
                        if (true == p2?.StartsWith(observablePath))
                            p2.RemoveRange(0, observablePath.Count);

                        if (mode == "CREATE")
                        {
                            if (/* doCreateDelete && */ plStruct != null)
                                plStruct.Changes.Add(new AasPayloadStructuralChangeItem(
                                    count: 1,
                                    timeStamp: sme.TimeStamp,
                                    AasPayloadStructuralChangeItem.ChangeReason.Create,
                                    path: p2,
                                    // Assumption: models will be serialized correctly
                                    data: JsonConvert.SerializeObject(sme)));
                        }
                        else
                        {
                            if (/* doUpdate && */ plUpdate != null)
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
                                var sme2 = smec.Value[i];
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
                            var sme2 = smec.Value[i];
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
                    if (true == p2?.StartsWith(observablePath))
                        p2.RemoveRange(0, observablePath.Count);

                    if (mode == "CREATE")
                    {
                        if (sme.TimeStampCreate > minimumDate)
                        {
                            if (/* doCreateDelete && */ plStruct != null)
                                plStruct.Changes.Add(new AasPayloadStructuralChangeItem(
                                    count: 1,
                                    timeStamp: sme.TimeStamp,
                                    AasPayloadStructuralChangeItem.ChangeReason.Create,
                                    path: p2,
                                    // Assumption: models will be serialized correctly
                                    data: JsonConvert.SerializeObject(sme)));
                        }
                    }
                    else
                    if (sme.TimeStamp > minimumDate && sme.TimeStamp != sme.TimeStampCreate)
                    {
                        if (/* doUpdate && */ plUpdate != null)
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

            public static void SendJsonResponse(Grapevine.Interfaces.Server.IHttpContext context, object obj)
            {
                // make JSON
                var settings = AasxIntegrationBase.AasxPluginOptionSerialization.GetDefaultJsonSettings(
                    new[] { typeof(AdminShellEvents.AasEventMsgEnvelope) });
                settings.TypeNameHandling = TypeNameHandling.Auto;
                settings.Formatting = Formatting.Indented;
                var json = JsonConvert.SerializeObject(obj, settings);

                // build buffer
                var buffer = context.Request.ContentEncoding.GetBytes(json);
                var length = buffer.Length;

                var queryString = context.Request.QueryString;
                string refresh = queryString["refresh"];
                if (refresh != null && refresh != "")
                {
                    context.Response.Headers.Remove("Refresh");
                    context.Response.Headers.Add("Refresh", refresh);
                }

                allowCORS(context);

                context.Response.ContentType = ContentType.JSON;
                context.Response.ContentEncoding = Encoding.UTF8;
                context.Response.ContentLength64 = length;
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

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff/([^/]+)(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff/update(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff/update/([^/]+)(/|)$")]

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff/aas/([^/]+)/time/([^/]+)(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff/aas/([^/]+)(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff/aas/([^/]+)/update(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diff/aas/([^/]+)/update/([^/]+)(/|)$")]

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson/([^/]+)(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson/update(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson/update/([^/]+)(/|)$")]

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson/aas/([^/]+)/time/([^/]+)(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson/aas/([^/]+)(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson/aas/([^/]+)/update(/|)$")]
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/diffjson/aas/([^/]+)/update/([^/]+)(/|)$")]

            public IHttpContext GetDiff(IHttpContext context)
            {
                string[] modes = { "DELETE", "CREATE", "UPDATE" };
                DateTime minimumDate = new DateTime();
                bool deep = false;
                int seconds = 0;
                string searchPath = "";
                int searchPathLen = 0;
                bool diffJson = false;

                var queryString = context.Request.QueryString;
                string refresh = queryString["refresh"];
                if (refresh != null && refresh != "")
                {
                    context.Response.Headers.Remove("Refresh");
                    context.Response.Headers.Add("Refresh", refresh);
                }

                string m = queryString["mode"];
                if (m != null && m != "")
                {
                    try
                    {
                        modes = m.Split(',');
                    }
                    catch { }
                }

                string time = queryString["time"];
                if (time != null && time != "")
                {
                    try
                    {
                        minimumDate = DateTime.Parse(time).ToUniversalTime();
                    }
                    catch { }
                }

                string auto = queryString["auto"];
                if (auto != null && auto != "")
                {
                    try
                    {
                        seconds = Convert.ToInt32(auto);
                        minimumDate = DateTime.UtcNow - new TimeSpan(0, 0, seconds);
                    }
                    catch { }
                }

                string dd = queryString["deep"];
                if (dd != null && dd != "")
                {
                    deep = true;
                }

                {
                    string path = queryString["path"];
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
                    if (split[2] == "aas")
                    {
                        try
                        {
                            if (!int.TryParse(split[3], out aasIndex))
                                aasIndex = -1;
                            if (aasIndex >= 0)
                            {
                                restPath = "";
                                for (int i = 1; i < split.Length; i++)
                                {
                                    if (i != 2 && i != 3)
                                    {
                                        restPath += "/" + split[i];
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                string diffText = "";
                List<diffEntry> diffList = new List<diffEntry>();

                addEntry(diffJson, ref diffText, ref diffList, "OPEN");

                int aascount = AasxServer.Program.env.Length;

                for (int imode = 0; imode < modes.Length; imode++)
                {
                    string mode = modes[imode];

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
                                    x = (IReferable)x.Parent;
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

                        var env = AasxServer.Program.env[i];
                        if (env != null)
                        {
                            var aas = env.AasEnv.AssetAdministrationShells[0];
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
                                                        mode, p, "SM", (DateTime)sm.TimeStamp);
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

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/authserver(/|)$")]

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
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))(|/core|/complete|/thumbnail|/aasenv|/aasenvjson)(/|)$")]

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
                        helper.EvalGetAasThumbnail(context, m.Groups[1].ToString());
                    }
                    else
                    if (helper.PathEndsWith(context, "aasenv") || helper.PathEndsWith(context, "aasenvjson"))
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
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/server/listasset(/|)$")]
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

            [RestRoute(HttpMethod = HttpMethod.PUT, PathInfo = @"^/server/getaasx/(\d+)(/|)$")]
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
                        helper.EvalPutAasxReplacePackage(context, m.Groups[1].ToString());
                    }
                    return context;
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = @"^/server/getaasxbyassetid/([^/]+)(/|)$")]
            public IHttpContext GetAASX2ByAssetId(IHttpContext context)
            {
                helper.EvalGetAasxByAssetId(context);
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

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = @"^/server/getfile/(\d+)/(([^/]+)/){0,99}([^/]+)$")]
            public IHttpContext GetFile(IHttpContext context)
            {
                int index = -1;
                string path = "";

                string[] split = context.Request.PathInfo.Split(new Char[] { '/' });
                if (split[1].ToLower() == "server" && split[2].ToLower() == "getfile")
                {
                    index = Int32.Parse(split[3]);
                    int i = 4;
                    while (i < split.Length)
                    {
                        path += "/" + split[i];
                        i++;
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

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)(|/core|/deep|/complete|/values)(/|)$")]
            public IHttpContext GetSubmodelContents(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo);
                if (m.Success && m.Groups.Count >= 4)
                {
                    var aasid = m.Groups[1].ToString();
                    var smid = m.Groups[3].ToString();

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

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)/submodel/submodelElements(/([^/]+)){1,99}?(|/core|/complete|/deep|/file|/blob|/events|/values/value)(/|)$")] // BaSyx-Style
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)/elements(/([^/]+)){1,99}?(|/core|/complete|/deep|/file|/blob|/events|/values|/value)(/|)$")]
            public IHttpContext GetSubmodelElementsContents(IHttpContext context)
            {
                var m = helper.PathInfoRegexMatch(MethodBase.GetCurrentMethod(), context.Request.PathInfo.Replace("submodel/submodelElements", "elements"));
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
                    if (helper.PathEndsWith(context, "values") || helper.PathEndsWith(context, "value"))
                    {
                        helper.EvalGetSubmodelAllElementsProperty(context, aasid, smid, elemids.ToArray());
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

            //An OPTIONS preflight call is made by browser before calling actual PUT
            [RestRoute(HttpMethod = HttpMethod.OPTIONS, PathInfo = "^/aas/(id|([^/]+))/submodels/([^/]+)/elements(/([^/]+)){0,99}?(/|)$")]
            public IHttpContext OptionsSubmodelElementsContents(IHttpContext context)
            {
                SendJsonResponse(context, new Object()); //returning just an empty object
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
