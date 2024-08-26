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

//var builder = WebApplication.CreateBuilder(args);
//var app = builder.Build();

using System.Text.Json;
using System.Text.Json.Serialization;
using AasSecurity;
using AasxServerDB;
using AasxServerStandardBib.Extensions;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using AasxServerStandardBib.Services;
using AdminShellNS;
using IO.Swagger.Controllers;
using IO.Swagger.Lib.V3.Formatters;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.Middleware;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using IO.Swagger.Lib.V3.Services;
using IO.Swagger.Models;
using IO.Swagger.Registry.Lib.V3.Formatters;
using IO.Swagger.Registry.Lib.V3.Interfaces;
using IO.Swagger.Registry.Lib.V3.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.OpenApi.Models;

internal class Startup
{
    IConfigurationRoot Configuration { get; }
    private const string _corsPolicyName = "AllowAll";

    public Startup()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json");

        Configuration = builder.Build();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Configuration);

        services.Configure<KestrelServerOptions>(options =>
                                                 {
                                                     options.AllowSynchronousIO        = true;
                                                     options.Limits.MaxRequestBodySize = int.MaxValue;
                                                 });
        services.Configure<FormOptions>(x =>
                                        {
                                            x.ValueLengthLimit            = int.MaxValue;
                                            x.MultipartBodyLengthLimit    = int.MaxValue;
                                            x.MultipartHeadersLengthLimit = int.MaxValue;
                                        });

        services.AddCors(options =>
                         {
                             options.AddPolicy(_corsPolicyName,
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
        services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));
        services.AddSingleton<IAuthorizationHandler, AasSecurityAuthorizationHandler>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IRegistryInitializerService, RegistryInitializerService>();
        services.AddTransient<IAasDescriptorPaginationService, AasDescriptorPaginationService>();
        services.AddTransient<IAasDescriptorWritingService, AasDescriptorWritingService>();
        services.AddTransient<IAasRegistryService, AasRegistryService>();
        services.AddTransient<IAasRepositoryApiHelperService, AasRepositoryApiHelperService>();
        services.AddTransient<IAasxFileServerInterfaceService, AasxFileServerInterfaceService>();
        services.AddTransient<IAdminShellPackageEnvironmentService, AdminShellPackageEnvironmentService>();
        services.AddTransient<IAssetAdministrationShellService, AssetAdministrationShellService>();
        services.AddTransient<IBase64UrlDecoderService, Base64UrlDecoderService>();
        services.AddTransient<IConceptDescriptionService, ConceptDescriptionService>();
        services.AddTransient<IGenerateSerializationService, GenerateSerializationService>();
        services.AddTransient<IIdShortPathParserService, IdShortPathParserService>();
        services.AddTransient<IJsonQueryDeserializer, JsonQueryDeserializer>();
        services.AddTransient<ILevelExtentModifierService, LevelExtentModifierService>();
        services.AddTransient<IMappingService, MappingService>();
        services.AddTransient<IMetamodelVerificationService, MetamodelVerificationService>();
        services.AddTransient<IPaginationService, PaginationService>();
        services.AddTransient<IPathModifierService, PathModifierService>();
        services.AddTransient<IReferenceModifierService, ReferenceModifierService>();
        services.AddTransient<ISecurityService, SecurityService>();
        services.AddTransient<IServiceDescription, ServiceDescription>();
        services.AddTransient<ISubmodelPropertyExtractionService, SubmodelPropertyExtractionService>();
        services.AddTransient<ISubmodelService, SubmodelService>();
        services.AddTransient<IValueOnlyJsonDeserializer, ValueOnlyJsonDeserializer>();

        // Add GraphQL services
        services
            .AddGraphQLServer()
            .AddQueryType<Query>();

        // Add framework services.
        services
            .AddLogging(config =>
                        {
                            config.AddConsole();
                        })
            .AddMvc(options =>
                    {
                        // Remove the default System.Text.Json formatters
                        options.InputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonInputFormatter>();
                        options.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonOutputFormatter>();

                        // Add your custom formatters
                        options.InputFormatters.Add(new AasRequestFormatter());
                        options.OutputFormatters.Add(new AasResponseFormatter());
                        options.InputFormatters.Add(new AasDescriptorRequestFormatter());
                        options.OutputFormatters.Add(new AasDescriptorResponseFormatter());
                    })
            .AddJsonOptions(opts =>
                            {
                                opts.JsonSerializerOptions.PropertyNamingPolicy   = JsonNamingPolicy.CamelCase;
                                opts.JsonSerializerOptions.DictionaryKeyPolicy    = null; // Preserve dictionary key casing
                                opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

                                // Add custom converters if needed
                                opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

                                // Add other custom converters if required
                                opts.JsonSerializerOptions.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                            });

        services
            .AddSwaggerGen(c =>
                           {
                               c.SwaggerDoc("Final-Draft",
                                            new OpenApiInfo
                                            {
                                                Version     = "Final-Draft",
                                                Title       = "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Repository",
                                                Description = "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Repository (ASP.NET Core 3.1)",
                                                Contact = new OpenApiContact()
                                                          {
                                                              Name  = "Michael Hoffmeister, Torben Miny, Andreas Orzelski, Manuel Sauer, Constantin Ziesche",
                                                              Url   = new Uri("https://github.com/swagger-api/swagger-codegen"),
                                                              Email = ""
                                                          },
                                                TermsOfService = new Uri("https://github.com/admin-shell-io/aas-specs")
                                            });

                               c.EnableAnnotations();
                               c.CustomSchemaIds(type => type.FullName);

                               //string swaggerCommentedAssembly = typeof(AssetAdministrationShellEnvironmentAPIController).Assembly.GetName().Name;
                               var swaggerCommentedAssembly = typeof(AssetAdministrationShellRepositoryAPIApiController).Assembly.GetName().Name;
                               c.IncludeXmlComments($"{AppContext.BaseDirectory}{System.IO.Path.DirectorySeparatorChar}{swaggerCommentedAssembly}.xml");

                               // Include DataAnnotation attributes on Controller Action parameters as Swagger validation rules (e.g required, pattern, ..)
                               // Use [ValidateModelState] on Actions to actually validate it in C# as well!

                               c.OperationFilter<IO.Swagger.Filters.GeneratePathParamsValidationFilter>();
                           });


        services.AddAuthentication("AasSecurityAuth")
                .AddScheme<AasSecurityAuthenticationOptions, AasSecurityAuthenticationHandler>("AasSecurityAuth", null);
        services.AddAuthorization(c =>
                                  {
                                      c.AddPolicy("SecurityPolicy", policy =>
                                                                    {
                                                                        policy.AuthenticationSchemes.Add("AasSecurityAuth");
                                                                        policy.Requirements.Add(new SecurityRequirement());
                                                                    });
                                  });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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

        // app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseRouting();
        //app.UseAuthentication();
        app.UseAuthorization();

        app.UseCors(_corsPolicyName);

        // Enable middleware to serve generated Swagger as a JSON endpoint.
        app.UseSwagger();
        // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
        // specifying the Swagger JSON endpoint.
        app.UseSwaggerUI(c =>
                         {
                             c.SwaggerEndpoint("Final-Draft/swagger.json", "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Repository");
                         });

        app.UseEndpoints(endpoints =>
                         {
                             endpoints.MapControllers();
                         });
    }
}