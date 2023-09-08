using AasSecurity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading;

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

            var host = CreateHostBuilder(args).Build();

            host.RunAsync();

            AasxServer.Program.Main(args);

            SecurityHelper.SecurityInit();

            host.WaitForShutdownAsync();

            //QuitEvent
            //HandleQuitEvent();
        }

        static void HandleQuitEvent()
        {
            ManualResetEvent quitEvent = new(false);
            try
            {
                Console.CancelKeyPress += (sender, eArgs) =>
                {
                    quitEvent.Set();
                    eArgs.Cancel = true;
                };
            }
            catch
            {
            }

            // wait for timeout or Ctrl-C
            quitEvent.WaitOne(Timeout.Infinite);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        //.UseStartup<Startup>()
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
