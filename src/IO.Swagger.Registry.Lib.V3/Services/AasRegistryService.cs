using AasxServerStandardBib.Logging;
using IO.Swagger.Registry.Lib.V3.Interfaces;
using IO.Swagger.Registry.Lib.V3.Models;
using IO.Swagger.Registry.Lib.V3.Serializers;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using AasxServerDB;
using AasxServerDB.Entities;

namespace IO.Swagger.Registry.Lib.V3.Services;

public class AasRegistryService : IAasRegistryService
{
    private readonly IAppLogger<AasRegistryService> _logger;
    private readonly IRegistryInitializerService _registryInitializerService;

    public AasRegistryService(IAppLogger<AasRegistryService> logger, IRegistryInitializerService registryInitializerService)
    {
        _logger                     = logger;
        _registryInitializerService = registryInitializerService;
    }

    public AssetAdministrationShellDescriptor CreateAasDescriptorFromDB(AASSet aasDB)
    {
        var assetAdministrationShellDescriptor = new AssetAdministrationShellDescriptor();
        var globalAssetId                      = aasDB.GlobalAssetId;

        using AasContext aasContext = new AasContext();
        assetAdministrationShellDescriptor.IdShort = aasDB.IdShort;
        assetAdministrationShellDescriptor.Id      = aasDB.Identifier ?? string.Empty;
        var e = new Models.Endpoint();
        e.ProtocolInformation = new ProtocolInformation();
        e.ProtocolInformation.Href =
            $"{AasxServer.Program.externalRepository}/shells/{Base64UrlEncoder.Encode(assetAdministrationShellDescriptor.Id)}";
        _logger.LogDebug("AAS " + assetAdministrationShellDescriptor.IdShort + " " + e.ProtocolInformation.Href);
        e.Interface                                      = "AAS-1.0";
        assetAdministrationShellDescriptor.Endpoints     = new List<Models.Endpoint> {e};
        assetAdministrationShellDescriptor.GlobalAssetId = globalAssetId;
        //
        assetAdministrationShellDescriptor.SpecificAssetIds = new List<SpecificAssetId>();
        var specificAssetId = new SpecificAssetId("AssetKind", aasDB.AssetKind,
                                                  externalSubjectId: new Reference(ReferenceTypes.ExternalReference,
                                                                                   new List<IKey>() {new Key(KeyTypes.GlobalReference, "assetKind")}));
        assetAdministrationShellDescriptor.SpecificAssetIds.Add(specificAssetId);

        // Submodels
        var submodelDBList = aasContext.SMSets.Where(s => s.AASId == aasDB.Id);
        if (!submodelDBList.Any())
        {
            return assetAdministrationShellDescriptor;
        }

        assetAdministrationShellDescriptor.SubmodelDescriptors = new List<SubmodelDescriptor>();
        foreach (var submodelDB in submodelDBList)
        {
            var submodelDescriptor = new SubmodelDescriptor {IdShort = submodelDB.IdShort, Id = submodelDB.Identifier ?? string.Empty};
            var esm                = new Endpoint();
            esm.ProtocolInformation = new ProtocolInformation
                                      {
                                          Href = AasxServer.Program.externalRepository + "/shells/" +
                                                 Base64UrlEncoder.Encode(assetAdministrationShellDescriptor.Id) + "/submodels/" +
                                                 Base64UrlEncoder.Encode(submodelDescriptor.Id)
                                      };
            esm.Interface                 = "SUBMODEL-1.0";
            submodelDescriptor.Endpoints  = new List<Endpoint> {esm};
            submodelDescriptor.SemanticId = new Reference(ReferenceTypes.ExternalReference, new List<IKey>() {new Key(KeyTypes.GlobalReference, submodelDB.SemanticId)});
            assetAdministrationShellDescriptor.SubmodelDescriptors.Add(submodelDescriptor);
        }

        return assetAdministrationShellDescriptor;
    }

    //getFromAasRegistry from old implementation
    public List<AssetAdministrationShellDescriptor> GetAllAssetAdministrationShellDescriptors(string? assetKind = null, List<string?>? assetList = null,
                                                                                              string? aasIdentifier = null)
    {
        var result = new List<AssetAdministrationShellDescriptor>();

        if (aasIdentifier != null && assetList != null)
            return result;

        // Check for stored combined basyx list for descriptors by getRegistry()
        var aasDescriptors = _registryInitializerService.GetAasDescriptorsForSubmodelView();

        if (aasDescriptors != null && aasDescriptors.Count != 0)
        {
            foreach (var ad in aasDescriptors)
            {
                var found = aasIdentifier == null && assetList.IsNullOrEmpty();
                if (aasIdentifier != null)
                {
                    if (aasIdentifier.Equals(ad.Id))
                    {
                        found = true;
                    }
                }

                if (found)
                {
                    result.Add(ad);
                }
            }

            return result;
        }

        var aasRegistry = _registryInitializerService.GetAasRegistry();
        if (aasRegistry == null)
        {
            return result;
        }

        AssetAdministrationShellDescriptor assetAdministrationShellDescriptor;
        foreach (var sme in aasRegistry.SubmodelElements)
        {
            if (sme is not SubmodelElementCollection smc)
            {
                continue;
            }

            var aasID          = "";
            var assetID        = "";
            var descriptorJSON = "";
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

            var found = aasIdentifier == null && assetList.IsNullOrEmpty();

            if (aasIdentifier != null && aasID != "" && descriptorJSON != "" && aasIdentifier.Equals(aasID))
            {
                found = true;
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

            if (!found)
            {
                continue;
            }

            //ad = JsonConvert.DeserializeObject<AssetAdministrationShellDescriptor>(descriptorJSON);
            if (!string.IsNullOrEmpty(descriptorJSON))
            {
                var node = JsonSerializer.Deserialize<JsonNode>(descriptorJSON);
                assetAdministrationShellDescriptor = DescriptorDeserializer.AssetAdministrationShellDescriptorFrom(node);
            }
            else
            {
                assetAdministrationShellDescriptor = null;
            }

            result.Add(assetAdministrationShellDescriptor);
        }

        return result;
    }
}