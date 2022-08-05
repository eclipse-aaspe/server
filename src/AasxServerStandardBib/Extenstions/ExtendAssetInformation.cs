using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
{
    public static class ExtendAssetInformation
    {
        public static AssetInformation ConvertFromV10(this AssetInformation assetInformation, AasxCompatibilityModels.AdminShellV10.Asset sourceAsset)
        {
            //Determine AssetKind
            var assetKind = AssetKind.Instance;
            if (sourceAsset.kind.IsType)
            {
                assetKind = AssetKind.Type;
            }

            assetInformation.AssetKind = assetKind;


            //Assign GlobalAssetId
            var key = new Key(KeyTypes.GlobalReference, sourceAsset.identification.id);
            assetInformation.GlobalAssetId = new Reference(ReferenceTypes.GlobalReference, new List<Key> { key });

            return assetInformation;
        }

        public static AssetInformation ConvertFromV20(this AssetInformation assetInformation, AasxCompatibilityModels.AdminShellV20.Asset sourceAsset)
        {
            //Determine AssetKind
            var assetKind = AssetKind.Instance;
            if (sourceAsset.kind.IsType)
            {
                assetKind = AssetKind.Type;
            }

            assetInformation.AssetKind = assetKind;


            //Assign GlobalAssetId
            var key = new Key(KeyTypes.GlobalReference, sourceAsset.identification.id);
            assetInformation.GlobalAssetId = new Reference(ReferenceTypes.GlobalReference, new List<Key> { key });

            return assetInformation;
        }
    }
}
