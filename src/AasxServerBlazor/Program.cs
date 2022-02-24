using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AasxServerBlazor
{
    public class Program1
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(Directory.GetCurrentDirectory());

            //TODO DELETE this piece of code make wrong assumptions about the current directory
            /*
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();
            */

            //TODO REFACTOR it is not ensured that this is the actual endpoint. Why is this information needed so early in the lifecycle?
            /*
            string[] url = config["Kestrel:Endpoints:Http:Url"].Split(':');
            if (url[2] != null)
                AasxServer.Program.blazorHostPort = url[2];
            */

            CreateHostBuilder(args).Build().RunAsync();

            AasxServer.Program.Main(args);

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>()
                        //TODO CHECK why is a manuel override used? server does not uses same port consistently (sometimes 5000, sometimes port specified in appconfig.json) across different platforms
                        //.UseUrls("http://*:5000")
                    /*
                    .UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, 5000);  // http:localhost:5000
                        options.Listen(IPAddress.Any, 80);         // http:*:80
                        options.Listen(IPAddress.Loopback, 443, listenOptions =>
                        {
                            listenOptions.UseHttps("certificate.pfx", "password");
                        });
                    })
                    */
                    ;
                });
    }
}
