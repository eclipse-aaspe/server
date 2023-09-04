using IO.Swagger.Registry.Lib.V3.Models;
using System.Collections.Generic;

namespace IO.Swagger.Registry.Lib.V3.Interfaces
{
    public interface IAasRegistryService
    {
        List<AssetAdministrationShellDescriptor> GetAllAssetAdministrationShellDescriptors(string assetKind = null, List<string> assetList = null, string aasIdentifier = null);
    }
}
