namespace AasRegistryDiscovery.App.Configuration;

using AasRegistryDiscovery.WebApi.Interfaces;
using AasRegistryDiscovery.WebApi.Services.Common;
using AasRegistryDiscovery.WebApi.Services.InMemory;

public static class DependencyRegistry
{
    public static void Register(IServiceCollection services)
    {
        services.AddTransient<IPersistenceService, InMemoryPersistenceService>();
        services.AddTransient<IBase64UrlDecoderService, Base64UrlDecoderService>();
        services.AddTransient<IDescriptorPaginationService, DescriptorPaginationService>();
    }
}
