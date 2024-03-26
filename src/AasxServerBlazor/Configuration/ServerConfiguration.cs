using System;
using System.Collections.Generic;
using System.IO;
using AasSecurity;
using AasxServer;
using AasxServerStandardBib.Extensions;
using IO.Swagger.Controllers;
using IO.Swagger.Lib.V3.Formatters;
using IO.Swagger.Lib.V3.Middleware;
using IO.Swagger.Registry.Lib.V3.Formatters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace AasxServerBlazor.Configuration;

public static class ServerConfiguration
{
    private const string CorsPolicyName = "AllowAll";

    public static void ConfigureServer(IServiceCollection services)
    {
        //jtikekar: changed w.r.t. AasDescriptorResponseFormatter
        services.Configure<KestrelServerOptions>(options =>
        {
            options.AllowSynchronousIO = true;
            options.Limits.MaxRequestBodySize = int.MaxValue;
        });
        services.Configure<FormOptions>(formOptions =>
        {
            formOptions.ValueLengthLimit = int.MaxValue;
            formOptions.MultipartBodyLengthLimit = int.MaxValue;
            formOptions.MultipartHeadersLengthLimit = int.MaxValue;
        });
        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName,
                builder =>
                {
                    builder
                        .AllowAnyOrigin() // Pass "Allowed Hosts" from appsettings.json
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
        });

        services.AddControllers();
        services.AddLazyResolution();
    }

    public static void AddFrameworkServices(IServiceCollection services)
    {
        services
            .AddLogging(loggingBuilder => { loggingBuilder.AddConsole(); })
            .AddMvc(options =>
            {
                options.InputFormatters
                    .RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonInputFormatter>();
                options.OutputFormatters
                    .RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonOutputFormatter>();
                options.InputFormatters.Add(new AasRequestFormatter());
                options.OutputFormatters.Add(new AasResponseFormatter());
                options.InputFormatters.Add(new AasDescriptorRequestFormatter());
                options.OutputFormatters.Add(new AasDescriptorResponseFormatter());
            })
            .AddNewtonsoftJson(opts =>
            {
                opts.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver()
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                    {
                        // Do not change dictionary keys casing
                        ProcessDictionaryKeys = false
                    }
                };
                opts.SerializerSettings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
                opts.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            });

        services
            .AddSwaggerGen(swaggerGenOptions =>
            {
                swaggerGenOptions.SwaggerDoc("Final-Draft", new OpenApiInfo
                {
                    Version = "Final-Draft",
                    Title = "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Repository",
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

                var swaggerCommentedAssembly = typeof(AssetAdministrationShellRepositoryAPIApiController).Assembly
                    .GetName().Name;
                swaggerGenOptions.IncludeXmlComments(
                    $"{AppContext.BaseDirectory}{Path.DirectorySeparatorChar}{swaggerCommentedAssembly}.xml");

                // Include DataAnnotation attributes on Controller Action parameters as Swagger validation rules (e.g required, pattern, ..)
                // Use [ValidateModelState] on Actions to actually validate it in C# as well!

                swaggerGenOptions.OperationFilter<IO.Swagger.Filters.GeneratePathParamsValidationFilter>();
            });
        services.AddAuthentication("AasSecurityAuth")
            .AddScheme<AasSecurityAuthenticationOptions, AasSecurityAuthenticationHandler>("AasSecurityAuth", null);
        services.AddAuthorization(authorizationOptions =>
        {
            authorizationOptions.AddPolicy("SecurityPolicy", policy =>
            {
                policy.AuthenticationSchemes.Add("AasSecurityAuth");
                policy.Requirements.Add(new SecurityRequirement());
            });
        });
    }

    public static void AddGraphQlServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<Query>();
    }

    public static void ConfigureSwagger(IApplicationBuilder app, IConfiguration configuration)
    {
        app.UseSwagger();
        app.UseSwaggerUI(swaggerUiOptions =>
        {
            //TODO: Either use the SwaggerGen generated Swagger contract (generated from C# classes) or
            //alternatively use the original Swagger contract that's included in the static files
            swaggerUiOptions.SwaggerEndpoint("Final-Draft/swagger.json",
                "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Repository");
            swaggerUiOptions.RoutePrefix = "swagger";
            swaggerUiOptions.ConfigObject.AdditionalItems["syntaxHighlight"] = new Dictionary<string, object>
            {
                ["activated"] = false
            };

            var syntaxHighlight = configuration["SyntaxHighlight"];
            swaggerUiOptions.ConfigObject.AdditionalItems["syntaxHighlight"] = new Dictionary<string, object>
            {
                ["activated"] = syntaxHighlight
            };
        });
    }

    public static void ConfigureEnvironment(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseMiddleware<ExceptionMiddleware>();

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();

        app.UseCors(CorsPolicyName);

        app.UseEndpoints(endpoints =>
        {
            // OZ
            endpoints.MapBlazorHub(options =>
            {
                // Do NOT use Websockets
                options.Transports =
                    HttpTransportType.ServerSentEvents |
                    HttpTransportType.LongPolling;
            });
            endpoints.MapFallbackToPage("/_Host");
            endpoints.MapControllers();
            endpoints.MapGraphQL();
        });
    }
}