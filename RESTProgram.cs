using AasxRestServerLibrary;
using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxRestConsoleServer
{
    class Program
    {
        static void Main2(string[] args)
        {
            return;
            // default command line options
            var fn = "Festo-USB-stick-sample-admin-shell.aasx";
            var host = "localhost";
            var port = "1111";

            // parse
            int i = 0;
            while (i < args.Length)
            {
                var x = args[i].Trim().ToLower();

               // real option?
               if (i < args.Length-1)
               {
                    if (x == "-host")
                    {
                        host = args[i + 1];
                        i += 2;
                        continue;
                    }

                    if (x == "-port")
                    {
                        port = args[i + 1];
                        i += 2;
                        continue;
                    }
                }

                // last??
                fn = args[i];
                i += 1;
            }

            // load?
            var package = new AdminShell.PackageEnv(fn);
            AasxRestServer.Start(package, host, port, new GrapevineLoggerToConsole());

            // wait for RETURN
            Console.ReadLine();
            AasxRestServer.Stop();
        }
    }
}
