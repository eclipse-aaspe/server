using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text;
using AdminShellNS;

namespace IO.Swagger.Lib.Models
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public partial class AssetAdministrationShellAndAsset
    {
        /// <summary>
        /// Gets or sets AssetAdministrationShell
        /// </summary>
        [DataMember(Name = "aas")]
        public AdminShell.AdministrationShell aas;

        /// <summary>
        /// Gets or sets Asset
        /// </summary>
        [DataMember(Name = "asset")]
        public AdminShell.Asset asset;
        /// <summary>
        /// Gets or sets Asset
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
    }
}
