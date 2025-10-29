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

using AasxServer;
using AasxServerDB;
using AasxServerStandardBib;
using Contracts;
using Contracts.DbRequests;
using Contracts.LevelExtent;
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
        services.AddTransient<IAasDescriptorWritingService, AasDescriptorWritingService>();
        services.AddTransient<IAasRegistryService, AasRegistryService>();
        services.AddTransient<IAasRepositoryApiHelperService, AasRepositoryApiHelperService>();
        services.AddTransient<IBase64UrlDecoderService, Base64UrlDecoderService>();
        services.AddTransient<IIdShortPathParserService, IdShortPathParserService>();
        services.AddTransient<IJsonQueryDeserializer, JsonQueryDeserializer>();
        services.AddTransient<ILevelExtentModifierService, LevelExtentModifierService>();
        services.AddTransient<IMappingService, MappingService>();
        services.AddTransient<IMetamodelVerificationService, MetamodelVerificationService>();
        services.AddTransient<IPaginationService, PaginationService>();
        services.AddTransient<IPathModifierService, PathModifierService>();
        services.AddTransient<IReferenceModifierService, ReferenceModifierService>();
        services.AddTransient<IServiceDescription, ServiceDescription>();
        services.AddTransient<ISubmodelPropertyExtractionService, SubmodelPropertyExtractionService>();
        services.AddTransient<IValueOnlyJsonDeserializer, ValueOnlyJsonDeserializer>();
        services.AddTransient<IValidateSerializationModifierService, ValidateSerializationModifierService>();
        services.AddTransient<IPersistenceService, EntityFrameworkPersistenceService>();
        services.AddSingleton<IDbRequestHandlerService, DbRequestHandlerService>();
        services.AddSingleton<IEventService, EventService>();

        /*
        services.AddSingleton<ISecurityService, SecurityService>();

        //ToDo: Should this be transient?
        services.AddSingleton<IContractSecurityRules, SecurityService>();
        */

        services.AddSingleton<SecurityService>();
        services.AddSingleton<ISecurityService>(provider => provider.GetRequiredService<SecurityService>());
        services.AddSingleton<IContractSecurityRules>(provider => provider.GetRequiredService<SecurityService>());

        services.AddTransient<QueryGrammarJSON>();

        services.AddSingleton<MqttClientService>();
        services.AddSingleton<AasxTaskService>();
    }
}