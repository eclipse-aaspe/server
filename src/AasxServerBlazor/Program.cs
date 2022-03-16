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

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();
            string[] url = config["Kestrel:Endpoints:Http:Url"].Split(':');
            if (url[2] != null)
                AasxServer.Program.blazorPort = url[2];

            CreateHostBuilder(args).Build().RunAsync();

            AasxServer.Program.Main(args);

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>()
                        .UseUrls("http://*:5000")
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
