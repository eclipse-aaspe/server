using AasxServer;
using IO.Swagger.Registry.Lib.V3.Models;
using System.Collections.Generic;

namespace IO.Swagger.Registry.Lib.V3.Interfaces
{
    public interface IAasRegistryService
    {
        AssetAdministrationShellDescriptor CreateAasDescriptorFromDB(AasSet aasDB);
        List<AssetAdministrationShellDescriptor> GetAllAssetAdministrationShellDescriptors(string assetKind = null, List<string> assetList = null, string aasIdentifier = null);
    }
}
