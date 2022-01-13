using AasxServer;
using AdminShellNS;
using Jose;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
Please notice: the API and REST routes implemented in this version of the source code are not specified and standardised by the
specification Details of the Administration Shell. The hereby stated approach is solely the opinion of its author(s). */

namespace AasxRestServerLibrary
{
    public class AasxHttpContextHelper
    {
        public static String SwitchToAASX = "";
        public static String DataPath = ".";

        public AdminShellPackageEnv[] Packages = null;

        public FindAasReturn FindAAS(string aasid, string queryString = null, string rawUrl = null)
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

                if (Packages[i] == null || Packages[i].AasEnv == null || Packages[i].AasEnv.AdministrationShells == null
                    || Packages[i].AasEnv.AdministrationShells.Count < 1)
                    return null;

                findAasReturn.aas = Packages[i].AasEnv.AdministrationShells[0];
                findAasReturn.iPackage = i;
            }
            else
            {
                // Name
                if (aasid == "id")
                {
                    findAasReturn.aas = Packages[0].AasEnv.AdministrationShells[0];
                    findAasReturn.iPackage = 0;
                }
                else
                {
                    for (int i = 0; i < Packages.Length; i++)
                    {
                        if (Packages[i] != null)
                        {
                            if (Packages[i].AasEnv.AdministrationShells[0].idShort == aasid)
                            {
                                findAasReturn.aas = Packages[i].AasEnv.AdministrationShells[0];
                                findAasReturn.iPackage = i;
                                break;
                            }
                        }
                    }
                }
            }

            return findAasReturn;
        }

        public class FindSubmodelElementResult
        {
            public AdminShell.Referable elem = null;
            public AdminShell.SubmodelElementWrapper wrapper = null;
            public AdminShell.Referable parent = null;

            public FindSubmodelElementResult(AdminShell.Referable elem = null, AdminShell.SubmodelElementWrapper wrapper = null, AdminShell.Referable parent = null)
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

        public ExpandoObject EvalGetAasAndAsset(HttpContext context, string aasid)
        {
            dynamic res = new ExpandoObject();

            // access the first AAS
            var findAasReturn = FindAAS(aasid, context.Request.QueryString.Value, context.Request.Path.Value);
            if (findAasReturn.aas == null)
            {
                context.Response.StatusCode = (int) HttpStatusCode.NotFound;//, $"No AAS with id '{aasid}' found.");
                return null;
            }

            // try to get the asset as well
            var asset = this.Packages[findAasReturn.iPackage].AasEnv.FindAsset(findAasReturn.aas.assetRef);

            // result
            res.AAS = findAasReturn.aas;
            res.Asset = asset;

            return res;
        }

        public ObjectResult EvalDeleteAasAndAsset(string aasid, bool deleteAsset = false)
        {
            ExpandoObject res = new ExpandoObject();

            // datastructure update
            if (this.Packages[0] == null || this.Packages[0].AasEnv == null || this.Packages[0].AasEnv.AdministrationShells == null)
            {
                return new ObjectResult($"Error accessing internal data structures.") { StatusCode = (int)HttpStatusCode.InternalServerError };
            }

            // access the AAS
            var findAasReturn = this.FindAAS(aasid);
            if (findAasReturn.aas == null)
            {
                return new ObjectResult($"No AAS with idShort '{aasid}' found.") { StatusCode = (int)HttpStatusCode.NotFound };
            }

            // find the asset
            var asset = this.Packages[findAasReturn.iPackage].AasEnv.FindAsset(findAasReturn.aas.assetRef);

            // delete
            Console.WriteLine($"Deleting AdministrationShell with idShort {findAasReturn.aas.idShort ?? "--"} and id {findAasReturn.aas.identification?.ToString() ?? "--"}");
            this.Packages[findAasReturn.iPackage].AasEnv.AdministrationShells.Remove(findAasReturn.aas);

            if (this.Packages[findAasReturn.iPackage].AasEnv.AdministrationShells.Count == 0)
            {
                this.Packages[findAasReturn.iPackage] = null;
            }
            else
            {
                if (deleteAsset && asset != null)
                {
                    Console.WriteLine($"Deleting Asset with idShort {asset.idShort ?? "--"} and id {asset.identification?.ToString() ?? "--"}");
                    this.Packages[findAasReturn.iPackage].AasEnv.Assets.Remove(asset);
                }
            }

            return new ObjectResult(string.Empty) { StatusCode = (int)HttpStatusCode.OK };
        }

        public static bool withAuthentification = false;
        public static string GuestToken = null;
        public static string secretString = "Industrie4.0-Asset-Administration-Shell";
        private static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

        public int sessionCount = 0;
        public char[] sessionUserType = new char[100];
        public static string[] sessionUserName = new string[100];
        public static RSA[] sessionUserPulicKey = new RSA[100];
        public static string[] sessionRandom = new string[100];
        public static string[] sessionChallenge = new string[100];

        public bool checkAccessLevel(string currentRole, string operation, string neededRights,
            string objPath = "", string aasOrSubmodel = null, object objectAasOrSubmodel = null)
        {
            if (currentRole == null)
                currentRole = "isNotAuthenticated";

            int iRole = 0;
            while (securityRole != null && iRole < securityRole.Count && securityRole[iRole].name != null)
            {
                if (aasOrSubmodel == "aas" && securityRole[iRole].objType == "aas" &&
                    objectAasOrSubmodel != null && securityRole[iRole].objReference == objectAasOrSubmodel &&
                    securityRole[iRole].permission == neededRights)
                {
                    if ((securityRole[iRole].condition == "" && securityRole[iRole].name == currentRole) ||
                        (securityRole[iRole].condition == "not" && securityRole[iRole].name != currentRole))
                    {
                        if (securityRole[iRole].kind == "allow")
                            return true;
                        if (securityRole[iRole].kind == "deny")
                            return false;
                    }
                }
                if (securityRole[iRole].name == currentRole && securityRole[iRole].objType == "api")
                {
                    if (securityRole[iRole].apiOperation == "*" || securityRole[iRole].apiOperation == operation)
                    {
                        if (securityRole[iRole].permission == neededRights)
                        {
                            return true;
                        }
                    }
                }
                iRole++;
            }
            if (operation == "/submodelelements" && objPath != "")
            {
                // next object with rule must have allow
                // no objects below must have deny
                string deepestDeny = "";
                string deepestAllow = "";
                foreach (var role in securityRole)
                {
                    if (role.objType == "submodelElement")
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
                                    return false;
                            }
                        }
                        if (role.kind == "allow")
                        {
                            if (objPath.Length >= role.objPath.Length) // allow in tree above
                            {
                                if (role.objPath == objPath.Substring(0, role.objPath.Length))
                                    deepestAllow = role.objPath;
                            }
                        }
                    }
                }
                if (deepestDeny.Length > deepestAllow.Length)
                    return false;
                return true;
            }

            return false;
        }

        public bool checkAccessRights(HttpContext context, string currentRole, string operation, string neededRights,
            string objPath = "", string aasOrSubmodel = null, object objectAasOrSubmodel = null)
        {
            if (checkAccessLevel(currentRole, operation, neededRights, objPath, aasOrSubmodel, objectAasOrSubmodel))
                return true;

            if (currentRole == null)
            {
                if (Program.redirectServer != "")
                {
                    string queryString = string.Empty;
                    string originalRequest = context.Request.Path.ToString();
                    Console.WriteLine("\nRedirect OriginalRequset: " + originalRequest);
                    string redirectUrl = AasxServer.Program.redirectServer + "?" + "authType=" + AasxServer.Program.authType + "&" + queryString;
                    Console.WriteLine("Redirect Response: " + redirectUrl + "\n");
                    context.Response.StatusCode = (int)HttpStatusCode.TemporaryRedirect;
                    context.Response.Redirect(redirectUrl);
                    return false;
                }
            }

            return false;
        }

        public string SecurityCheck(HttpContext context, ref int index)
        {
            bool error = false;
            string accessrights = null;

            // receive token with sessionID inside
            // check if token is signed by sessionRandom
            // read username for sessionID
            // check accessrights for username

            dynamic res = new ExpandoObject();
            int id = -1;
            string token = null;
            string random = null;
            string bearerToken = null;
            string user = null;

            index = -1; // not found

            string[] split = null;

            string headers = context.Request.Headers.ToString();

            // Check bearer token
            token = context.Request.Headers["Authorization"];
            if (token != null)
            {
                split = token.Split(new Char[] { ' ', '\t' });
                if (split[0] != null)
                {
                    if (split[0].ToLower() == "bearer")
                    {
                        Console.WriteLine("Received bearer token = " + split[1]);
                        bearerToken = split[1];
                    }
                }
            }
            else // check query string for bearer token
            {
                split = context.Request.Path.ToString().Split(new char[] { '?' });
                if (split != null && split.Length > 1 && split[1] != null)
                {
                    Console.WriteLine("Received query string = " + split[1]);
                    bearerToken = split[1];
                }
            }

            if (bearerToken == null)
            {
                error = true;

                // Check email token
                token = context.Request.Headers["Email"];
                if (token != null)
                {
                    Console.WriteLine("Received Email token = " + token);
                    user = token;
                    error = false;
                }
            }

            // Check email token
            token = context.Request.Headers["Email"];
            if (token != null)
            {
                Console.WriteLine("Received Email token = " + token);
                user = token;
                error = false;
            }

            if (!error)
            {
                JObject parsed2 = null;

                try
                {
                    if (bearerToken != null)
                    {
                        string serverName = "";
                        string email = "";

                        parsed2 = JObject.Parse(JWT.Payload(bearerToken));

                        try
                        {
                            email = parsed2.SelectToken("email").Value<string>();
                        }
                        catch { }
                        try
                        {
                            serverName = parsed2.SelectToken("serverName").Value<string>();
                        }
                        catch
                        {
                            serverName = "keycloak";
                        }
                        if (email != "")
                        {
                            user = email.ToLower();
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
                                JWT.Decode(bearerToken, cert.GetRSAPublicKey(), JwsAlgorithm.RS256); // correctly signed by auth server cert?
                            }
                            catch
                            {
                                return null;
                            }

                            try
                            {
                                user = parsed2.SelectToken("userName").Value<string>();
                                user = user.ToLower();
                            }
                            catch { }
                        }
                    }

                    if (user != null && user != "")
                    {
                        if (securityRights != null)
                        {
                            int rightsCount = securityRights.Count;

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
                            for (int i = 0; i < rightsCount; i++)
                            {
                                if (!securityRights[i].name.Contains("@")) // domain name only
                                {
                                    string[] splitUser = user.Split('@');
                                    if (splitUser[1] == securityRights[i].name)
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
                }
                catch
                {
                    error = true;
                }
            }

            if (!error)
            {
                try
                {
                    string Body = null;

                    switch (sessionUserType[id])
                    {
                        case 'G':
                        case 'U':
                        case 'T':
                            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                            Body = JWT.Decode(bearerToken, enc.GetBytes(random), JwsAlgorithm.HS256); // correctly signed by session token?
                            break;
                    }
                }
                catch
                {
                    error = true;
                }
            }

            if (!error)
            {
                index = id;
            }
            return accessrights;
        }

        public ExpandoObject EvalGetListAAS(HttpContext context)
        {
            dynamic res = new ExpandoObject();
            int index = -1;
            string accessrights = SecurityCheck(context, ref index);

            // get the list
            var aaslist = new List<string>();

            int aascount = Program.env.Count;

            for (int i = 0; i < aascount; i++)
            {
                if (Program.env[i] != null)
                {
                    var aas = Program.env[i].AasEnv.AdministrationShells[0];
                    string idshort = aas.idShort;
                    string aasRights = "NONE";
                    if (securityRightsAAS != null && securityRightsAAS.Count != 0)
                    {
                        securityRightsAAS.TryGetValue(idshort, out aasRights);
                    }

                    bool addEntry = false;
                    if (!withAuthentification || checkAccessLevel(accessrights, "/server/listaas", "READ", "", "aas", aas))
                    {
                        addEntry = true;
                    }

                    if (addEntry)
                    {
                        aaslist.Add(i.ToString() + " : "
                            + idshort + " : "
                            + aas.identification + " : "
                            + Program.envFileName[i]);
                    }
                }
            }

            res.aaslist = aaslist;
            return res;
        }

        public Stream EvalGetAASX(HttpContext context, int fileIndex)
        {
            dynamic res = new ExpandoObject();

            // check authentication
            int index = -1;
            string accessrights = null;
            if (withAuthentification)
            {
                accessrights = SecurityCheck(context, ref index);

                var aas = Program.env[fileIndex].AasEnv.AdministrationShells[0];
                if (!checkAccessRights(context, accessrights, "/aasx", "READ", "", "aas", aas))
                {
                    return null;
                }
            }

            // save actual data as file
            lock (Program.changeAasxFile)
            {
                string fname = "./temp/" + Path.GetFileName(Program.envFileName[fileIndex]);
                Program.env[fileIndex].SaveAs(fname);

                // return as FILE
                return File.OpenRead(fname);
            }
        }

        public static string[] securityUserName = null;
        public static string[] securityUserPassword = null;

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
            public string condition = null;
            public string name = null;
            public string objType = null;
            public string apiOperation = null;
            public object objReference = null;
            public string objPath = "";
            public string permission = null;
            public string kind = null;

            public securityRoleClass() { }
        }

        public static List<securityRoleClass> securityRole = null;

        public static void securityInit()
        {
            withAuthentification = !Program.noSecurity;

            securityRole = new List<securityRoleClass>();

            int aascount = AasxServer.Program.env.Count;

            for (int i = 0; i < aascount; i++)
            {
                var env = AasxServer.Program.env[i];
                if (env != null)
                {
                    var aas = env.AasEnv.AdministrationShells[0];
                    if (aas.submodelRefs != null && aas.submodelRefs.Count > 0)
                    {
                        foreach (var smr in aas.submodelRefs)
                        {
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm != null && sm.idShort != null)
                            {
                                if (!sm.idShort.ToLower().Contains("Security"))
                                {
                                    sm.SetAllParents();
                                }
                            }
                        }

                        foreach (var smr in aas.submodelRefs)
                        {
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm != null && sm.idShort != null)
                            {
                                if (sm.idShort == "SecuritySettingsForServer")
                                {
                                    int countSme = sm.submodelElements.Count;
                                    for (int iSme = 0; iSme < countSme; iSme++)
                                    {
                                        var sme = sm.submodelElements[iSme].submodelElement;
                                        var smec = sme as AdminShell.SubmodelElementCollection;
                                        int countSmec = smec.value.Count;
                                        switch (smec.idShort)
                                        {
                                            case "authenticationServer":
                                                for (int iSmec = 0; iSmec < countSmec; iSmec++)
                                                {
                                                    var sme2 = smec.value[iSmec].submodelElement;
                                                    switch (sme2.idShort)
                                                    {
                                                        case "endpoint":
                                                            var p2 = sme2 as AdminShell.Property;
                                                            AasxServer.Program.redirectServer = p2.value;
                                                            break;
                                                        case "type":
                                                            var p3 = sme2 as AdminShell.Property;
                                                            AasxServer.Program.authType = p3.value;
                                                            break;
                                                        case "publicCertificate":
                                                            var f = sme2 as AdminShell.File;
                                                            serverCertfileNames = new string[1];
                                                            serverCerts = new X509Certificate2[1];
                                                            var s = AasxServer.Program.env[i].GetLocalStreamFromPackage(f.value);
                                                            if (s != null)
                                                            {
                                                                using (var m = new System.IO.MemoryStream())
                                                                {
                                                                    s.CopyTo(m);
                                                                    var b = m.GetBuffer();
                                                                    serverCerts[0] = new X509Certificate2(b);
                                                                    string[] split = f.value.Split('/');
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
                                                    var smec2 = smec.value[iSmec].submodelElement as AdminShell.SubmodelElementCollection;
                                                    int countSmec2 = smec2.value.Count;
                                                    List<string> subjects = new List<string>();

                                                    for (int iSmec2 = 0; iSmec2 < countSmec2; iSmec2++)
                                                    {
                                                        var smec3 = smec2.value[iSmec2].submodelElement as AdminShell.SubmodelElementCollection;
                                                        int countSmec3 = smec3.value.Count;

                                                        switch (smec3.idShort)
                                                        {
                                                            case "subjects":
                                                                for (int iSmec3 = 0; iSmec3 < countSmec3; iSmec3++)
                                                                {
                                                                    var p = smec3.value[iSmec3].submodelElement as AdminShell.Property;
                                                                    switch (p.idShort)
                                                                    {
                                                                        case "emailDomain":
                                                                        case "email":
                                                                            subjects.Add(p.value);
                                                                            break;
                                                                        default:
                                                                            subjects.Add(p.idShort);
                                                                            break;

                                                                    }
                                                                }
                                                                break;
                                                            case "roles":
                                                                for (int iSmec3 = 0; iSmec3 < countSmec3; iSmec3++)
                                                                {
                                                                    var p = smec3.value[iSmec3].submodelElement as AdminShell.Property;
                                                                    foreach (var s in subjects)
                                                                    {
                                                                        securityRightsClass sr = new securityRightsClass();
                                                                        sr.name = s;
                                                                        sr.role = p.idShort;
                                                                        securityRights.Add(sr);
                                                                    }
                                                                }
                                                                break;
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                }
                                if (sm.idShort == "SecurityMetaModelForServer" || sm.idShort == "SecurityMetaModelForAAS")
                                {
                                    var smc1 = sm.submodelElements.FindFirstIdShortAs<AdminShell.SubmodelElementCollection>("accessControlPolicyPoints");
                                    var smc2 = smc1?.value.FindFirstIdShortAs<AdminShell.SubmodelElementCollection>("policyAdministrationPoint");
                                    var smc3 = smc2?.value.FindFirstIdShortAs<AdminShell.SubmodelElementCollection>("localAccessControl");
                                    var smc4 = smc3?.value.FindFirstIdShortAs<AdminShell.SubmodelElementCollection>("accessPermissionRules");
                                    if (smc4 == null) continue;

                                    int countSme = smc4.value.Count;
                                    for (int iSme = 0; iSme < countSme; iSme++)
                                    {
                                        var sme = smc4.value[iSme].submodelElement; // actual rule
                                        var smc5 = sme as AdminShell.SubmodelElementCollection;
                                        var smc6 = smc5?.value.FindFirstIdShortAs<AdminShell.SubmodelElementCollection>("targetSubjectAttributes");
                                        List<AdminShell.Property> role = new List<AdminShellV20.Property>();
                                        int iRole = 0;
                                        while (smc6?.value.Count > iRole)
                                        {
                                            if (smc6?.value[iRole].submodelElement is AdminShell.Property rp)
                                            {
                                                role.Add(rp);
                                                iRole++;
                                            }
                                        }
                                        smc6 = smc5?.value.FindFirstIdShortAs<AdminShell.SubmodelElementCollection>("permissionsPerObject");
                                        var smc7 = smc6?.value[0].submodelElement as AdminShell.SubmodelElementCollection;
                                        var objProp = smc7?.value.FindFirstIdShortAs<AdminShell.Property>("object");
                                        var objRef = smc7?.value.FindFirstIdShortAs<AdminShell.ReferenceElement>("object");
                                        object aasObject = null;
                                        if (objRef != null)
                                        {
                                            aasObject = env.AasEnv.FindReferableByReference(objRef.value);
                                        }
                                        var smc8 = smc7?.value.FindFirstIdShortAs<AdminShell.SubmodelElementCollection>("permission");

                                        int countSmc8 = smc8.value.Count;
                                        List<string> listPermission = new List<string>();
                                        AdminShell.Property kind = null;
                                        for (int iSmc8 = 0; iSmc8 < countSmc8; iSmc8++)
                                        {
                                            var sme9 = smc8.value[iSmc8].submodelElement;
                                            if (sme9 is AdminShell.Property)
                                                kind = sme9 as AdminShell.Property;
                                            if (sme9 is AdminShell.ReferenceElement)
                                            {
                                                var refer = sme9 as AdminShell.ReferenceElement;
                                                var permission = env.AasEnv.FindReferableByReference(refer.value);
                                                if (!(permission is AdminShell.Property))
                                                    continue;
                                                var p = permission as AdminShell.Property;
                                                listPermission.Add(p.idShort);
                                            }
                                        }

                                        string[] split = null;
                                        foreach (var l in listPermission)
                                        {
                                            foreach (var r in role)
                                            {
                                                securityRoleClass src = new securityRoleClass();
                                                if (r.idShort.Contains(":"))
                                                {
                                                    split = r.idShort.Split(':');
                                                    src.condition = split[0].ToLower();
                                                    src.name = split[1];
                                                }
                                                else
                                                {
                                                    src.condition = "";
                                                    src.name = r.idShort;
                                                }
                                                if (objProp != null)
                                                {
                                                    string value = objProp.value.ToLower();
                                                    src.objType = value;
                                                    if (value.Contains("api"))
                                                    {
                                                        split = value.Split(':');
                                                        if (split[0] == "api")
                                                        {
                                                            src.objType = split[0];
                                                            src.apiOperation = split[1];
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (aasObject != null)
                                                    {
                                                        src.objReference = aasObject;
                                                        if (aasObject is AdminShell.AdministrationShell)
                                                            src.objType = "aas";
                                                        if (aasObject is AdminShell.Submodel)
                                                            src.objType = "sm";
                                                        if (aasObject is AdminShell.SubmodelElement smep)
                                                        {
                                                            AdminShell.Referable rp = smep;
                                                            src.objType = "submodelElement";
                                                            string path = rp.idShort;
                                                            while (rp.parent != null)
                                                            {
                                                                rp = rp.parent;
                                                                path = rp.idShort + "." + path;
                                                            }
                                                            src.objPath = path;
                                                        }
                                                    }
                                                }
                                                src.permission = l.ToUpper();
                                                if (kind != null)
                                                    src.kind = kind.value.ToLower();
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

        public static void serverCertsInit()
        {
            if (Directory.Exists("./authservercerts"))
            {
                serverCertfileNames = Directory.GetFiles("./authservercerts", "*.cer");

                serverCerts = new X509Certificate2[serverCertfileNames.Length];

                for (int i = 0; i < serverCertfileNames.Length; i++)
                {
                    serverCerts[i] = new X509Certificate2(serverCertfileNames[i]);
                    Console.WriteLine("Loaded auth server certifcate: " + Path.GetFileName(serverCertfileNames[i]));
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
    }
}
