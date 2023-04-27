
using AasxServerStandardBib.Logging;
using System.Collections.Generic;
using System.Linq;

namespace IO.Swagger.V1RC03.Services
{
    public class GenerateSerializationService : IGenerateSerializationService
    {
        private readonly IAppLogger<GenerateSerializationService> _logger;
        private readonly IAssetAdministrationShellEnvironmentService _aasEnvService;

        public GenerateSerializationService(IAppLogger<GenerateSerializationService> logger, IAssetAdministrationShellEnvironmentService aasEnvSerive)
        {
            _logger = logger;
            _aasEnvService = aasEnvSerive;
        }

        public Environment GenerateSerializationByIds(List<string> aasIds, List<string> submodelIds, bool includeConceptDescriptions)
        {
            var outputEnv = new Environment();
            outputEnv.AssetAdministrationShells = new List<AssetAdministrationShell>();
            outputEnv.Submodels = new List<Submodel>();

            //Fetch AASs for the requested aasIds
            var aasList = _aasEnvService.GetAllAssetAdministrationShells();
            foreach (var aasId in aasIds)
            {
                var foundAas = aasList.Where(a => a.Id.Equals(aasId));
                if (foundAas.Any())
                {
                    outputEnv.AssetAdministrationShells.Add(foundAas.First());
                }
            }

            //Fetch Submodels for the requested submodelIds
            var submodelList = _aasEnvService.GetAllSubmodels();
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
