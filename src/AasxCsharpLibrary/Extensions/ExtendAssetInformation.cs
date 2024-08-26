/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

using System;

namespace Extensions
{
    public static class ExtendAssetInformation
    {
        #region AasxPackageExplorer

        public static Tuple<string, string> ToCaptionInfo(this AssetInformation assetInformation)
        {
            // TODO (jtikekar, 2023-09-04): support KeyType.AssetInformation
            //var caption = Key.AssetInformation;
            var caption = "AssetInformation";
            var info = "" + assetInformation.GlobalAssetId;
            return Tuple.Create(caption, info);
        }

        #endregion
        public static AssetInformation? ConvertFromV10(this AssetInformation? assetInformation, AasxCompatibilityModels.AdminShellV10.Asset sourceAsset)
        {
            //Determine AssetKind
            var assetKind = AssetKind.Instance;
            if (sourceAsset.kind.IsType)
            {
                assetKind = AssetKind.Type;
            }

            assetInformation.AssetKind = assetKind;


            //Assign GlobalAssetId
            assetInformation.GlobalAssetId = sourceAsset.identification.id;

            return assetInformation;
        }

        public static AssetInformation? ConvertFromV20(this AssetInformation? assetInformation, AasxCompatibilityModels.AdminShellV20.Asset sourceAsset)
        {
            //Determine AssetKind
            var assetKind = AssetKind.Instance;
            if (sourceAsset.kind.IsType)
            {
                assetKind = AssetKind.Type;
            }

            assetInformation.AssetKind = assetKind;

            //Assign GlobalAssetId
            assetInformation.GlobalAssetId = sourceAsset.identification.id;

            return assetInformation;
        }
    }
}
