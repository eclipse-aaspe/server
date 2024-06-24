using AasSecurity;
using AasxServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace AasxServerBlazor;

public static class BlazorServerStarter
{
    private const string AppSettingsFileName = "appsettings.json";
    private const string KestrelEndpointsHttpUrl = "Kestrel:Endpoints:Http:Url";
    private const string DefaultUrl = "http://localhost:5000";
    private const char KestrelUrlSeparator = ':';

    public static void Main(string[] args)
    {
        var config = LoadConfiguration();
        var host   = BuildHost(args, config);

        host.RunAsync();

        InitializeProgram(args, config);

        host.WaitForShutdownAsync();
    }

    private static void InitializeProgram(string[] args, IConfiguration config)
    {
        Console.WriteLine($"{nameof(InitializeProgram)}");
        Program.con = config;
        Program.Main(args);
        SecurityHelper.SecurityInit();
    }

    private static IHost BuildHost(string[] args, IConfiguration config)
    {
        Console.WriteLine($"{nameof(BuildHost)} with {config}");
        var hostBuilder = CreateHostBuilder(args, config);
        return hostBuilder.Build();
    }

    private static IHostBuilder CreateHostBuilder(string[] args, IConfiguration config)
    {
        var url = config[KestrelEndpointsHttpUrl]?.Split(KestrelUrlSeparator);
        if (url?[2] != null)
        {
            Program.blazorPort = url[2];
        }

        return Host.CreateDefaultBuilder(args)
                   .ConfigureWebHostDefaults(webBuilder =>
                                             {
                                                 webBuilder
                                                     .UseStartup<Startup>()
                                                     .UseUrls(DefaultUrl);
                                             });
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
}