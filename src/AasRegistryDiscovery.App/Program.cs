using System.Text.Json.Serialization;
using System.Text.Json;
using AasRegistryDiscovery.App.Components;
using AasRegistryDiscovery.App.Configuration;
using AasRegistryDiscovery.WebApi.Controllers;
using AasRegistryDiscovery.WebApi.Filters;
using AasRegistryDiscovery.WebApi.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using AasRegistryDiscovery.WebApi.Formatters;
using Microsoft.AspNetCore.Server.Kestrel.Core;

const string ErrorHandlingPath = "/Error";

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
//builder.Services.AddRazorComponents()
//    .AddInteractiveServerComponents();
ConfigureKestrel(builder.Services);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
DependencyRegistry.Register(builder.Services);
AddMvc(builder.Services);
AddSwaggerGen(builder.Services);



var app = builder.Build();

ConfigureEnvironment(app, app.Environment);
ConfigureSwagger(app, app.Configuration);


app.Run();

static void ConfigureKestrel(IServiceCollection services) =>
        services.Configure<KestrelServerOptions>(options =>
        {
            options.AllowSynchronousIO = true;
            options.Limits.MaxRequestBodySize = int.MaxValue;
        });

static void AddMvc(IServiceCollection services) =>
        services.AddMvc(options =>
        {
            // Remove the default System.Text.Json formatters
            options.InputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonInputFormatter>();
            options.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonOutputFormatter>();

            // Add custom formatters
            options.InputFormatters.Add(new AasDescriptorRequestFormatter());
            options.OutputFormatters.Add(new AasDescriptorResponseFormatter());
        })
                .AddJsonOptions(opts =>
                {
                    // Configure JSON options to use camel case and ignore null values
                    opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    opts.JsonSerializerOptions.DictionaryKeyPolicy = null; // Preserve dictionary key casing
                    opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

                    // Add a converter for enum types to be serialized as camel case strings
                    opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

                    opts.JsonSerializerOptions.MaxDepth = 128; // increase default of 64
                });


void AddSwaggerGen(IServiceCollection services) => services
        .AddSwaggerGen(c =>
        {
            c.SwaggerDoc("V3.0.3_SSP-001", new OpenApiInfo
            {
                Version = "V3.0.3_SSP-001",
                Title = "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Registry Service Specification",
                Description = "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Registry Service Specification (ASP.NET Core 7.0)",
                Contact = new OpenApiContact()
                {
                    Name = "Industrial Digital Twin Association (IDTA)",
                    Url = new Uri("https://github.com/swagger-api/swagger-codegen"),
                    Email = "info@idtwin.org"
                },
                TermsOfService = new Uri("https://github.com/admin-shell-io/aas-specs")
            });
            c.CustomSchemaIds(type => type.FullName.Replace("+", "."));
            var swaggerCommentedAssembly =
                                       typeof(AssetAdministrationShellRegistryAPIApiController).Assembly.GetName().Name;
            c.IncludeXmlComments($"{AppContext.BaseDirectory}{Path.DirectorySeparatorChar}{swaggerCommentedAssembly}.xml");

            // Include DataAnnotation attributes on Controller Action parameters as Swagger validation rules (e.g required, pattern, ..)
            // Use [ValidateModelState] on Actions to actually validate it in C# as well!
            c.OperationFilter<GeneratePathParamsValidationFilter>();
        });

void ConfigureEnvironment(IApplicationBuilder app, IWebHostEnvironment env)
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
    //app.UsePathBase("/api/v3.0");
    app.UseRouting();
    app.UseAuthorization();
    //app.UseCors(CorsPolicyName);

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}

void ConfigureSwagger(WebApplication app, IConfiguration configuration)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        //TODO: Either use the SwaggerGen generated Swagger contract (generated from C# classes)
        c.SwaggerEndpoint("/swagger/V3.0.3_SSP-001/swagger.json", "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Registry Service Specification");

        //TODO: Or alternatively use the original Swagger contract that's included in the static files
        // c.SwaggerEndpoint("/swagger-original.json", "DotAAS Part 2 | HTTP/REST | Asset Administration Shell Registry Service Specification Original");
    });
}
