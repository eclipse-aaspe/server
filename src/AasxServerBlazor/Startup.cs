using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AasxServerBlazor.Data;
using IO.Swagger.V1RC03;
using IO.Swagger.V1RC03.APIModels.ValueOnly;
using IO.Swagger.V1RC03.Controllers;
using IO.Swagger.V1RC03.Filters;
using IO.Swagger.V1RC03.Logging;
using IO.Swagger.V1RC03.Middleware;
using IO.Swagger.V1RC03.Services;
//using IO.Swagger.Controllers;
//using IO.Swagger.Filters;
//using IO.Swagger.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace AasxServerBlazor
{
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

            var corsOrigins = Configuration["CorsOrigins"];
            if (corsOrigins.Equals("*"))
            {
                services.AddCors(options =>
                {
                    options.AddPolicy(_corsPolicyName,
                        builder =>
                        {
                            builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                        });
                });
            }
            else if (corsOrigins.Contains(','))
            {
                string[] allowedOrigins = corsOrigins.Split(',');
                services.AddCors(options =>
                {
                    options.AddPolicy(_corsPolicyName,
                        builder =>
                        {
                            builder
                            .WithOrigins(allowedOrigins)
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                        });
                });
            }
            else
            {
                //case where only one host is defined, hence not comma separated
                services.AddCors(options =>
                {
                    options.AddPolicy(_corsPolicyName,
                        builder =>
                        {
                            builder
                            .WithOrigins(corsOrigins)
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                        });
                });
            }
            services.AddScoped<BlazorSessionService>();
            // services.AddScoped<CredentialService>();
            services.AddSingleton<CredentialService>();

            services.AddControllers();

            services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));
            services.AddTransient<IAssetAdministrationShellEnvironmentService, AssetAdministrationShellEnvironmentService>();
            services.AddTransient<IJsonQueryDeserializer, JsonQueryDeserializer>();
            services.AddTransient<IBase64UrlDecoderService, Base64UrlDecoderService>();
            services.AddTransient<IAasxFileServerInterfaceService, AasxFileServerInterfaceService>();
            services.AddTransient<IOutputModifiersService, OutputModifiersService>();
            services.AddTransient<IInputModifierService, InputModifierService>();
            services.AddTransient<IGenerateSerializationService, GenerateSerializationService>();
            services.AddTransient<IValueOnlyDeserializerService, ValueOnlyDeserializerService>();

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
                    options.InputFormatters.Add(new AasCoreInputFormatter());
                    options.OutputFormatters.Add(new AasCoreOutputFormatter());
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

                    c.SchemaFilter<EnumSchemaFilter>();

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

                    string swaggerCommentedAssembly = typeof(AssetAdministrationShellEnvironmentAPIController).Assembly.GetName().Name;
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
