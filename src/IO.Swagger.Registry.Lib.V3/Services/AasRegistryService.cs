using AasxServer;
using AasxServerStandardBib.Logging;
using IO.Swagger.Registry.Lib.V3.Interfaces;
using IO.Swagger.Registry.Lib.V3.Models;
using IO.Swagger.Registry.Lib.V3.Serializers;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using static AasxServer.Program;

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

        public AssetAdministrationShellDescriptor CreateAasDescriptorFromDB(AasSet aasDB)
        {
            AssetAdministrationShellDescriptor ad = new AssetAdministrationShellDescriptor();
            //string asset = aas.assetRef?[0].Value;
            string globalAssetId = aasDB.AssetId;

            using (AasContext db = new AasContext())
            {
                // ad.Administration.Version = aas.administration.version;
                // ad.Administration.Revision = aas.administration.revision;
                ad.IdShort = aasDB.Idshort;
                ad.Id = aasDB.AasId;
                var e = new Models.Endpoint();
                e.ProtocolInformation = new ProtocolInformation();
                e.ProtocolInformation.Href =
                    AasxServer.Program.externalBlazor + "/shells/" +
                    Base64UrlEncoder.Encode(ad.Id);
                _logger.LogDebug("AAS " + ad.IdShort + " " + e.ProtocolInformation.Href);
                e.Interface = "AAS-1.0";
                ad.Endpoints = new List<Models.Endpoint>
                {
                    e
                };
                ad.GlobalAssetId = globalAssetId;
                //
                ad.SpecificAssetIds = new List<SpecificAssetId>();
                var specificAssetId = new SpecificAssetId("AssetKind", aasDB.AssetKind, externalSubjectId: new Reference(ReferenceTypes.ExternalReference, new List<IKey>() { new Key(KeyTypes.GlobalReference, "assetKind") }));
                ad.SpecificAssetIds.Add(specificAssetId);

                // Submodels
                var submodelDBList = db.SubmodelSets.Where(s => s.AasNum == aasDB.AasNum);
                if (submodelDBList.Any())
                {
                    ad.SubmodelDescriptors = new List<SubmodelDescriptor>();
                    foreach (var submodelDB in submodelDBList)
                    {
                        SubmodelDescriptor sd = new SubmodelDescriptor();
                        sd.IdShort = submodelDB.Idshort;
                        sd.Id = submodelDB.SubmodelId;
                        var esm = new Models.Endpoint();
                        esm.ProtocolInformation = new ProtocolInformation();
                        esm.ProtocolInformation.Href =
                            AasxServer.Program.externalBlazor + "/shells/" +
                            Base64UrlEncoder.Encode(ad.Id) + "/submodels/" +
                            Base64UrlEncoder.Encode(sd.Id);
                        // Base64UrlEncoder.Encode(sd.Identification) + "/submodel/";
                        // Console.WriteLine("SM " + sd.IdShort + " " + esm.ProtocolInformation.EndpointAddress);
                        esm.Interface = "SUBMODEL-1.0";
                        sd.Endpoints = new List<Models.Endpoint>
                        {
                            esm
                        };
                        sd.SemanticId = new Reference(ReferenceTypes.ExternalReference, new List<IKey>() { new Key(KeyTypes.GlobalReference, submodelDB.SemanticId) });
                        ad.SubmodelDescriptors.Add(sd);
                    }
                }
            }

            return ad;
        }

        //getFromAasRegistry from old implementation
        public List<AssetAdministrationShellDescriptor> GetAllAssetAdministrationShellDescriptors(string assetKind = null, List<string> assetList = null, string aasIdentifier = null)
        {
            List<AssetAdministrationShellDescriptor> result = new List<AssetAdministrationShellDescriptor>();

            if (aasIdentifier != null && assetList != null)
                return result;

            // Check for stored combined basyx list for descriptors by getRegistry()
            var aasDescriptors = _registryInitializerService.GetAasDescriptorsForSubmodelView();

            if (aasDescriptors != null && aasDescriptors.Count != 0)
            {
                foreach (var ad in aasDescriptors)
                {
                    bool found = false;
                    if (aasIdentifier == null && assetList.IsNullOrEmpty())
                        found = true;
                    if (aasIdentifier != null)
                    {
                        if (aasIdentifier.Equals(ad.Id))
                        {
                            found = true;
                        }
                    }
                    if (found)
                        result.Add(ad);
                }

                return result;
            }

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
