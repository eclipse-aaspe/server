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

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateSerializationService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for logging activities.</param>
    /// <param name="aasService">Service for accessing Asset Administration Shells.</param>
    /// <param name="submodelService">Service for accessing Submodels.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the required services (logger, aasService, submodelService) are null.
    /// </exception>
    public GenerateSerializationService(IAppLogger<GenerateSerializationService> logger, IAssetAdministrationShellService aasService, ISubmodelService submodelService)
    {
        _logger          = logger ?? throw new ArgumentNullException(nameof(logger));
        _aasService      = aasService ?? throw new ArgumentNullException(nameof(aasService));
        _submodelService = submodelService ?? throw new ArgumentNullException(nameof(submodelService));
    }

    /// <inheritdoc />
    public Environment GenerateSerializationByIds(List<string?>? aasIds = null, List<string?>? submodelIds = null)
    {
        var outputEnv = new Environment {AssetAdministrationShells = new List<IAssetAdministrationShell>(), Submodels = new List<ISubmodel>()};

        //Fetch AASs for the requested aasIds
        var aasList = _aasService.GetAllAssetAdministrationShells();
        //Using is null or empty, as the query parameter in controll currently receives empty list (not null, but count = 0)
        if (!aasIds.IsNullOrEmpty())
        {
            foreach (var foundAas in aasIds.Select(aasId => aasList.Where(a => a.Id != null && a.Id.Equals(aasId, StringComparison.Ordinal))).Where(foundAas => foundAas.Any()))
            {
                outputEnv.AssetAdministrationShells.Add(foundAas.First());
            }
        }
        else
        {
            outputEnv.AssetAdministrationShells.AddRange(aasList);
        }

        //Fetch Submodels for the requested submodelIds
        var submodelList = _submodelService.GetAllSubmodels();
        //Using is null or empty, as the query parameter in controll currently receives empty list (not null, but count = 0)
        if (submodelIds.IsNullOrEmpty())
        {
            outputEnv.Submodels.AddRange(submodelList);
            return outputEnv;
        }

        foreach (var foundSubmodel in submodelIds.Select(submodelId => submodelList.Where(s => s.Id != null && s.Id.Equals(submodelId, StringComparison.Ordinal)))
                                                 .Where(foundSubmodel => foundSubmodel.Any()))
        {
            outputEnv.Submodels.Add(foundSubmodel.First());
        }

        return outputEnv;
    }
}