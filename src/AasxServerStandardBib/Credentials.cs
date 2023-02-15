using System;
using System.Collections.Generic;
using System.IO;

namespace AasxServer
{
    class AasxCredentialsEntry
    {
        public string urlPrefix = string.Empty;
        public string type = string.Empty;
        public List<string> parameters = new List<string>();
    }

    public class AasxCredentials
    {
        static List<AasxCredentialsEntry> credentials = new List<AasxCredentialsEntry>();

        public static void init()
        {
            init("CREDENTIALS-DEFAULT.DAT");
        }
        public static void init(string fileName)
        {
            credentials.Clear();
            if (File.Exists(fileName))
            {
                try
                {   // Open the text file using a stream reader.
                    using (StreamReader sr = new StreamReader(fileName))
                    {
                        var line = sr.ReadLine();
                        while (line != null)
                        {
                            if (line != "" && line.Substring(0, 1) != "#")
                            {
                                var cols = line.Split(',');
                                if (cols.Length > 2)
                                {
                                    var c = new AasxCredentialsEntry();
                                    c.urlPrefix = cols[0];
                                    c.type = cols[1];
                                    for (int i = 2; i < cols.Length; i++)
                                        c.parameters.Add(cols[i]);
                                    credentials.Add(c);
                                }
                            }
                            line = sr.ReadLine();
                        }
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine(fileName + " could not be read!");
                    Console.WriteLine(e.Message);
                    return;
                }
            }
        }

        public static bool get(string urlPath, out string queryPar)
        {
            queryPar=null;

            if (AasxServer.Program.Email != "")
            {
                queryPar = "Email=" + AasxServer.Program.Email;
                return true;
            }

            for (int i = 0; i < credentials.Count; i++)
            {
                int len = credentials[i].urlPrefix.Length;
                string u = urlPath.Substring(0, len);
                if (u == credentials[i].urlPrefix)
                {
                    switch (credentials[i].type)
                    {
                        case "email":
                            queryPar = "Email=" + credentials[i].parameters[0];
                            return true;
                    }
                }
            }

            return false; // no entry found
        }
    }
}
