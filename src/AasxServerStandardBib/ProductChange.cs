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


using AdminShellNS;
using Extensions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Xml;

namespace ProductChange
{
    public static class pcn
    {
        static Thread pcnThread;
        public static DateTime lastRun = new DateTime();
        public static void pcnInit()
        {
            xmlImport();
            pcnThread = new Thread(new ThreadStart(pcnLoop));
            pcnThread.Start();
        }

        public static void pcnLoop()
        {
            while (true)
            {
                pcnRun();
            }
        }

        public static void pcnRun()
        {
            DateTime timeStamp = DateTime.UtcNow;

            // Run every hour
            if (timeStamp - lastRun >= TimeSpan.FromHours(1))
            {
                lastRun = timeStamp;
                Console.WriteLine("Checking Emails for Product Change Notifications: " + timeStamp + "Z");

                AdminShellPackageEnv pcnEnv = null;
                ISubmodel pcnSub = null;
                SubmodelElementCollection imported = null;
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
                                if (sm != null && sm.IdShort != null && sm.IdShort.ToLower().Contains("pcn"))
                                {
                                    pcnEnv = env;
                                    pcnSub = sm;

                                    foreach (var sme in sm.SubmodelElements)
                                    {
                                        if (sme is SubmodelElementCollection smc)
                                        {
                                            if (smc.IdShort == "imported")
                                            {
                                                imported = smc;
                                                break;
                                            }
                                        }
                                    }
                                    if (imported == null)
                                    {
                                        imported = new SubmodelElementCollection(idShort: "imported");
                                        sm.Add(imported);
                                    }
                                }
                            }
                        }
                    }
                }
                if (pcnEnv == null || pcnSub == null || imported == null)
                    return;

                if (!Directory.Exists("./pcn"))
                {
                    Directory.CreateDirectory("./pcn");
                }
                // using (var client = new ImapClient(new ProtocolLogger("imap.log")))
                using (var client = new ImapClient())
                {
                    string emailAddress = "";
                    string username = "";
                    string password = "";
                    string emailFile = "email.txt";
                    if (System.IO.File.Exists(emailFile))
                    {
                        try
                        {   // Open the text file using a stream reader.
                            using (StreamReader sr = new StreamReader(emailFile))
                            {
                                emailFile = sr.ReadLine();
                            }
                        }
                        catch (IOException e)
                        {
                            Console.WriteLine("email.txt could not be read:");
                            Console.WriteLine(e.Message);
                            return;
                        }
                    }

                    if (System.IO.File.Exists(emailFile))
                    {
                        Console.WriteLine("Read email login data: " + emailFile);
                        try
                        {   // Open the text file using a stream reader.
                            using (StreamReader sr = new StreamReader(emailFile))
                            {
                                emailAddress = sr.ReadLine();
                                username = sr.ReadLine();
                                password = sr.ReadLine();
                            }
                        }
                        catch (IOException e)
                        {
                            Console.WriteLine("The file " + emailFile + " could not be read:");
                            Console.WriteLine(e.Message);
                            return;
                        }
                    }

                    client.Connect(emailAddress, 993, SecureSocketOptions.SslOnConnect);
                    client.Authenticate(username, password);
                    client.Inbox.Open(FolderAccess.ReadWrite);

                    var uids = client.Inbox.Search(SearchQuery.All);
                    var importedCount = 0;

                    foreach (var uid in uids)
                    {
                        var message = client.Inbox.GetMessage(uid);

                        // write the message to a file
                        // message.WriteTo(string.Format("{0}.eml", uid));
                        var s = message.Subject;
                        // if (s.Contains("Neue PCN / PDN"))
                        if (s.Contains("PCN Notification"))
                        {
                            var html = message.HtmlBody;
                            if (html != null)
                            {
                                // int downloadPos = html.IndexOf(">herunterladen<");
                                int downloadPos = html.IndexOf(">download<");
                                while (downloadPos != -1)
                                {
                                    int hrefPos = html.IndexOf("href=");
                                    int lastHrefPos = -1;
                                    while (hrefPos != -1 && hrefPos < downloadPos)
                                    {
                                        lastHrefPos = hrefPos;
                                        hrefPos = html.IndexOf("href=", lastHrefPos + 1);
                                    }
                                    if (lastHrefPos != -1)
                                    {
                                        int hl = "href=".Length;
                                        string link = html.Substring(lastHrefPos + hl, downloadPos - lastHrefPos - hl);
                                        link = link.Replace("&amp;", "&");
                                        Console.WriteLine(link);
                                        var handler = new HttpClientHandler();
                                        if (AasxServer.AasxTask.proxy != null)
                                            handler.Proxy = AasxServer.AasxTask.proxy;
                                        else
                                            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                                        var httpClient = new HttpClient(handler);
                                        var getTask = httpClient.GetAsync(link);
                                        getTask.Wait(30000);
                                        if (getTask.Result.IsSuccessStatusCode)
                                        {
                                            string fileHttp = getTask.Result.Content.ReadAsStringAsync().Result;
                                            var filePos = fileHttp.IndexOf("URL=");
                                            if (filePos != -1)
                                            {
                                                var filePosEnd = fileHttp.IndexOf("\"", filePos);
                                                if (filePosEnd != -1)
                                                {
                                                    int u = "URL=".Length;
                                                    string fNameUrl = fileHttp.Substring(filePos + u, filePosEnd - filePos - u);
                                                    int iFile = fNameUrl.IndexOf("/$File/");
                                                    if (iFile != -1)
                                                    {
                                                        SubmodelElementCollection c = null;
                                                        string fName = fNameUrl.Substring(iFile + "/$File/".Length);
                                                        fName = System.Text.RegularExpressions.Regex.Replace(fName, @"[\\/:*?,""<>|]", string.Empty);
                                                        fName = System.Text.RegularExpressions.Regex.Replace(fName, @" -", "_");
                                                        string name = Path.GetFileNameWithoutExtension(fName);
                                                        bool import = true;
                                                        foreach (var sme in imported.Value)
                                                        {
                                                            if (sme is Property p)
                                                            {
                                                                if (p.IdShort == name)
                                                                {
                                                                    import = false;
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                        if (import)
                                                        {
                                                            Console.WriteLine("Import: " + name);
                                                            importedCount++;
                                                            var p = new Property(DataTypeDefXsd.String, idShort: name);
                                                            imported.Add(p);
                                                            p.TimeStampCreate = timeStamp;
                                                            p.SetTimeStamp(timeStamp); getTask = httpClient.GetAsync(fNameUrl);
                                                            getTask.Wait(30000);
                                                            if (getTask.Result.IsSuccessStatusCode)
                                                            {
                                                                using (var fs = new FileStream("./pcn/" + fName, FileMode.Create))
                                                                {
                                                                    var fileTask = getTask.Result.Content.CopyToAsync(fs);
                                                                    fileTask.Wait(30000);
                                                                }
                                                                if (pcnEnv != null && pcnSub != null)
                                                                {
                                                                    pcnEnv.AddSupplementaryFileToStore("./pcn/" + fName, "/aasx", fName, false);
                                                                    c = new SubmodelElementCollection(idShort: name);
                                                                    c.TimeStampCreate = timeStamp;
                                                                    c.SetTimeStamp(timeStamp);
                                                                    var f = new AasCore.Aas3_0.File(contentType: "", idShort: name);
                                                                    f.TimeStampCreate = timeStamp;
                                                                    f.SetTimeStamp(timeStamp);
                                                                    f.Value = "/aasx/" + fName;
                                                                    pcnSub.Add(c);
                                                                    c.Add(f);
                                                                }
                                                                if (Path.GetExtension(fName).ToLower() == ".zip")
                                                                {
                                                                    string path = "./pcn/" + Path.GetFileNameWithoutExtension(fName);
                                                                    try
                                                                    {
                                                                        ZipFile.ExtractToDirectory("./pcn/" + fName, path);
                                                                    }
                                                                    catch
                                                                    {

                                                                    }
                                                                    // File.Delete("./pcn/" + fName);

                                                                    try
                                                                    {
                                                                        // Preprocessing
                                                                        // Find empty elements, i.e. Element without EndElement
                                                                        // Ignore these afterwards
                                                                        List<String> elements = new List<string>();
                                                                        List<String> endElements = new List<string>();
                                                                        List<String> emptyElements = new List<string>();

                                                                        XmlTextReader reader = new XmlTextReader(path + "/PCNbody.xml");
                                                                        while (reader != null && reader.Read())
                                                                        {
                                                                            string eName = reader.Name;
                                                                            switch (reader.NodeType)
                                                                            {
                                                                                case XmlNodeType.Element:
                                                                                    if (!elements.Contains(eName))
                                                                                        elements.Add(eName);
                                                                                    break;
                                                                                case XmlNodeType.EndElement:
                                                                                    if (!endElements.Contains(eName))
                                                                                        endElements.Add(eName);
                                                                                    break;
                                                                                default:
                                                                                    break;
                                                                            }
                                                                        }
                                                                        reader.Close();
                                                                        foreach (var e in elements)
                                                                        {
                                                                            if (!endElements.Contains(e))
                                                                                emptyElements.Add(e);
                                                                        }

                                                                        reader = new XmlTextReader(path + "/PCNbody.xml");
                                                                        int stack = -1;
                                                                        string[] readerName = new string[10]
                                                                            { "", "", "", "", "", "", "", "", "", "" };
                                                                        string[] readerValue = new string[10]
                                                                            { "", "", "", "", "", "", "", "", "", "" };
                                                                        SubmodelElementCollection[] smc =
                                                                            new SubmodelElementCollection[10]
                                                                            { null, null, null, null, null, null, null, null, null, null };
                                                                        while (reader != null && reader.Read())
                                                                        {
                                                                            if (emptyElements.Contains(reader.Name))
                                                                                continue;

                                                                            switch (reader.NodeType)
                                                                            {
                                                                                case XmlNodeType.Element: // The node is an element.
                                                                                    if (stack >= 0 && smc[stack] == null)
                                                                                    {
                                                                                        smc[stack] = new SubmodelElementCollection(idShort: readerName[stack]);
                                                                                        smc[stack].TimeStampCreate = timeStamp;
                                                                                        smc[stack].SetTimeStamp(timeStamp);
                                                                                        if (stack == 0)
                                                                                        {
                                                                                            if (c != null)
                                                                                                c.Add(smc[stack]);
                                                                                        }
                                                                                        if (stack > 0)
                                                                                        {
                                                                                            if (smc[stack - 1] != null)
                                                                                                smc[stack - 1].Add(smc[stack]);
                                                                                        }
                                                                                    }
                                                                                    stack++;
                                                                                    if (stack >= 0 && stack < 10)
                                                                                        readerName[stack] = reader.Name;
                                                                                    break;
                                                                                case XmlNodeType.Text:
                                                                                    if (stack >= 0 && stack < 10)
                                                                                        readerValue[stack] = reader.Value;
                                                                                    break;
                                                                                case XmlNodeType.EndElement:
                                                                                    if (stack >= 0 && stack < 10)
                                                                                    {
                                                                                        if (readerName[stack] != "" && readerValue[stack] != "")
                                                                                        {
                                                                                            var pxml = new Property(DataTypeDefXsd.String, idShort: readerName[stack]);
                                                                                            pxml.TimeStampCreate = timeStamp;
                                                                                            pxml.SetTimeStamp(timeStamp);
                                                                                            pxml.Value = readerValue[stack];
                                                                                            if (stack == 0)
                                                                                            {
                                                                                                if (c != null)
                                                                                                    c.Add(pxml);
                                                                                            }
                                                                                            if (stack > 0)
                                                                                            {
                                                                                                if (smc[stack - 1] != null)
                                                                                                    smc[stack - 1].Add(pxml);
                                                                                            }
                                                                                        }
                                                                                        {
                                                                                        }
                                                                                        readerName[stack] = "";
                                                                                        readerValue[stack] = "";
                                                                                        smc[stack] = null;
                                                                                    }
                                                                                    stack--;
                                                                                    break;
                                                                                default:
                                                                                    break;
                                                                            }
                                                                        }
                                                                        reader.Close();
                                                                    }
                                                                    catch
                                                                    {
                                                                        Console.WriteLine("Can not parse XML of: " + fName);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    // html = html.Substring(downloadPos + ">herunterladen<".Length);
                                    // downloadPos = html.IndexOf(">herunterladen<");
                                    html = html.Substring(downloadPos + ">download<".Length);
                                    downloadPos = html.IndexOf(">download<");
                                }
                            }
                        }
                        client.Inbox.AddFlags(uid, MessageFlags.Deleted, true);
                    }
                    try
                    {
                        Console.WriteLine("ImportedCount: " + importedCount);
                        if (importedCount > 0)
                            AasxServer.Program.env[0].SaveAs(AasxServer.Program.envFileName[0]);
                    }
                    catch
                    {
                        Console.WriteLine("Can not write: " + AasxServer.Program.envFileName[0]);
                    }
                    // if (!error)
                    // client.Inbox.Expunge();
                    client.Disconnect(true);
                    AasxServer.Program.signalNewData(2);
                }
            }
            Thread.Sleep(5 * 60 * 1000);
        }

        static void xmlImport()
        {
            Console.WriteLine("Importing xml files from ./xml to submodel xml");

            AdminShellPackageEnv xmlEnv = null;
            ISubmodel xmlSub = null;
            SubmodelElementCollection imported = null;
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
                            if (sm != null && sm.IdShort != null && sm.IdShort.ToLower().Contains("xml"))
                            {
                                xmlEnv = env;
                                xmlSub = sm;

                                foreach (var sme in sm.SubmodelElements)
                                {
                                    if (sme is SubmodelElementCollection smc)
                                    {
                                        if (smc.IdShort == "imported")
                                        {
                                            imported = smc;
                                            break;
                                        }
                                    }
                                }
                                if (imported == null)
                                {
                                    imported = new SubmodelElementCollection(idShort: "imported");
                                    sm.Add(imported);
                                }
                            }
                        }
                    }
                }
            }
            if (xmlEnv == null || xmlSub == null || imported == null)
                return;
            // if (!Directory.Exists("./xml"))
            {
                DateTime timeStamp = DateTime.UtcNow;
                System.IO.DirectoryInfo ParentDirectory = new System.IO.DirectoryInfo("./xml");
                foreach (System.IO.FileInfo fi in ParentDirectory.GetFiles("*.xml"))
                {
                    string name = fi.Name;
                    var c = new SubmodelElementCollection(idShort: name.Replace(".", "_"));
                    c.TimeStampCreate = timeStamp;
                    c.SetTimeStamp(timeStamp);
                    /*
                    var f = File.CreateNew(name);
                    f.TimeStampCreate = timeStamp;
                    f.SetTimeStamp(timeStamp);
                    f.value = "/aasx/" + name;
                    c.Add(f);
                    */
                    xmlSub.Add(c);

                    var reader = new XmlTextReader(fi.DirectoryName + "/" + name);
                    int stack = -1;
                    string[] readerName = new string[100];
                    string[] readerValue = new string[100];
                    SubmodelElementCollection[] smc =
                        new SubmodelElementCollection[100];
                    while (reader != null && reader.Read())
                    {
                        // if (emptyElements.Contains(reader.Name))
                        //    continue;

                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element: // The node is an element.
                                if (reader.IsEmptyElement)
                                    break;
                                if (stack >= 0 && smc[stack] == null)
                                {
                                    smc[stack] = new SubmodelElementCollection(idShort: readerName[stack]);
                                    smc[stack].TimeStampCreate = timeStamp;
                                    smc[stack].SetTimeStamp(timeStamp);
                                    if (stack == 0)
                                    {
                                        if (c != null)
                                            c.Add(smc[stack]);
                                    }
                                    if (stack > 0)
                                    {
                                        if (smc[stack - 1] != null)
                                            smc[stack - 1].Add(smc[stack]);
                                    }
                                }
                                stack++;
                                if (stack >= 0 && stack < 100)
                                {
                                    if (reader.HasAttributes)
                                    {
                                        smc[stack] = new SubmodelElementCollection(idShort: reader.Name);
                                        smc[stack].TimeStampCreate = timeStamp;
                                        smc[stack].SetTimeStamp(timeStamp);
                                        Qualifier q = new Qualifier("XmlHasAttributes", DataTypeDefXsd.String);
                                        smc[stack].Qualifiers = new List<IQualifier>();
                                        smc[stack].Qualifiers.Add(q);
                                        if (stack == 0)
                                        {
                                            if (c != null)
                                                c.Add(smc[stack]);
                                        }
                                        if (stack > 0)
                                        {
                                            if (smc[stack - 1] != null)
                                                smc[stack - 1].Add(smc[stack]);
                                        }

                                        for (int i = 0; i < reader.AttributeCount; i++)
                                        {
                                            reader.MoveToAttribute(i);
                                            string n = reader.Name;
                                            string v = reader.Value;
                                            var p = new Property(DataTypeDefXsd.String, idShort: n);
                                            p.TimeStampCreate = timeStamp;
                                            p.SetTimeStamp(timeStamp);
                                            p.Value = v;
                                            q = new Qualifier("XmlAttribute", DataTypeDefXsd.String);
                                            p.Qualifiers = new List<IQualifier>();
                                            p.Qualifiers.Add(q);
                                            if (smc[stack] != null)
                                                smc[stack].Add(p);
                                        }
                                        reader.MoveToElement();
                                    }
                                    else
                                        readerName[stack] = reader.Name;
                                }
                                break;
                            case XmlNodeType.Text:
                                if (stack >= 0 && stack < 100)
                                {
                                    if (smc[stack] != null)
                                    {
                                        string n = smc[stack].IdShort;
                                        string v = reader.Value;
                                        var p = new Property(DataTypeDefXsd.String, idShort: n);
                                        p.TimeStampCreate = timeStamp;
                                        p.SetTimeStamp(timeStamp);
                                        p.Value = v;
                                        smc[stack].Add(p);
                                    }
                                    else
                                        readerValue[stack] = reader.Value;
                                }
                                break;
                            case XmlNodeType.EndElement:
                                if (stack >= 0 && stack < 100)
                                {
                                    if (readerName[stack] != null && readerValue[stack] != null)
                                    {
                                        var pxml = new Property(DataTypeDefXsd.String, idShort: readerName[stack]);
                                        pxml.TimeStampCreate = timeStamp;
                                        pxml.SetTimeStamp(timeStamp);
                                        pxml.Value = readerValue[stack];
                                        if (stack == 0)
                                        {
                                            if (c != null)
                                                c.Add(pxml);
                                        }
                                        if (stack > 0)
                                        {
                                            if (smc[stack - 1] != null)
                                                smc[stack - 1].Add(pxml);
                                        }
                                    }
                                    readerName[stack] = null;
                                    readerValue[stack] = null;
                                    smc[stack] = null;
                                }
                                stack--;
                                break;
                            default:
                                break;
                        }
                    }
                    reader.Close();
                }
            }

            return;
        }
    }
}