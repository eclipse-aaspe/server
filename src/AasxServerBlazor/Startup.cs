using AasxServerBlazor.Data;
using AasxServerStandardBib.Extensions;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using AasxServerStandardBib.Services;
using IO.Swagger.Controllers;
using IO.Swagger.Filters;
using IO.Swagger.Lib.V3.Formatters;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.Middleware;
using IO.Swagger.Lib.V3.Services;
//using IO.Swagger.Controllers;
//using IO.Swagger.Filters;
//using IO.Swagger.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

namespace AasxServerBlazor
{
    //TODO:jtikekar change to startup instead of startupV3
    public class Startup
    {
        private const string _corsPolicyName = "AllowAll";
        private readonly IWebHostEnvironment _hostingEnv;

        /*
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        */
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            _hostingEnv = env;
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<AASService>();
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
            services.AddScoped<BlazorSessionService>();
            // services.AddScoped<CredentialService>();
            services.AddSingleton<CredentialService>();

            services.AddControllers();

            services.AddLazyResolution();

            services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));
            services.AddTransient<IAssetAdministrationShellService, AssetAdministrationShellService>();
            services.AddTransient<IAdminShellPackageEnvironmentService, AdminShellPackageEnvironmentService>();
            services.AddTransient<ISubmodelService, SubmodelService>();
            services.AddTransient<IConceptDescriptionService, ConceptDescriptionService>();
            services.AddTransient<IBase64UrlDecoderService, Base64UrlDecoderService>();
            services.AddTransient<IPaginationService, PaginationService>();
            services.AddTransient<IAasRepositoryApiHelperService, AasRepositoryApiHelperService>();
            services.AddTransient<IMetamodelVerificationService, MetamodelVerificationService>();
            services.AddTransient<IJsonQueryDeserializer, JsonQueryDeserializer>();
            services.AddTransient<IReferenceModifierService, ReferenceModifierService>();
            //TODO:jtikekar uncomment
            //services.AddTransient<IAssetAdministrationShellEnvironmentService, AssetAdministrationShellEnvironmentService>();
            //services.AddTransient<IJsonQueryDeserializer, JsonQueryDeserializer>();

            //services.AddTransient<IAasxFileServerInterfaceService, AasxFileServerInterfaceService>();
            //services.AddTransient<IOutputModifiersService, OutputModifiersService>();
            //services.AddTransient<IInputModifierService, InputModifierService>();
            //services.AddTransient<IGenerateSerializationService, GenerateSerializationService>();
            //services.AddTransient<IValueOnlyDeserializerService, ValueOnlyDeserializerService>();

            // Add framework services.
            services
                .AddLogging(config =>
                {
                    config.AddConsole();
                })
                .AddMvc(options =>
                {
                    options.InputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonInputFormatter>();
                    options.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonOutputFormatter>();
                    options.InputFormatters.Add(new AasRequestFormatter());
                    options.OutputFormatters.Add(new AasResponseFormatter());
                    //TODO:jtikekar uncomment
                    //options.InputFormatters.Add(new AasCoreInputFormatter());
                    //options.OutputFormatters.Add(new AasCoreOutputFormatter());
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
                });
            //TODO:jtikekar Uncomment
            //.AddXmlSerializerFormatters();

            services
                .AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("Final-Draft", new OpenApiInfo
                    {
                        Version = "Final-Draft",
                        Title = "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Repository",
                        Description = "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Repository (ASP.NET Core 3.1)",
                        Contact = new OpenApiContact()
                        {
                            Name = "Michael Hoffmeister, Torben Miny, Andreas Orzelski, Manuel Sauer, Constantin Ziesche",
                            Url = new Uri("https://github.com/swagger-api/swagger-codegen"),
                            Email = ""
                        },
                        TermsOfService = new Uri("https://github.com/admin-shell-io/aas-specs")
                    });

                    //TODO:jtikekar uncomment
                    //c.SchemaFilter<EnumSchemaFilter>();

                    //c.AddSecurityDefinition("basic", new OpenApiSecurityScheme
                    //{
                    //    Name = "Authorization",
                    //    Type = SecuritySchemeType.Http,
                    //    Scheme = "basic",
                    //    In = ParameterLocation.Header,
                    //    Description = "Basic Authorization header using the Bearer scheme."
                    //});

                    //c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    //{
                    //    {
                    //          new OpenApiSecurityScheme
                    //            {
                    //                Reference = new OpenApiReference
                    //                {
                    //                    Type = ReferenceType.SecurityScheme,
                    //                    Id = "basic"
                    //                }
                    //            },
                    //            new string[] {}
                    //    }
                    //});

                    c.EnableAnnotations();
                    c.CustomSchemaIds(type => type.FullName);

                    //string swaggerCommentedAssembly = typeof(AssetAdministrationShellEnvironmentAPIController).Assembly.GetName().Name;
                    string swaggerCommentedAssembly = typeof(AssetAdministrationShellRepositoryAPIApiController).Assembly.GetName().Name;
                    c.IncludeXmlComments($"{AppContext.BaseDirectory}{Path.DirectorySeparatorChar}{swaggerCommentedAssembly}.xml");

                    // Include DataAnnotation attributes on Controller Action parameters as Swagger validation rules (e.g required, pattern, ..)
                    // Use [ValidateModelState] on Actions to actually validate it in C# as well!

                    c.OperationFilter<GeneratePathParamsValidationFilter>();
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
            //app.UseAuthorization();

            app.UseCors(_corsPolicyName);

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                //TODO: Either use the SwaggerGen generated Swagger contract (generated from C# classes)
                c.SwaggerEndpoint("Final-Draft/swagger.json", "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Repository");
                c.RoutePrefix = "swagger";
                c.ConfigObject.AdditionalItems["syntaxHighlight"] = new Dictionary<string, object>
                {
                    ["activated"] = false
                };

                var syntaxHighlight = Configuration["SyntaxHighlight"];
                c.ConfigObject.AdditionalItems["syntaxHighlight"] = new Dictionary<string, object>
                {
                    ["activated"] = syntaxHighlight
                };

                //TODO: Or alternatively use the original Swagger contract that's included in the static files
                // c.SwaggerEndpoint("swagger-original.json", "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Repository Original");
            });

            app.UseEndpoints(endpoints =>
            {
                // OZ
                endpoints.MapBlazorHub(options =>
                {
                    // NO Websockets
                    options.Transports =
                        // HttpTransportType.WebSockets |
                        HttpTransportType.ServerSentEvents |
                        HttpTransportType.LongPolling;
                });
                endpoints.MapFallbackToPage("/_Host");
                endpoints.MapControllers();
            });

        }
    }
}
