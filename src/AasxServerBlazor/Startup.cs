using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AasxServerBlazor.Data;
using IO.Swagger.Controllers;
using IO.Swagger.Filters;
using IO.Swagger.Helpers;
using IO.Swagger.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace AasxServerBlazor
{
    public class Startup
    {
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
            services.AddCors();
            services.AddScoped<BlazorSessionService>();

            services.AddControllers();

            services.AddTransient<IAASXFileServerInterfaceService, AASXFileServerInterfaceService>();

            // Add framework services.
            services
                .AddMvc(options =>
                {
                    options.InputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonInputFormatter>();
                    options.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonOutputFormatter>();
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
                })
                .AddXmlSerializerFormatters();

            //// configure DI for application services
            //services.AddScoped<IUserService, UserService>();
            //// configure basic authentication 
            //services.AddAuthentication("BasicAuthentication")
            //    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null); services.AddAuthentication();


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
                    string swaggerCommentedAssembly = typeof(AssetAdministrationShellRepositoryApiController).Assembly.GetName().Name;
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

            // app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();
            //app.UseAuthentication();
            //app.UseAuthorization();

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

            app.UseCors(x => x
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(origin => true) // allow any origin
            .AllowCredentials());
        }
    }
}
