using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Xml;
using AdminShellNS;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;

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
                AdminShell.Submodel pcnSub = null;
                AdminShell.SubmodelElementCollection imported = null;
                int aascount = AasxServer.Program.env.Length;

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
                                if (sm != null && sm.idShort != null && sm.idShort.ToLower().Contains("pcn"))
                                {
                                    pcnEnv = env;
                                    pcnSub = sm;

                                    foreach (var sme in sm.submodelElements)
                                    {
                                        if (sme.submodelElement is AdminShell.SubmodelElementCollection smc)
                                        {
                                            if (smc.idShort == "imported")
                                            {
                                                imported = smc;
                                                break;
                                            }
                                        }
                                    }
                                    if (imported == null)
                                    {
                                        imported = AdminShell.SubmodelElementCollection.CreateNew("imported");
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
                    if (File.Exists(emailFile))
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

                    if (File.Exists(emailFile))
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
                    int importedCount = 0;
                    bool error = false;

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
                                                        AdminShell.SubmodelElementCollection c = null;
                                                        string fName = fNameUrl.Substring(iFile + "/$File/".Length);
                                                        fName = System.Text.RegularExpressions.Regex.Replace(fName, @"[\\/:*?,""<>|]", string.Empty);
                                                        fName = System.Text.RegularExpressions.Regex.Replace(fName, @" -", "_");
                                                        string name = Path.GetFileNameWithoutExtension(fName);
                                                        bool import = true;
                                                        foreach (var sme in imported.value)
                                                        {
                                                            if (sme.submodelElement is AdminShell.Property p)
                                                            {
                                                                if (p.idShort == name)
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
                                                            var p = AdminShell.Property.CreateNew(name);
                                                            imported.Add(p);
                                                            p.TimeStampCreate = timeStamp;
                                                            p.setTimeStamp(timeStamp); getTask = httpClient.GetAsync(fNameUrl);
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
                                                                    c = AdminShell.SubmodelElementCollection.CreateNew(name);
                                                                    c.TimeStampCreate = timeStamp;
                                                                    c.setTimeStamp(timeStamp);
                                                                    var f = AdminShell.File.CreateNew(name);
                                                                    f.TimeStampCreate = timeStamp;
                                                                    f.setTimeStamp(timeStamp);
                                                                    f.value = "/aasx/" + fName;
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
                                                                        AdminShell.SubmodelElementCollection[] smc =
                                                                            new AdminShell.SubmodelElementCollection[10]
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
                                                                                        smc[stack] = AdminShell.SubmodelElementCollection.CreateNew(readerName[stack]);
                                                                                        smc[stack].TimeStampCreate = timeStamp;
                                                                                        smc[stack].setTimeStamp(timeStamp);
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
                                                                                            var pxml = AdminShell.Property.CreateNew(readerName[stack]);
                                                                                            pxml.TimeStampCreate = timeStamp;
                                                                                            pxml.setTimeStamp(timeStamp);
                                                                                            pxml.valueType = "string";
                                                                                            pxml.value = readerValue[stack];
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
                                                                        error = true;
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
                        error = true;
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
            AdminShell.Submodel xmlSub = null;
            AdminShell.SubmodelElementCollection imported = null;
            int aascount = AasxServer.Program.env.Length;

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
                            if (sm != null && sm.idShort != null && sm.idShort.ToLower().Contains("xml"))
                            {
                                xmlEnv = env;
                                xmlSub = sm;

                                foreach (var sme in sm.submodelElements)
                                {
                                    if (sme.submodelElement is AdminShell.SubmodelElementCollection smc)
                                    {
                                        if (smc.idShort == "imported")
                                        {
                                            imported = smc;
                                            break;
                                        }
                                    }
                                }
                                if (imported == null)
                                {
                                    imported = AdminShell.SubmodelElementCollection.CreateNew("imported");
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
                    var c = AdminShell.SubmodelElementCollection.CreateNew(name.Replace(".", "_"));
                    c.TimeStampCreate = timeStamp;
                    c.setTimeStamp(timeStamp);
                    /*
                    var f = AdminShell.File.CreateNew(name);
                    f.TimeStampCreate = timeStamp;
                    f.setTimeStamp(timeStamp);
                    f.value = "/aasx/" + name;
                    c.Add(f);
                    */
                    xmlSub.Add(c);

                    var reader = new XmlTextReader(fi.DirectoryName + "/" + name);
                    int stack = -1;
                    string[] readerName = new string[100];
                    string[] readerValue = new string[100];
                    AdminShell.SubmodelElementCollection[] smc =
                        new AdminShell.SubmodelElementCollection[100];
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
                                    smc[stack] = AdminShell.SubmodelElementCollection.CreateNew(readerName[stack]);
                                    smc[stack].TimeStampCreate = timeStamp;
                                    smc[stack].setTimeStamp(timeStamp);
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
                                        smc[stack] = AdminShell.SubmodelElementCollection.CreateNew(reader.Name);
                                        smc[stack].TimeStampCreate = timeStamp;
                                        smc[stack].setTimeStamp(timeStamp);
                                        AdminShell.Qualifier q = new AdminShellV20.Qualifier();
                                        q.type = "XmlHasAttributes";
                                        smc[stack].qualifiers = new AdminShellV20.QualifierCollection();
                                        smc[stack].qualifiers.Add(q);
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
                                            var p = AdminShell.Property.CreateNew(n);
                                            p.TimeStampCreate = timeStamp;
                                            p.setTimeStamp(timeStamp);
                                            p.valueType = "string";
                                            p.value = v;
                                            q = new AdminShellV20.Qualifier();
                                            q.type = "XmlAttribute";
                                            p.qualifiers = new AdminShellV20.QualifierCollection();
                                            p.qualifiers.Add(q);
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
                                        string n = smc[stack].idShort;
                                        string v = reader.Value;
                                        var p = AdminShell.Property.CreateNew(n);
                                        p.TimeStampCreate = timeStamp;
                                        p.setTimeStamp(timeStamp);
                                        p.valueType = "string";
                                        p.value = v;
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
                                        var pxml = AdminShell.Property.CreateNew(readerName[stack]);
                                        pxml.TimeStampCreate = timeStamp;
                                        pxml.setTimeStamp(timeStamp);
                                        pxml.valueType = "string";
                                        pxml.value = readerValue[stack];
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