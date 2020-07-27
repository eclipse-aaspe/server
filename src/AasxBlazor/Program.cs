using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
/*
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Transports;
using Owin;

namespace MyApplication
{
    public static class Startup
    {
        public static void ConfigureSignalR(IAppBuilder app)
        {
            // If using the global dependency resolver
            TurnOfForeverFrame(GlobalHost.DependencyResolver);
            app.MapSignalR();
        }
        public static void TurnOfForeverFrame(IDependencyResolver resolver)
        {
            var transportManager = resolver.Resolve<ITransportManager>() as TransportManager;
            transportManager.Remove("webSockets");
        }
    }
}
*/

namespace AasxBlazor
{
    public class Program1
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(Directory.GetCurrentDirectory());

            CreateHostBuilder(args).Build().RunAsync();

            Net46ConsoleServer.Program.Main(args);
            
            // CreateHostBuilder(args).Build().Run();

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>()
                        .UseUrls("http://*:5000");
                });
    }
}
