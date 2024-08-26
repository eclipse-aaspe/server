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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AasxServerBlazor.Configuration;

namespace AasxServerBlazor;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }

    /// <summary>
    /// Configures the services for the application's dependency injection container.
    /// This method is called by the runtime. Use it to add services required by the application.
    /// For more information on how to configure your application, visit:
    /// <a href="https://go.microsoft.com/fwlink/?LinkID=398940">App startup in ASP.NET Core</a>
    /// </summary>
    /// <param name="services">The collection of services to configure.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        ServerConfiguration.ConfigureServer(services);

        DependencyRegistry.Register(services);

        ServerConfiguration.AddFrameworkServices(services);
    }

    /// <summary>
    /// Configures the HTTP request pipeline for the application.
    /// This method is called by the runtime. Use it to define how incoming requests are handled.
    /// </summary>
    /// <param name="app">The application builder used to configure the middleware pipeline.</param>
    /// <param name="environment">The hosting environment the application is running in.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
    {
        ServerConfiguration.ConfigureEnvironment(app, environment);

        ServerConfiguration.ConfigureSwagger(app, Configuration);
    }
}