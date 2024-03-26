using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AasxServerBlazor.Configuration;

namespace AasxServerBlazor
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            ServerConfiguration.ConfigureServer(services);

            DependencyRegistry.Register(services);

            ServerConfiguration.AddGraphQlServices(services);

            ServerConfiguration.AddFrameworkServices(services);
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            ServerConfiguration.ConfigureEnvironment(app, env);

            ServerConfiguration.ConfigureSwagger(app, Configuration);
        }
    }
}