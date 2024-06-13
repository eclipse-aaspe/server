using IO.Swagger.Registry.Lib.V3.Models;
using System.Collections.Generic;
using AasxServerDB.Entities;

namespace IO.Swagger.Registry.Lib.V3.Interfaces
{
    public interface IAasRegistryService
    {
        AssetAdministrationShellDescriptor CreateAasDescriptorFromDB(AASSet aasDB);
        List<AssetAdministrationShellDescriptor> GetAllAssetAdministrationShellDescriptors(string assetKind = null, List<string> assetList = null, string aasIdentifier = null);
    }
}
