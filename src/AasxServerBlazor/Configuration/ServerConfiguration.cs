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

namespace AasxServerBlazor.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AasSecurity;
using AasxServerStandardBib.ServiceExtensions;
using IO.Swagger.Controllers;
using IO.Swagger.Lib.V3.Formatters;
using IO.Swagger.Lib.V3.Middleware;
using IO.Swagger.Registry.Lib.V3.Formatters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using System.Text.Json;
using System.Text.Json.Serialization;
using AdminShellNS;
using Contracts;
using IO.Swagger.Lib.V3.MCP;
#if GRAPHQL
using HotChocolate.AspNetCore;
#endif

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

#if GRAPHQL
        services.AddGraphQLServer()
            .AddQueryType<GraphQLAPI>()
            .UseField<ParameterNamesMiddleware>()
            .SetRequestOptions(_ =>
                new HotChocolate.Execution.Options.RequestExecutorOptions {
                    ExecutionTimeout = TimeSpan.FromMinutes(10),
                    IncludeExceptionDetails = true
                });
#endif

        // MCP-Server (Streamable HTTP) als dünner Adapter über die Query-Pipeline. Immer aktiv.
        // Zwei Endpunkte: /mcp (voll, alle Tools) und /mcp-basic (nur aas_find_product, für schwache Modelle).
        // Die Aufteilung erfolgt über Request-Filter, die den Request-Pfad prüfen.
        services.AddHttpContextAccessor();
        services.AddMcpServer()
            .WithHttpTransport()
            .WithTools<McpQueryTools>()
            // Exportdateien (CSV/XLSX) der Export-Tools als MCP-Resource: aas-export://{token}.
            .WithResources<McpExportResources>()
            .WithRequestFilters(filters =>
            {
                filters.AddListToolsFilter(next => async (context, ct) =>
                {
                    var result = await next(context, ct);
                    var allowed = AllowedMcpTools(context.Services);
                    if (result.Tools is not null)
                    {
                        if (allowed is not null)
                        {
                            result.Tools = result.Tools.Where(t => allowed.Contains(t.Name)).ToList();
                        }

                        // OpenAI Apps SDK / ChatGPT compatibility: declare each (read-only, public) tool as
                        // no-auth and give it invocation status text. securitySchemes is mirrored into _meta
                        // (the location ChatGPT reads; the typed SDK cannot emit a custom top-level field).
                        // title + readOnly/idempotent/etc. annotations come from the [McpServerTool] attributes.
                        foreach (var tool in result.Tools)
                        {
                            var meta = tool.Meta ?? new System.Text.Json.Nodes.JsonObject();
                            meta["securitySchemes"] = new System.Text.Json.Nodes.JsonArray(
                                new System.Text.Json.Nodes.JsonObject { ["type"] = "noauth" });
                            if (McpToolStatus.TryGetValue(tool.Name, out var status))
                            {
                                meta["openai/toolInvocation/invoking"] = status.Invoking;
                                meta["openai/toolInvocation/invoked"] = status.Invoked;
                            }
                            tool.Meta = meta;
                        }
                    }

                    return result;
                });

                filters.AddCallToolFilter(next => async (context, ct) =>
                {
                    var allowed = AllowedMcpTools(context.Services);
                    if (allowed is not null
                        && context.Params is not null
                        && !allowed.Contains(context.Params.Name))
                    {
                        throw new InvalidOperationException(
                            $"Tool '{context.Params.Name}' ist auf diesem MCP-Endpunkt nicht verfügbar.");
                    }

                    var logSession = ExplicitLogSession(context.Services);
                    if (!McpSessionLogStore.IsValidSessionId(logSession))
                    {
                        return await next(context, ct);
                    }

                    using var _ = DiagnosticsLog.BeginBrowserLogSession(logSession!);
                    return await next(context, ct);
                });
            });
    }

    // Per-tool invocation status text (OpenAI Apps SDK _meta), kept <= 64 chars per the spec.
    private static readonly IReadOnlyDictionary<string, (string Invoking, string Invoked)> McpToolStatus =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["aas_query"]               = ("Searching Voyager…", "Voyager search complete"),
            ["aas_query_export_csv"]    = ("Exporting Voyager results…", "CSV export ready"),
            ["aas_query_export_xlsx"]   = ("Exporting Voyager results…", "Excel export ready"),
            ["aas_count"]               = ("Counting Voyager results…", "Voyager count complete"),
            ["aas_get_submodel"]        = ("Reading AAS submodel…", "AAS submodel loaded"),
            ["aas_get_submodels"]       = ("Reading AAS submodels…", "AAS submodels loaded"),
            ["aas_get_shell"]           = ("Reading AAS shell…", "AAS shell loaded"),
            ["aas_get_shells"]          = ("Reading AAS shells…", "AAS shells loaded"),
            ["aas_get_product"]         = ("Reading AAS product…", "AAS product loaded"),
            ["aas_find_product"]        = ("Finding AAS product…", "AAS product found"),
            ["aas_find_product_simple"] = ("Finding product…", "Product found"),
            ["aas_get_element"]         = ("Reading AAS element…", "AAS element loaded"),
            ["aas_find_concepts"]       = ("Searching concept definitions…", "Concept definitions found"),
            ["aas_describe_model"]      = ("Analyzing data structures…", "Data structure overview ready"),
        };

    // Erlaubter Tool-Satz je nach MCP-Endpunkt-Pfad (null = alle Tools, voller Endpunkt /mcp).
    // /mcp-simple zuerst prüfen (Pfad enthält nicht "mcp-basic"), dann /mcp-basic, sonst voll.
    private static HashSet<string>? AllowedMcpTools(IServiceProvider? services)
    {
        var path = services?.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()?.HttpContext?.Request.Path.Value;
        if (path is null)
        {
            return null;
        }

        if (path.Contains("mcp-simple", StringComparison.OrdinalIgnoreCase))
        {
            return McpQueryTools.SimpleToolNames;
        }

        if (path.Contains("mcp-basic", StringComparison.OrdinalIgnoreCase))
        {
            return McpQueryTools.BasicToolNames;
        }

        return null;
    }

    private static string? ExplicitLogSession(IServiceProvider? services)
    {
        var request = services?
            .GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()?
            .HttpContext?
            .Request;

        return request?.Query.TryGetValue("logSession", out var value) == true
            ? value.FirstOrDefault()
            : null;
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
                             swaggerUiOptions.RoutePrefix                                            = SwaggerRoutePrefix;
                             swaggerUiOptions.ConfigObject.AdditionalItems[SyntaxHighLightLowercase] = new Dictionary<string, object> {[ActivatedKey] = false};

                             var syntaxHighlight = configuration[SyntaxHighlightUppercase];
                             swaggerUiOptions.ConfigObject.AdditionalItems[SyntaxHighLightLowercase] = new Dictionary<string, object> {[ActivatedKey] = syntaxHighlight};
                         });
    }

    /// <summary>
    /// Configures the HTTP request pipeline for the application.
    /// </summary>
    /// <param name="app">The application builder used to configure the middleware pipeline.</param>
    /// <param name="env">The hosting environment the application is running in.</param>
    public static void ConfigureEnvironment(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration)
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
        app.UsePathBase("/api/v3.0");
        app.UseRouting();
        app.UseAuthorization();
        app.UseCors(CorsPolicyName);

        // Swagger must be registered BEFORE UseEndpoints: the Blazor fallback
        // endpoint (MapFallbackToPage("/_Host")) matches every unmatched route
        // and would otherwise swallow "/swagger" / "/swagger/index.html".
        ConfigureSwagger(app, configuration);

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
#if GRAPHQL
        endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions
        {
            Tool = { Enable = true }
        });
#endif
        // MCP-Endpoints (Streamable HTTP). PathBase ist "/api/v3.0", real also "/api/v3.0/mcp" bzw. "/api/v3.0/mcp-basic".
        // /mcp = voller Toolsatz (starke Modelle), /mcp-basic = nur aas_find_product (schwache Modelle); Filter s. ConfigureMcp.
        endpoints.MapMcp("/mcp");
        endpoints.MapMcp("/mcp-basic");
        endpoints.MapMcp("/mcp-simple");

        // Download-Endpunkt für die von aas_query_export_csv/xlsx erzeugten Dateien.
        // Das Token stammt aus der Tool-Antwort (downloadUrl); die Dateien verfallen nach ca. 60 Minuten.
        endpoints.MapGet("/mcp-exports/{token}", (string token) =>
        {
            if (!McpExportFileStore.TryGet(token, out var content, out var fileName, out var mimeType))
            {
                return Microsoft.AspNetCore.Http.Results.NotFound();
            }

            return Microsoft.AspNetCore.Http.Results.File(content, mimeType, fileName);
        });
    }

#endregion

        #region Server Configuration

        private static void ConfigureCors(IServiceCollection services) =>
        services.AddCors(options =>
                         {
                             options.AddPolicy(CorsPolicyName, builder =>
                                                               {
                                                                   builder.AllowAnyOrigin()
                                                                          .AllowAnyMethod()
                                                                          .AllowAnyHeader();
                                                               });
                         });

    private static void ConfigureFormOptions(IServiceCollection services) =>
        services.Configure<FormOptions>(formOptions =>
                                        {
                                            formOptions.ValueLengthLimit            = int.MaxValue;
                                            formOptions.MultipartBodyLengthLimit    = int.MaxValue;
                                            formOptions.MultipartHeadersLengthLimit = int.MaxValue;
                                        });

    private static void ConfigureKestrel(IServiceCollection services) =>
        services.Configure<KestrelServerOptions>(options =>
                                                 {
                                                     options.AllowSynchronousIO        = true;
                                                     options.Limits.MaxRequestBodySize = int.MaxValue;
                                                 });

    private static void ConfigureRazorPages(IServiceCollection services)
    {
        services.AddRazorPages();
        services.AddServerSideBlazor()
             .AddHubOptions(options =>
             {
                 options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
                 options.HandshakeTimeout = TimeSpan.FromMinutes(2);
                 options.KeepAliveInterval = TimeSpan.FromMinutes(1);
             });
    }

    #endregion

    #region Framework Services Configuration

    private static void AddAuthorization(IServiceCollection services) =>
        services.AddAuthorizationBuilder()
                .AddPolicy("SecurityPolicy", policy =>
                                             {
                                                 policy.AuthenticationSchemes.Add(AuthenticationScheme);
                                                 policy.Requirements.Add(new SecurityRequirement());
                                             });

    private static void AddMvc(IServiceCollection services) =>
        services.AddMvc(options =>
                        {
                            // Remove the default System.Text.Json formatters
                            options.InputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonInputFormatter>();
                            options.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonOutputFormatter>();

                            // Add custom formatters
                            options.InputFormatters.Add(new AasRequestFormatter());
                            options.OutputFormatters.Add(new AasResponseFormatter());
                            options.InputFormatters.Add(new AasDescriptorRequestFormatter());
                            options.OutputFormatters.Add(new AasDescriptorResponseFormatter());
                        })
                .AddJsonOptions(opts =>
                                {
                                    // Configure JSON options to use camel case and ignore null values
                                    opts.JsonSerializerOptions.PropertyNamingPolicy   = JsonNamingPolicy.CamelCase;
                                    opts.JsonSerializerOptions.DictionaryKeyPolicy    = null; // Preserve dictionary key casing
                                    opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

                                    // Add a converter for enum types to be serialized as camel case strings
                                    opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

                                    // Add other necessary custom converters
                                    opts.JsonSerializerOptions.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));

                                    opts.JsonSerializerOptions.MaxDepth = 128; // increase default of 64
                                });


    private static void AddSwaggerGen(IServiceCollection services) =>
        services.AddSwaggerGen(swaggerGenOptions =>
                               {
                                   swaggerGenOptions.SwaggerDoc("Final-Draft", new OpenApiInfo
                                                                               {
                                                                                   Version = "V3.1",
                                                                                   Title   = HttpRestAssetAdministrationShellRepository,
                                                                                   Description =
                                                                                       "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Repository (V3.1)",
                                                                                   Contact = new OpenApiContact()
                                                                                             {
                                                                                                 Name
                                                                                                     = "Michael Hoffmeister, Torben Miny, Andreas Orzelski, Manuel Sauer, Constantin Ziesche",
                                                                                                 Url   = new Uri("https://github.com/swagger-api/swagger-codegen"),
                                                                                                 Email = ""
                                                                                             },
                                                                                   TermsOfService = new Uri("https://github.com/admin-shell-io/aas-specs")
                                                                               });

                                   swaggerGenOptions.EnableAnnotations();
                                   //Based on issue https://github.com/swagger-api/swagger-ui/issues/7911
                                   swaggerGenOptions.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

                                   var swaggerCommentedAssembly =
                                       typeof(AssetAdministrationShellRepositoryAPIApiController).Assembly.GetName().Name;
                                   swaggerGenOptions.IncludeXmlComments(
                                                                        $"{AppContext.BaseDirectory}{Path.DirectorySeparatorChar}{swaggerCommentedAssembly}.xml");
                                   swaggerGenOptions.OperationFilter<IO.Swagger.Filters.GeneratePathParamsValidationFilter>();
                               });

    #endregion
}
