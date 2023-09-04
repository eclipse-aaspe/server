using AasxServerStandardBib.Logging;
using IO.Swagger.Registry.Lib.V3.Interfaces;
using IO.Swagger.Registry.Lib.V3.Models;
using IO.Swagger.Registry.Lib.V3.Serializers;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IO.Swagger.Registry.Lib.V3.Services
{
    public class AasRegistryService : IAasRegistryService
    {
        private readonly IAppLogger<AasRegistryService> _logger;
        private readonly IRegistryInitializerService _registryInitializerService;

        public AasRegistryService(IAppLogger<AasRegistryService> logger, IRegistryInitializerService registryInitializerService)
        {
            _logger = logger;
            _registryInitializerService = registryInitializerService;
        }

        //getFromAasRegistry from old implementation
        public List<AssetAdministrationShellDescriptor> GetAllAssetAdministrationShellDescriptors(string assetKind = null, List<string> assetList = null, string aasIdentifier = null)
        {
            List<AssetAdministrationShellDescriptor> result = new List<AssetAdministrationShellDescriptor>();

            if (aasIdentifier != null && assetList != null)
                return result;

            var aasRegistry = _registryInitializerService.GetAasRegistry();
            if (aasRegistry != null)
            {
                AssetAdministrationShellDescriptor ad = null;
                foreach (var sme in aasRegistry.SubmodelElements)
                {
                    if (sme is SubmodelElementCollection smc)
                    {
                        string aasID = "";
                        string assetID = "";
                        string descriptorJSON = "";
                        foreach (var sme2 in smc.Value)
                        {
                            if (sme2 is Property p)
                            {
                                switch (p.IdShort)
                                {
                                    case "aasID":
                                        aasID = p.Value;
                                        break;
                                    case "assetID":
                                        assetID = p.Value;
                                        break;
                                    case "descriptorJSON":
                                        descriptorJSON = p.Value;
                                        break;
                                }

                            }
                        }
                        bool found = false;
                        if (aasIdentifier == null && assetList.IsNullOrEmpty())
                            found = true;
                        if (aasIdentifier != null)
                        {
                            if (aasID != "" && descriptorJSON != "")
                            {
                                if (aasIdentifier.Equals(aasID))
                                {
                                    found = true;
                                }
                            }
                        }
                        if (!assetList.IsNullOrEmpty())
                        {
                            if (assetID != "" && descriptorJSON != "")
                            {
                                if (assetList.Contains(assetID))
                                {
                                    found = true;
                                }
                            }
                        }
                        if (found)
                        {
                            //ad = JsonConvert.DeserializeObject<AssetAdministrationShellDescriptor>(descriptorJSON);
                            if (!string.IsNullOrEmpty(descriptorJSON))
                            {
                                JsonNode node = JsonSerializer.Deserialize<JsonNode>(descriptorJSON);
                                ad = DescriptorDeserializer.AssetAdministrationShellDescriptorFrom(node);
                            }
                            else
                            {
                                ad = null;
                            }
                            result.Add(ad);
                        }
                    }
                }
            }
            return result;
        }
    }
}
