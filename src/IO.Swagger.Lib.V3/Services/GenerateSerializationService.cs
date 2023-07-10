using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using IO.Swagger.Lib.V3.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IO.Swagger.Lib.V3.Services
{
    public class GenerateSerializationService : IGenerateSerializationService
    {
        private readonly IAppLogger<GenerateSerializationService> _logger;
        private readonly IAssetAdministrationShellService _aasService;
        private readonly ISubmodelService _submodelService;

        public GenerateSerializationService(IAppLogger<GenerateSerializationService> logger, IAssetAdministrationShellService aasService, ISubmodelService submodelService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aasService = aasService ?? throw new ArgumentNullException(nameof(aasService));
            _submodelService = submodelService ?? throw new ArgumentNullException(nameof(submodelService));
        }

        public AasCore.Aas3_0.Environment GenerateSerializationByIds(List<string> aasIds = null, List<string> submodelIds = null, bool includeConceptDescriptions = false)
        {
            var outputEnv = new AasCore.Aas3_0.Environment();
            outputEnv.AssetAdministrationShells = new List<IAssetAdministrationShell>();
            outputEnv.Submodels = new List<ISubmodel>();

            //Fetch AASs for the requested aasIds
            var aasList = _aasService.GetAllAssetAdministrationShells();
            foreach (var aasId in aasIds)
            {
                var foundAas = aasList.Where(a => a.Id.Equals(aasId));
                if (foundAas.Any())
                {
                    outputEnv.AssetAdministrationShells.Add(foundAas.First());
                }
            }

            //Fetch Submodels for the requested submodelIds
            var submodelList = _submodelService.GetAllSubmodels();
            foreach (var submodelId in submodelIds)
            {
                var foundSubmodel = submodelList.Where(s => s.Id.Equals(submodelId));
                if (foundSubmodel.Any())
                {
                    outputEnv.Submodels.Add(foundSubmodel.First());
                }
            }

            return outputEnv;
        }
    }
}
