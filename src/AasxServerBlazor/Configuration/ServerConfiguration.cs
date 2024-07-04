/********************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
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

using System;
using System.Collections.Generic;
using System.IO;
using AasSecurity;
using AasxServerDB;
using AasxServerStandardBib.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using IO.Swagger.Controllers;
using IO.Swagger.Lib.V3.Formatters;
using IO.Swagger.Lib.V3.Middleware;
using IO.Swagger.Registry.Lib.V3.Formatters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace AasxServerBlazor.Configuration;

public static class ServerConfiguration
{
    private const string ActivatedKey = "activated";
    private const string AuthenticationScheme = "AasSecurityAuth";
    private const string CorsPolicyName = "AllowAll";
    private const string ErrorHandlingPath = "/Error";
    private const string FallbackHostPattern = "/_Host";
    private const string HttpRestAssetAdministrationShellRepository = "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Repository";
    private const string SwaggerJsonEndpoint = "Final-Draft/swagger.json";
    private const string SwaggerRoutePrefix = "swagger";
    private const string SyntaxHighLightLowercase = "syntaxHighlight";
    private const string SyntaxHighlightUppercase = "SyntaxHighlight";

    /// <summary>
    /// Configures server-related services for the application.
    /// </summary>
    /// <param name="services">The collection of services to configure.</param>
    public static void ConfigureServer(IServiceCollection services)
    {
        ConfigureKestrel(services);
        ConfigureFormOptions(services);
        ConfigureRazorPages(services);
        ConfigureCors(services);

        services.AddControllers();
        services.AddLazyResolution();

        services.AddGraphQLServer().AddQueryType<Query>();
    }

    /// <summary>
    /// Adds framework-specific services required by the application.
    /// </summary>
    /// <param name="services">The collection of services to configure.</param>
    public static void AddFrameworkServices(IServiceCollection services)
    {
        services.AddLogging(loggingBuilder => { loggingBuilder.AddConsole(); });

        AddMvc(services);
        AddSwaggerGen(services);

        services.AddAuthentication(AuthenticationScheme)
            .AddScheme<AasSecurityAuthenticationOptions, AasSecurityAuthenticationHandler>(AuthenticationScheme, null);

        AddAuthorization(services);
    }

    /// <summary>
    /// Configures Swagger UI for API documentation.
    /// </summary>
    /// <param name="app">The application builder used to configure the middleware pipeline.</param>
    /// <param name="configuration">The configuration settings for the application.</param>
    public static void ConfigureSwagger(IApplicationBuilder app, IConfiguration configuration)
    {
        app.UseSwagger();
        app.UseSwaggerUI(swaggerUiOptions =>
        {
            //TODO: Either use the SwaggerGen generated Swagger contract (generated from C# classes) or
            //alternatively use the original Swagger contract that's included in the static files
            swaggerUiOptions.SwaggerEndpoint(SwaggerJsonEndpoint,
                HttpRestAssetAdministrationShellRepository);
            swaggerUiOptions.RoutePrefix = SwaggerRoutePrefix;
            swaggerUiOptions.ConfigObject.AdditionalItems[SyntaxHighLightLowercase] = new Dictionary<string, object>
            {
                [ActivatedKey] = false
            };

            var syntaxHighlight = configuration[SyntaxHighlightUppercase];
            swaggerUiOptions.ConfigObject.AdditionalItems[SyntaxHighLightLowercase] = new Dictionary<string, object>
            {
                [ActivatedKey] = syntaxHighlight
            };
        });
    }

    /// <summary>
    /// Configures the HTTP request pipeline for the application.
    /// </summary>
    /// <param name="app">The application builder used to configure the middleware pipeline.</param>
    /// <param name="env">The hosting environment the application is running in.</param>
    public static void ConfigureEnvironment(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler(ErrorHandlingPath);
        }

        app.UseMiddleware<ExceptionMiddleware>();

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();
        app.UseCors(CorsPolicyName);
        
        app.UseEndpoints(ConfigureEndpoints);
    }

    #region Endpoint Configuration

    private static void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapBlazorHub(options =>
        {
            // Do NOT use Websockets
            options.Transports =
                HttpTransportType.ServerSentEvents |
                HttpTransportType.LongPolling;
        });
        endpoints.MapFallbackToPage(FallbackHostPattern);
        endpoints.MapControllers();
        endpoints.MapGraphQL();
    }

    #endregion

    #region Server Configuration

    private static void ConfigureCors(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
    }

    private static void ConfigureFormOptions(IServiceCollection services)
    {
        services.Configure<FormOptions>(formOptions =>
        {
            formOptions.ValueLengthLimit = int.MaxValue;
            formOptions.MultipartBodyLengthLimit = int.MaxValue;
            formOptions.MultipartHeadersLengthLimit = int.MaxValue;
        });
    }

    private static void ConfigureKestrel(IServiceCollection services)
    {
        services.Configure<KestrelServerOptions>(options =>
        {
            options.AllowSynchronousIO = true;
            options.Limits.MaxRequestBodySize = int.MaxValue;
        });
    }

    private static void ConfigureRazorPages(IServiceCollection services)
    {
        services.AddRazorPages();
        services.AddServerSideBlazor();
    }

    #endregion

    #region Framework Services Configuration

    private static void AddAuthorization(IServiceCollection services)
    {
        services.AddAuthorization(authorizationOptions =>
        {
            authorizationOptions.AddPolicy("SecurityPolicy", policy =>
            {
                policy.AuthenticationSchemes.Add(AuthenticationScheme);
                policy.Requirements.Add(new SecurityRequirement());
            });
        });
    }

    private static void AddMvc(IServiceCollection services)
    {
        services.AddMvc(options =>
        {
            options.InputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonInputFormatter>();
            options.OutputFormatters
                .RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonOutputFormatter>();
            options.InputFormatters.Add(new AasRequestFormatter());
            options.OutputFormatters.Add(new AasResponseFormatter());
            options.InputFormatters.Add(new AasDescriptorRequestFormatter());
            options.OutputFormatters.Add(new AasDescriptorResponseFormatter());
        }).AddNewtonsoftJson(opts =>
        {
            opts.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
                {
                    ProcessDictionaryKeys = false
                }
            };
            opts.SerializerSettings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
            opts.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        });
    }

    private static void AddSwaggerGen(IServiceCollection services)
    {
        services.AddSwaggerGen(swaggerGenOptions =>
        {
            swaggerGenOptions.SwaggerDoc("Final-Draft", new OpenApiInfo
            {
                Version = "Final-Draft",
                Title = HttpRestAssetAdministrationShellRepository,
                Description =
                    "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Repository (ASP.NET Core 3.1)",
                Contact = new OpenApiContact()
                {
                    Name = "Michael Hoffmeister, Torben Miny, Andreas Orzelski, Manuel Sauer, Constantin Ziesche",
                    Url = new Uri("https://github.com/swagger-api/swagger-codegen"),
                    Email = ""
                },
                TermsOfService = new Uri("https://github.com/admin-shell-io/aas-specs")
            });

            swaggerGenOptions.EnableAnnotations();
            swaggerGenOptions.CustomSchemaIds(type => type.FullName);

            var swaggerCommentedAssembly =
                typeof(AssetAdministrationShellRepositoryAPIApiController).Assembly.GetName().Name;
            swaggerGenOptions.IncludeXmlComments(
                $"{AppContext.BaseDirectory}{Path.DirectorySeparatorChar}{swaggerCommentedAssembly}.xml");
            swaggerGenOptions.OperationFilter<IO.Swagger.Filters.GeneratePathParamsValidationFilter>();
        });
    }

    #endregion
}