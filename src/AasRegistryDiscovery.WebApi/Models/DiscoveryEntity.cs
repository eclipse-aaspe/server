namespace AasRegistryDiscovery.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DiscoveryEntity
{
    public string AasIdentifier { get; set; }

    public List<ISpecificAssetId> AssetLinks { get; set; }

    public DiscoveryEntity(string aasIdentifier, List<ISpecificAssetId> assetLinks)
    {
        AasIdentifier = aasIdentifier;
        AssetLinks = assetLinks;
    }
}


