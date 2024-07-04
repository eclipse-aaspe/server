using AasSecurity;
using AasxServerBlazor.Data;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using AasxServerStandardBib.Services;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using IO.Swagger.Lib.V3.Services;
using IO.Swagger.Registry.Lib.V3.Interfaces;
using IO.Swagger.Registry.Lib.V3.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AasxServerBlazor.Configuration;

using IO.Swagger.Models;

public static class DependencyRegistry
{
    public static void Register(IServiceCollection services)
    {
        // NOTE: If you register new classes, keep them sorted alphabetically for better readability!

        services.AddSingleton<AASService>();
        services.AddScoped<BlazorSessionService>();
        services.AddSingleton<CredentialService>();

        services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));
        services.AddSingleton<IAuthorizationHandler, AasSecurityAuthorizationHandler>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IRegistryInitializerService, RegistryInitializerService>();
        services.AddTransient<IAasDescriptorPaginationService, AasDescriptorPaginationService>();
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
        services.AddTransient<ISubmodelService, SubmodelService>();
        services.AddTransient<IValueOnlyJsonDeserializer, ValueOnlyJsonDeserializer>();
    }
}