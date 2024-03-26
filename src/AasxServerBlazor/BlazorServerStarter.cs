using AasSecurity;
using AasxServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace AasxServerBlazor
{
    public class BlazorServerStarter
    {
        private const string AppSettingsFileName = "appsettings.json";
        private const string KestrelEndpointsHttpUrl = "Kestrel:Endpoints:Http:Url";
        private const string DefaultUrl = "http://*:5000";

        public static void Main(string[] args)
        {
            var config = LoadConfiguration();
            var url = config[KestrelEndpointsHttpUrl]?.Split(':');
            if (url?[2] != null)
                Program.blazorPort = url[2];

            var host = CreateHostBuilder(args).Build();

            host.RunAsync();

            Program.con = config;
            Program.Main(args);
            SecurityHelper.SecurityInit();

            host.WaitForShutdownAsync();

        }

        private static IConfigurationRoot LoadConfiguration()
        {
            Console.WriteLine($"Loading Configuration in path: {Directory.GetCurrentDirectory()}");

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(AppSettingsFileName)
                .AddEnvironmentVariables()
                .Build();
            return config;
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>()
                        .UseUrls(DefaultUrl)
                    ;
                });
    }
}
