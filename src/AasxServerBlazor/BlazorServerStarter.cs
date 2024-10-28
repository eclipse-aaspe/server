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
    private const string DefaultPort = "5001";
    private const string DefaultUrl = $"http://*:{DefaultPort}";
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
        // QueryGrammar.storeSecurityRoles(GlobalSecurityVariables.SecurityRoles);
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
        if (url?.Length > 2 && int.TryParse(url[2], out var port))
        {
            // Use the dynamically retrieved port
            Program.blazorPort = port.ToString();
        }
        else
        {
            // Use default port if dynamic port retrieval fails
            Program.blazorPort = DefaultPort;
        }
        
        return Host.CreateDefaultBuilder(args)
                   .ConfigureWebHostDefaults(webBuilder =>
                                             {
                                                 webBuilder
                                                     .UseUrls(DefaultUrl)
                                                     .UseStartup<Startup>();
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