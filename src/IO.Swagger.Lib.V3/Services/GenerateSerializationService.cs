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

using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using AdminShellNS.Extensions;
using IO.Swagger.Lib.V3.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IO.Swagger.Lib.V3.Services;

using Environment = AasCore.Aas3_0.Environment;

/// <inheritdoc />
public class GenerateSerializationService : IGenerateSerializationService
{
    private readonly IAppLogger<GenerateSerializationService> _logger;
    private readonly IAssetAdministrationShellService _aasService;
    private readonly ISubmodelService _submodelService;
    private readonly IConceptDescriptionService _cdService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateSerializationService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for logging activities.</param>
    /// <param name="aasService">Service for accessing Asset Administration Shells.</param>
    /// <param name="submodelService">Service for accessing Submodels.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the required services (logger, aasService, submodelService) are null.
    /// </exception>
    public GenerateSerializationService(IAppLogger<GenerateSerializationService> logger, IAssetAdministrationShellService aasService, ISubmodelService submodelService, IConceptDescriptionService cdService)
    {
        _logger          = logger ?? throw new ArgumentNullException(nameof(logger));
        _aasService      = aasService ?? throw new ArgumentNullException(nameof(aasService));
        _submodelService = submodelService ?? throw new ArgumentNullException(nameof(submodelService));
        _cdService = cdService ?? throw new ArgumentNullException(nameof(cdService));
    }

    /// <inheritdoc />
    public Environment GenerateSerializationByIds(List<string?>? aasIds = null, List<string?>? submodelIds = null, bool? includeCD = false)
    {
        List<IAssetAdministrationShell> aas = null;
        List<ISubmodel> submodels = null;
        List<IConceptDescription> conceptDescriptions = null;
        var outputEnv = new Environment(aas, submodels, conceptDescriptions);

        //Fetch AASs for the requested aasIds
        var aasList = _aasService.GetAllAssetAdministrationShells();
        //Using is null or empty, as the query parameter in controll currently receives empty list (not null, but count = 0)
        if (!aasIds.IsNullOrEmpty())
        {
            foreach (var foundAas in aasIds.Select(aasId => aasList.Where(a => a.Id != null && a.Id.Equals(aasId, StringComparison.Ordinal))).Where(foundAas => foundAas.Any()))
            {
                outputEnv.AssetAdministrationShells ??= new List<IAssetAdministrationShell>();
                outputEnv.AssetAdministrationShells.Add(foundAas.First());
            }
        }
        else
        {
            outputEnv.AssetAdministrationShells ??= new List<IAssetAdministrationShell>();
            outputEnv.AssetAdministrationShells.AddRange(aasList);
        }

        //Fetch Submodels for the requested submodelIds
        var submodelList = _submodelService.GetAllSubmodels();
        //Using is null or empty, as the query parameter in controll currently receives empty list (not null, but count = 0)
        if (!submodelIds.IsNullOrEmpty())
        {
            foreach (var foundSubmodel in submodelIds.Select(submodelId => submodelList.Where(s => s.Id != null && s.Id.Equals(submodelId, StringComparison.Ordinal)))
                                                 .Where(foundSubmodel => foundSubmodel.Any()))
            {
                outputEnv.Submodels ??= new List<ISubmodel>();
                outputEnv.Submodels.Add(foundSubmodel.First());
            }
        }
        else
        {
            outputEnv.Submodels ??= new List<ISubmodel>();
            outputEnv.Submodels.AddRange(submodelList);
        }

        

        if((bool)includeCD)
        {
            outputEnv.ConceptDescriptions ??= new List<IConceptDescription>();
            outputEnv.ConceptDescriptions.AddRange(_cdService.GetAllConceptDescriptions());
        }

        return outputEnv;
    }
}