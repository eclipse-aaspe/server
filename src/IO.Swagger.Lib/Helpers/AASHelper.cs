using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using AdminShellNS;
using IO.Swagger.Extensions;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using static AdminShellNS.AdminShellV20;

namespace IO.Swagger.Helpers
{
    /// <summary>
    /// A helper class to suffice all the AAS related queries.
    /// </summary>
    public class AASHelper
    {
        private static AdminShellPackageEnv[] Packages;
        private Timer m_simulationTimer;

        /// <summary>
        /// Constructor
        /// </summary>
        public AASHelper()
        {
            Packages = AasxServer.Program.env;
        }

        internal Submodel FindSubmodelWithinAAS(string aasIdentifier, string submodelIdentifier)
        {
            FindAasReturn aasReturn = FindAas(aasIdentifier);

            if (aasReturn.AAS == null)
                return null;

            foreach (var submodelRef in aasReturn.AAS.submodelRefs)
            {
                var submodel = Packages[aasReturn.IPackage].AasEnv.FindSubmodel(submodelRef);
                if (submodel != null && submodel.identification.id != null && submodel.identification.id.Trim().ToLower() == submodelIdentifier.Trim().ToLower())
                    return submodel;
            }

            // no
            return null;
        }

        /// <summary>
        /// Return a specific AAS from the server
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id</param>
        /// <returns></returns>
        public FindAasReturn FindAas(string aasIdentifier)
        {
            FindAasReturn findAasReturn = new FindAasReturn();

            if (Packages == null)
                return null;

            if (Regex.IsMatch(aasIdentifier, @"^\d+$")) // only number, i.e. index
            {
                // Index
                int i = Convert.ToInt32(aasIdentifier);

                if (i > Packages.Length)
                    return null;

                if (Packages[i] == null || Packages[i].AasEnv == null || Packages[i].AasEnv.AdministrationShells == null
                    || Packages[i].AasEnv.AdministrationShells.Count < 1)
                    return null;

                findAasReturn.AAS = Packages[i].AasEnv.AdministrationShells[0];
                findAasReturn.IPackage = i;
            }
            else
            {
                // Name
                if (aasIdentifier == "id")
                {
                    findAasReturn.AAS = Packages[0].AasEnv.AdministrationShells[0];
                    findAasReturn.IPackage = 0;
                }
                else
                {
                    for (int i = 0; i < Packages.Length; i++)
                    {
                        if (Packages[i] != null)
                        {
                            if (Packages[i].AasEnv.AdministrationShells[0].identification.id == aasIdentifier)
                            {
                                findAasReturn.AAS = Packages[i].AasEnv.AdministrationShells[0];
                                findAasReturn.IPackage = i;
                                break;
                            }
                        }
                    }
                }
            }

            return findAasReturn;
        }

        /// <summary>
        /// Deletes a concept description
        /// </summary>
        /// <param name="conceptDescription">Concept Description Object</param>
        /// <param name="packageIndex">Package Index of the CD from the server</param>
        /// <returns></returns>
        internal bool DeleteConceptDescription(ConceptDescription conceptDescription, int packageIndex)
        {
            if (conceptDescription != null)
            {
                Packages[packageIndex].AasEnv.ConceptDescriptions.Remove(conceptDescription);
                return true;
            }

            return false;
        }


        internal ConceptDescription FindConceptDescription(string cdIdentifier, out int packageIndex)
        {
            if (Packages != null)
            {
                for (int i = 0; i < Packages.Length; i++)
                {
                    var env = Packages[i];
                    if (env != null)
                    {
                        foreach (var cd in env.AasEnv.ConceptDescriptions)
                        {
                            if (cd.identification.id != null && cd.identification.id.Trim().ToLower() == cdIdentifier.Trim().ToLower())
                            {
                                packageIndex = i;
                                return cd;
                            }
                        }
                    }
                }
            }

            packageIndex = -1;
            return null;
        }

        //One of the open Questions
        //TODO: Check if asset needs to be deleted? an asset can be referenced in many shells.
        internal bool DeleteAASAndAsset(string aasIdentifier)
        {
            bool success = false;
            // find the AAS
            var aasReturn = FindAas(aasIdentifier);
            if (aasReturn.AAS != null)
            {
                // find the asset
                var asset = FindAssetwithReference(aasReturn.AAS.assetRef);
                //Deleting AAS from the server
                Packages[aasReturn.IPackage].AasEnv.AdministrationShells.Remove(aasReturn.AAS);
                success = true;
                if (Packages[aasReturn.IPackage].AasEnv.AdministrationShells.Count == 0)
                {
                    Packages[aasReturn.IPackage] = null;
                }
                else
                {
                    if (asset != null)
                    {
                        //TODO:Check if needs to be removed (BG: Asset can have multiple shells)
                        Packages[aasReturn.IPackage].AasEnv.Assets.Remove(asset);
                    }
                }
            }

            return success;
        }


        internal bool DeleteSubmodelElementByPath(SubmodelElement submodelElement, object parent)
        {
            if (parent is SubmodelElementCollection parentColl)
            {
                parentColl.value.Remove(submodelElement);
                return true;
            }
            else if (parent is Submodel parentSm)
            {
                parentSm.Remove(submodelElement);
                return true;
            }

            return false;
        }

        internal Asset FindAssetwithReference(AssetRef assetRef)
        {
            if (Packages != null)
            {
                foreach (var env in Packages)
                {
                    if (env != null)
                    {
                        Asset asset = env.AasEnv.FindAsset(assetRef);
                        if (asset != null)
                        {
                            return asset;
                        }
                    }
                }
            }

            return null;
        }

        internal bool DeleteSubmodelReferenceFromAAS(string aasIdentifier, string submodelIdentifier)
        {
            FindAasReturn aasReturn = FindAas(aasIdentifier);

            if (aasReturn.AAS != null)
            {
                SubmodelRef submodelRef = FindSubmodelRefWithinAAS(aasReturn, submodelIdentifier);
                if (submodelRef != null)
                {
                    aasReturn.AAS.submodelRefs.Remove(submodelRef);

                    return true;
                }
            }

            return false;
        }

        internal SubmodelRef FindSubmodelRefWithinAAS(FindAasReturn aasReturn, string submodelIdentifier)
        {
            if (aasReturn.AAS != null)
            {
                foreach (var submodelRef in aasReturn.AAS.submodelRefs)
                {
                    var submodel = Packages[aasReturn.IPackage].AasEnv.FindSubmodel(submodelRef);
                    if (submodel != null && submodel.identification.id != null && submodel.identification.id.Trim().ToLower() == submodelIdentifier.Trim().ToLower())
                        return submodelRef;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the submodel from the server
        /// </summary>
        /// <param name="submodelIdentifier">submodelIdentifier</param>
        /// <param name="packageIndex">The Package index of the found submodel from the server</param>
        /// <returns></returns>
        internal Submodel FindSubmodel(string submodelIdentifier, out int packageIndex)
        {
            packageIndex = -1;
            if (Packages[0] == null || Packages[0].AasEnv == null || string.IsNullOrEmpty(submodelIdentifier))
                return null;

            for (int envi = 0; envi < Packages.Length; envi++)
            {
                var env = Packages[envi];
                if (env != null)
                {
                    foreach (var submodel in env.AasEnv.Submodels)
                    {
                        if (submodel != null && submodel.identification.id != null && submodel.identification.id.Trim().ToLower() == submodelIdentifier.Trim().ToLower())
                        {
                            packageIndex = envi;
                            return submodel;
                        }
                    }
                }
            }

            // no
            return null;
        }

        /// <summary>
        /// Creates a new Asset Administration Shell
        /// </summary>
        /// <param name="aas">Requested AAS to be added in the server</param>
        /// <returns>If AAS was successfully added</returns>
        internal bool PostAAS(AdministrationShell aas)
        {
            bool emptyPackageAvailable = false;
            int emptyPackageIndex = -1;

            for (int envi = 0; envi < Packages.Length; envi++)
            {
                if (Packages[envi] == null)
                {
                    emptyPackageAvailable = true;
                    emptyPackageIndex = envi;
                    break;
                }
            }

            if (emptyPackageAvailable)
            {
                Packages[emptyPackageIndex] = new AdminShellPackageEnv();
                Packages[emptyPackageIndex].AasEnv.AdministrationShells.Add(aas);
                return true;
            }

            return false;
        }

        internal List<AdministrationShell> FindAllAasByAASIds(List<string> aasIds)
        {
            var aasList = new List<AdministrationShell>();
            foreach (string aasId in aasIds)
            {
                string aasIdDecoded = Base64UrlEncoder.Decode(aasId);
                var aasReturn = FindAas(aasIdDecoded);
                if (aasReturn != null)
                {
                    aasList.Add(aasReturn.AAS);
                }
            }
            return aasList;
        }

        internal void FindAllConceptDescriptionsInSubmodels(List<string> submodelIds, List<ConceptDescription> conceptDescriptionList)
        {
            foreach (string submodelId in submodelIds)
            {
                string submodelIdDecoded = Base64UrlEncoder.Decode(submodelId);
                var submodel = FindSubmodel(submodelIdDecoded, out _);
                FindAllConceptDescriptionsInSubmodel(submodel, conceptDescriptionList);
            }
        }

        internal List<ConceptDescription> FindConceptDescriptionInAASs(List<string> aasIds)
        {
            var conceptDescriptionList = new List<ConceptDescription>();
            foreach (var aasId in aasIds)
            {
                string aasIdDecoded = Base64UrlEncoder.Decode(aasId);
                var aasReturn = FindAas(aasIdDecoded);
                if (aasReturn != null)
                {
                    //Find direct concept descriptions
                    foreach (ConceptDictionary conceptDictionary in aasReturn.AAS.conceptDictionaries)
                    {
                        var cds = FindConceptDescriptionByReference(conceptDictionary.conceptDescriptionsRefs);
                        conceptDescriptionList.AddRange(cds);
                    }

                    //Find concept descriptions from the submodels of the AAS
                    foreach (var submodelRef in aasReturn.AAS.submodelRefs)
                    {
                        var submodel = FindSubmodelWithReference(submodelRef);
                        if (submodel != null)
                        {
                            var conceptDescriptions = new List<ConceptDescription>();
                            FindAllConceptDescriptionsInSubmodel(submodel, conceptDescriptions);
                            conceptDescriptionList.AddRange(conceptDescriptions);
                        }
                    }
                }
            }
            return conceptDescriptionList;
        }

        /// <summary>
        /// All the concept descriptions from the submodel, including submodel elements. The method is recursive.
        /// </summary>
        /// <param name="obj">Submodel or Submodel Element</param>
        /// <param name="conceptDescriptions">A list of found concept descriptions</param>
        public void FindAllConceptDescriptionsInSubmodel(object obj, List<ConceptDescription> conceptDescriptions)
        {
            //Concept description of the submodel
            if (obj is Submodel submodel)
            {
                if (submodel.semanticId != null)
                {
                    var cd = FindConceptDescriptionByReference(submodel.semanticId);
                    if (cd != null && !conceptDescriptions.Contains(cd))
                    {
                        conceptDescriptions.Add(cd);
                    }

                    //Also search recursively for all its submodel elements
                    foreach (var submodelElement in submodel.submodelElements)
                    {
                        FindAllConceptDescriptionsInSubmodel(submodelElement.submodelElement, conceptDescriptions);
                    }
                }
            }
            else if (obj is SubmodelElementCollection collection)
            {
                if (collection.semanticId != null)
                {
                    var cd = FindConceptDescriptionByReference(collection.semanticId);
                    if (cd != null && !conceptDescriptions.Contains(cd))
                    {
                        conceptDescriptions.Add(cd);
                    }

                    //Also search recursively for all its submodel elements
                    foreach (var submodelElement in collection.value)
                    {
                        FindAllConceptDescriptionsInSubmodel(submodelElement.submodelElement, conceptDescriptions);
                    }
                }
            }
            else if (obj is Entity entity)
            {
                if (entity.semanticId != null)
                {
                    var cd = FindConceptDescriptionByReference(entity.semanticId);
                    if (cd != null && !conceptDescriptions.Contains(cd))
                    {
                        conceptDescriptions.Add(cd);
                    }

                    //Also search recursively for all its submodel elements
                    foreach (var submodelElement in entity.statements)
                    {
                        FindAllConceptDescriptionsInSubmodel(submodelElement.submodelElement, conceptDescriptions);
                    }
                }
            }
            else if (obj is SubmodelElement submodelElement)
            {
                var cd = FindConceptDescriptionByReference(submodelElement.semanticId);
                if (cd != null && !conceptDescriptions.Contains(cd))
                {
                    conceptDescriptions.Add(cd);
                }
            }

        }

        private List<ConceptDescription> FindConceptDescriptionByReference(ConceptDescriptionRefs conceptDescriptionsRefs)
        {
            var conceptDescriptions = new List<ConceptDescription>();
            if (conceptDescriptionsRefs != null && conceptDescriptionsRefs.conceptDescriptions != null)
            {
                foreach (var cdr in conceptDescriptionsRefs.conceptDescriptions)
                {
                    var cd = FindConceptDescriptionByReference(cdr);
                    if (cd != null)
                    {
                        conceptDescriptions.Add(cd);
                    }
                }
            }

            return conceptDescriptions;
        }

        private ConceptDescription FindConceptDescriptionByReference(Reference cdr)
        {
            if (cdr == null)
                return null;

            var keys = cdr.Keys;

            if (keys == null)
                return null;

            // can only refs with 1 key
            if (keys.Count != 1)
                return null;

            var key = keys[0];
            if (!key.local || key.type.ToLower().Trim() != "conceptdescription")
                return null;

            var conceptDescription = FindConceptDescription(key.value, out _);
            if (conceptDescription != null)
                return conceptDescription;

            return null;
        }

        internal List<Submodel> FindAllSubmodelsBySubmodelIds(List<string> submodelIds)
        {
            var submodelList = new List<Submodel>();
            foreach (string submodelId in submodelIds)
            {
                string submodelIdDecoded = Base64UrlEncoder.Decode(submodelId);
                var submodel = FindSubmodel(submodelIdDecoded, out _);
                if (submodel != null)
                {
                    submodelList.Add(submodel);
                }
            }

            return submodelList;
        }

        /// <summary>
        /// Updates the Asset Administration Shell
        /// </summary>
        /// <param name="aas">Requested AAS to be updated in the server</param>
        /// <param name="aasReturn">Existing AAS from the server</param>
        /// <returns>If AAS was successfully modified</returns>
        internal bool PutAAS(AdministrationShell aas, FindAasReturn aasReturn)
        {
            if (aasReturn.AAS != null)
            {
                Packages[aasReturn.IPackage].AasEnv.AdministrationShells.Remove(aasReturn.AAS);
                Packages[aasReturn.IPackage].AasEnv.AdministrationShells.Add(aas);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a new Concept Description
        /// </summary>
        /// <param name="conceptDescription">Concept Description object</param>
        /// <returns>If the concept description was successfully added</returns>
        internal bool PostConceptDescription(ConceptDescription conceptDescription)
        {
            bool emptyPackageAvailable = false;
            int emptyPackageIndex = -1;

            for (int envi = 0; envi < Packages.Length; envi++)
            {
                if (Packages[envi] == null)
                {
                    emptyPackageAvailable = true;
                    emptyPackageIndex = envi;
                    break;
                }
            }

            if (emptyPackageAvailable)
            {
                Packages[emptyPackageIndex] = new AdminShellPackageEnv();
                Packages[emptyPackageIndex].AasEnv.ConceptDescriptions.Add(conceptDescription);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates an existing Concept Description
        /// </summary>
        /// <param name="conceptDescription">Concept Description object</param>
        /// <param name="existingCD">Existing Concept Description object from the server</param>
        /// <param name="packageIndex">Package Index of the existingCD </param>
        /// <returns></returns>
        internal bool PutConceptDescription(ConceptDescription conceptDescription, ConceptDescription existingCD, int packageIndex)
        {
            if (packageIndex != -1)
            {
                int indexExistingCd = Packages[packageIndex].AasEnv.ConceptDescriptions.IndexOf(existingCD);
                Packages[packageIndex].AasEnv.ConceptDescriptions.RemoveAt(indexExistingCd);
                Packages[packageIndex].AasEnv.ConceptDescriptions.Insert(indexExistingCd, conceptDescription);
                return true;
            }
            return false;
        }

        internal List<AdministrationShell> FindAllAasByIdShort(string idShort)
        {
            List<AdministrationShell> outputShells = new List<AdministrationShell>();
            foreach (AdminShellPackageEnv env in Packages)
            {
                if (env != null)
                {
                    foreach (AdministrationShell aas in env.AasEnv.AdministrationShells)
                    {
                        if (!string.IsNullOrEmpty(aas.idShort) && aas.idShort.Equals(idShort))
                        {
                            outputShells.Add(aas);
                        }
                    }
                }
            }

            return outputShells;
        }

        internal List<AdministrationShell> FindAllAasByAssetIds(List<IdentifierKeyValuePair_V2> assetIdList)
        {
            List<AdministrationShell> outputShells = new List<AdministrationShell>();

            foreach (IdentifierKeyValuePair_V2 assetId in assetIdList)
            {
                outputShells.AddRange(FindAasByAssetId(assetId.Value));
            }

            return outputShells;
        }

        private List<AdministrationShell> FindAasByAssetId(string assetId)
        {
            List<AdministrationShell> outputShells = new List<AdministrationShell>();
            foreach (AdminShellPackageEnv env in Packages)
            {
                if (env != null)
                {
                    foreach (AdministrationShell aas in env.AasEnv.AdministrationShells)
                    {
                        if (!aas.assetRef.IsEmpty && aas.assetRef.Keys[0].value.Equals(assetId))
                        {
                            outputShells.Add(aas);
                        }
                    }
                }
            }

            return outputShells;
        }

        internal List<ConceptDescription> FindAllConceptDescriptionsByIdShort(string idShort)
        {
            List<ConceptDescription> outputCds = new List<ConceptDescription>();
            foreach (AdminShellPackageEnv env in AasxServer.Program.env)
            {
                if (env != null)
                {
                    foreach (ConceptDescription conceptDescription in env.AasEnv.ConceptDescriptions)
                    {
                        if (conceptDescription.idShort.Equals(idShort))
                        {
                            outputCds.Add(conceptDescription);
                        }
                    }
                }
            }

            return outputCds;
        }

        internal object FindAllConceptDescriptionsByDataSpecRef(DataSpecificationRef dataSpecRefReq)
        {
            List<ConceptDescription> outputCds = new List<ConceptDescription>();
            foreach (AdminShellPackageEnv env in AasxServer.Program.env)
            {
                if (env != null)
                {
                    foreach (ConceptDescription conceptDescription in env.AasEnv.ConceptDescriptions)
                    {
                        if (conceptDescription.embeddedDataSpecification != null)
                        {
                            foreach (EmbeddedDataSpecification embDataSpec in conceptDescription.embeddedDataSpecification)
                            {
                                if (embDataSpec.dataSpecification.Matches(dataSpecRefReq))
                                {
                                    outputCds.Add(conceptDescription);
                                }
                            }

                        }
                    }
                }
            }

            return outputCds;
        }

        internal List<ConceptDescription> FindAllConceptDescriptionsByIsCaseOf(List<Reference> isCaseOfObj)
        {
            List<ConceptDescription> outputCds = new List<ConceptDescription>();
            foreach (AdminShellPackageEnv env in AasxServer.Program.env)
            {
                if (env != null)
                {
                    foreach (ConceptDescription conceptDescription in env.AasEnv.ConceptDescriptions)
                    {
                        if ((conceptDescription.IsCaseOf != null) && CompareIsCaseOf(conceptDescription.IsCaseOf, isCaseOfObj))
                        {
                            outputCds.Add(conceptDescription);
                        }
                    }
                }
            }

            return outputCds;
        }

        internal bool CompareDataSpecification(HasDataSpecification embeddedDataSpecification, DataSpecificationRef dataSpecRefReq)
        {
            if (embeddedDataSpecification != null)
            {
                foreach (EmbeddedDataSpecification embDataSpec in embeddedDataSpecification)
                {
                    if (embDataSpec.dataSpecification.Matches(dataSpecRefReq))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal bool CompareIsCaseOf(List<Reference> isCaseOf1, List<Reference> isCaseOf2)
        {
            foreach (Reference isCaseOf1_Ref in isCaseOf1)
            {
                bool found = false;
                foreach (Reference isCaseOf2_Ref in isCaseOf2)
                {
                    if (isCaseOf1_Ref.Matches(isCaseOf2_Ref))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        internal object HandleOutputModifiers(object obj, string level = "deep", string content = "normal", string extent = "withoutBlobValue")
        {
            //Validate requested modifiers against applicable modifiers as per specifications
            ValidateOutputModifiers(obj, level, content, extent);

            //TODO: Better way to use default values when null
            if (string.IsNullOrEmpty(level))
            {
                level = "deep";
            }

            if (string.IsNullOrEmpty(content))
            {
                content = "normal";
            }

            if (string.IsNullOrEmpty(extent))
            {
                extent = "withoutBlobValue";
            }

            OutputModifierContractResolver contractResolver = new OutputModifierContractResolver();

            if (level.Equals("core", StringComparison.OrdinalIgnoreCase))
            {
                contractResolver.Deep = false;
            }

            contractResolver.Content = content;
            contractResolver.Extent = extent;

            if (content.Equals("reference", StringComparison.OrdinalIgnoreCase))
            {
                obj = GetObjectReference(obj);
            }

            if (content.Equals("path", StringComparison.OrdinalIgnoreCase))
            {
                List<string> idShortPath = new List<string>();
                GetIdShortPath(obj, level, idShortPath);
                return idShortPath;
            }

            var settings = new JsonSerializerSettings();
            if (content.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                //settings.Converters.Add(new ValueOnlyJsonConverter(true, obj));
                //var jsonTest = JsonConvert.SerializeObject(obj, settings);
                object output = GetValueOnly(obj, level, extent);
                settings.ContractResolver = new CamelCasePropertyNamesContractResolver()
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                    {
                        // Do not change dictionary keys casing
                        ProcessDictionaryKeys = false
                    }
                };
                var jsonOutput = JsonConvert.SerializeObject(output, Formatting.Indented, settings);
                return jsonOutput;
            }

            if (contractResolver != null)
                settings.ContractResolver = contractResolver;
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);

            return json;
        }

        /// <summary>
        /// Validates the output modifiers as per specifications, (Refer Details of Asset Administration Shell Part 2, Section 9.3)
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="level"></param>
        /// <param name="content"></param>
        /// <param name="extent"></param>
        private void ValidateOutputModifiers(object obj, string level, string content, string extent)
        {
            if (obj is AdministrationShell)
            {
                //No level or extent applicable, bzw., not even in the API
                if ((content != null) && !content.CompareMultiple("normal", "reference", "trimmed"))
                {
                    throw new Exception($"Invalid Output Modifier {content}");
                }
            }
            else if (obj is SubmodelElementCollection)
            {
                //Do nothing, everything is applicable. Added the clause, as SMEColl is also an SME, hence below conditions could have been applied
            }
            else if (obj is SubmodelElement)
            {
                //Level and extent are not applicable.
                if (level != null)
                {
                    throw new Exception($"Invalid Output Modifier {level}");
                }

                //Conditions on Content
                if (obj is Capability || obj is Operation)
                {
                    if ((content != null) && !content.CompareMultiple("normal", "reference"))
                    {
                        throw new Exception($"Invalid Output Modifier {content}");
                    }
                }

                if ((content != null) && !content.CompareMultiple("normal", "reference", "value", "trimmed"))
                {
                    throw new Exception($"Invalid Output Modifier {content}");
                }

                //Conditions on Extent
                if (extent != null)
                {
                    if (obj is Blob)
                    {
                        if (!extent.CompareMultiple("withoutBlobValue", "withBlobValue"))
                        {
                            throw new Exception($"Invalid Output Modifier {extent}");
                        }
                    }
                    else
                    {
                        throw new Exception($"Invalid Output Modifier {extent}");
                    }
                }
            }
        }

        private object GetValueOnly(object obj, string level, string extent)
        {
            try
            {
                if (obj is Submodel submodel)
                {
                    //Submodel is serialized as an unnamed JSON object
                    //List<object> values = new List<object>();
                    var output = new Dictionary<string, object>();
                    foreach (var smElement in submodel.submodelElements)
                    {
                        if (smElement.submodelElement is Operation || smElement.submodelElement is Capability)
                        {
                            continue;
                        }

                        object value = GetValueOnly(smElement.submodelElement, level, extent);
                        if (value is IDictionary valDict)
                        {
                            foreach (var key in valDict.Keys)
                            {
                                output.Add((string)key, valDict[key]);
                            }
                        }
                        //values.Add(value);
                    }
                    return output;
                    //return values;
                }
                else if (obj is SubmodelElementCollection collection)
                {
                    //SMECollection is serialized as named JSON Object
                    //Dictionary<string, List<object>> output = new Dictionary<string, List<object>>();
                    //List<object> values = new List<object>();
                    Dictionary<string, object> output = new Dictionary<string, object>();
                    var values = new Dictionary<string, object>();
                    foreach (var smElement in collection.value)
                    {
                        //When core, should only include direct child elements. SMEs of child collection cannot be considered as direct child
                        if ((smElement.submodelElement is SubmodelElementCollection) && level.Equals("core", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        if (smElement.submodelElement is Operation || smElement.submodelElement is Capability)
                        {
                            continue;
                        }

                        object value = GetValueOnly(smElement.submodelElement, level, extent);
                        if (value is IDictionary valDict)
                        {
                            foreach (var key in valDict.Keys)
                            {
                                values.Add((string)key, valDict[key]);
                            }
                        }
                        //values.Add(value);
                    }
                    output.Add(collection.idShort, values);
                    return output;
                }
                else if (obj is SubmodelElement submodelElement)
                {
                    if (obj is Blob blob)
                    {
                        if (extent.Equals("withBlobValue", StringComparison.OrdinalIgnoreCase))
                        {
                            return blob.ToWithBlobOnlyValue();
                        }
                    }
                    return submodelElement.ToValueOnlySerialization();
                }
                else if (obj is List<AdminShellV20.SubmodelElement> smEleList)
                {
                    List<object> values = new List<object>();
                    foreach (var smElement in smEleList)
                    {
                        object value = GetValueOnly(smElement, level, extent);
                        values.Add(value);
                    }
                    return values;
                }
                else
                {
                    Console.WriteLine("Not supported");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            return null;
        }

        private void GetIdShortPath(object obj, string level, List<string> idShortPath)
        {
            if (obj is Submodel submodel)
            {
                idShortPath.Add(submodel.idShort);
                foreach (var smElement in submodel.submodelElements)
                {
                    GetIdShortPath(smElement.submodelElement, level, idShortPath);
                }
            }
            else if (obj is SubmodelElementCollection collection)
            {
                if (idShortPath.Count == 0)
                {
                    idShortPath.Add(collection.idShort);
                }
                else
                {
                    idShortPath.Add(idShortPath.Last() + "." + collection.idShort);
                }
                if (level.Equals("deep", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var smEle in collection.value)
                    {
                        GetIdShortPath(smEle.submodelElement, level, idShortPath);
                    }
                }
            }
            else if (obj is Entity entity)
            {
                if (idShortPath.Count == 0)
                {
                    idShortPath.Add(entity.idShort);
                }
                else
                {
                    idShortPath.Add(idShortPath.Last() + "." + entity.idShort);
                }
                //TODO: look for definition and children*
            }
            else if (obj is SubmodelElement smEle)
            {
                idShortPath.Add(idShortPath.Last() + "." + smEle.idShort);
            }
            else
            {
                Console.WriteLine($"Outout modifier pathis not applicable to {obj.GetType()}");
            }
        }

        private object GetObjectReference(object obj)
        {
            if (obj is AdminShellV20.AdministrationShell aas)
                return aas.GetReference();
            else if (obj is Submodel submodel)
                return submodel.GetReference();
            else if (obj is SubmodelElement submodelElement)
                return submodelElement.GetReference();
            else if (obj is List<AdminShellV20.SubmodelElement> smEleList)
            {
                List<object> values = new List<object>();
                foreach (var smElement in smEleList)
                {
                    object value = GetObjectReference(smElement);
                    values.Add(value);
                }
                return values;
            }
            else
            {
                Console.WriteLine("Error: Object not handled for the Reference type modifier.");
                return obj;
            }
        }

        /// <summary>
        /// Creates a new Submodel
        /// </summary>
        /// <param name="submodel">Submodel object</param>
        /// <returns></returns>
        internal bool PostSubmodel(Submodel submodel)
        {
            bool emptyPackageAvailable = false;
            int emptyPackageIndex = -1;

            for (int envi = 0; envi < Packages.Length; envi++)
            {
                if (Packages[envi] == null)
                {
                    emptyPackageAvailable = true;
                    emptyPackageIndex = envi;
                    break;
                }
            }

            if (emptyPackageAvailable)
            {
                Packages[emptyPackageIndex] = new AdminShellPackageEnv();
                Packages[emptyPackageIndex].AasEnv.Submodels.Add(submodel);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the Submodel
        /// </summary>
        /// <param name="submodel">Submodel object</param>
        /// <param name="existingSubmodel">Existing Submodel object from the server</param>
        /// <param name="packageIndex">The package index of the existing Submodel object</param>
        /// <returns></returns>
        internal bool PutSubmodel(Submodel submodel, Submodel existingSubmodel, int packageIndex)
        {
            if (packageIndex != -1)
            {
                Packages[packageIndex].AasEnv.Submodels.Remove(existingSubmodel);
                Packages[packageIndex].AasEnv.Submodels.Add(submodel);
                return true;
            }
            return false;
        }

        internal Submodel FindSubmodelWithReference(Reference submodelRef)
        {
            //There should be exactly one key
            if (submodelRef.Count != 1)
                return null;

            foreach (var env in Packages)
            {
                if (env != null)
                {
                    var submodel = env.AasEnv.FindSubmodel(new Identification(submodelRef.First.idType, submodelRef.First.value));
                    if (submodel != null)
                    {
                        return submodel;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a new submodel element
        /// </summary>
        /// <param name="submodel">Parent Submodel</param>
        /// <param name="submodelElement">New Submodel Element to be added</param>
        /// <returns></returns>
        internal bool PostSubmodelElement(Submodel submodel, SubmodelElement submodelElement)
        {
            if (submodel != null)
            {
                submodel.Add(submodelElement);
                submodel.SetAllParents(DateTime.UtcNow);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="idShortPath">e.g. SMEColl_idShort.SME_idShort</param>
        /// <param name="outParent">Parent of SME, bzw. SMEColl</param>
        /// <returns></returns>
        internal SubmodelElement FindSubmodelElementByPath(object parent, string idShortPath, out object outParent)
        {
            outParent = parent;
            if (idShortPath.Contains('.'))
            {
                string[] idShorts = idShortPath.Split('.', 2);
                if (parent is Submodel submodel)
                {
                    var submodelElement = submodel.FindSubmodelElementWrapper(idShorts[0]);
                    if (submodelElement != null)
                    {
                        return FindSubmodelElementByPath(submodelElement.submodelElement, idShorts[1], out outParent);
                    }
                }
                else if (parent is SubmodelElementCollection collection)
                {
                    var submodelElement = collection.FindFirstIdShort(idShorts[0]);
                    if (submodelElement != null)
                    {
                        return FindSubmodelElementByPath(submodelElement.submodelElement, idShorts[1], out outParent);
                    }
                }
            }
            else
            {
                if (parent is Submodel submodel)
                {
                    var submodelElement = submodel.FindSubmodelElementWrapper(idShortPath);
                    if (submodelElement != null)
                    {
                        return submodelElement.submodelElement;
                    }
                }
                else if (parent is SubmodelElementCollection collection)
                {
                    var submodelElement = collection.FindFirstIdShort(idShortPath);
                    if (submodelElement != null)
                    {
                        return submodelElement.submodelElement;
                    }
                }
            }
            return null;
        }

        internal object FindAllSubmodelsBySemanticId(SemanticId reqSemaniticId)
        {
            var outputSubmodels = new List<AdminShellV20.Submodel>();
            foreach (AdminShellPackageEnv env in AasxServer.Program.env)
            {
                if (env != null)
                {
                    foreach (Submodel submodel in env.AasEnv.Submodels)
                    {
                        if ((submodel.semanticId != null) && submodel.semanticId.Matches(reqSemaniticId))
                        {
                            outputSubmodels.Add(submodel);
                        }
                    }
                }
            }

            return outputSubmodels;
        }

        internal object GetOperationAsyncResult(string handleId)
        {
            AsyncOperationResultStorage.opResultAsyncDict.TryGetValue(handleId, out Models.OperationResult opResult);
            return opResult;
        }

        internal List<Submodel> FindAllSubmodelsByIdShort(string idShort)
        {
            var outputSubmodels = new List<AdminShellV20.Submodel>();
            foreach (AdminShellPackageEnv env in AasxServer.Program.env)
            {
                if (env != null)
                {
                    foreach (Submodel submodel in env.AasEnv.Submodels)
                    {
                        if (submodel.idShort.Equals(idShort))
                        {
                            outputSubmodels.Add(submodel);
                        }
                    }
                }
            }

            return outputSubmodels;
        }

        internal object InvokeOperationSync(Operation operation, Models.OperationRequest operationRequest)
        {
            Models.OperationResult opResult = new Models.OperationResult();
            //Check the qualifier for demo
            if (operation.HasQualifierOfType("Demo") != null)
            {
                opResult.OutputArguments = new List<Models.OperationVariable>();
                opResult.OutputArguments.Add(new Models.OperationVariable());
                opResult.ExecutionState = Models.OperationResult.ExecutionStateEnum.CompletedEnum;
                Models.Result result = new Models.Result();
                result.Success = true;
                opResult.ExecutionResult = result;
                opResult.RequestId = operationRequest.RequestId;
            }

            return opResult;
        }

        internal Models.OperationHandle InvokeOperationAsync(Operation operation, Models.OperationRequest body)
        {
            Models.OperationHandle opHandle = new Models.OperationHandle();
            //Check the qualifier for demo
            if (operation.HasQualifierOfType("Demo") != null)
            {
                opHandle.RequestId = body.RequestId;
                opHandle.HandleId = Guid.NewGuid().ToString();
                InvokeTestOperation(opHandle);
            }

            return opHandle;
        }


        //TestOperation invokation for demo
        private void InvokeTestOperation(Models.OperationHandle opHandle)
        {
            //First invokation
            Models.OperationResult opResult = new Models.OperationResult();
            opResult.OutputArguments = new List<Models.OperationVariable>
            {
                new Models.OperationVariable()
            };
            opResult.ExecutionState = Models.OperationResult.ExecutionStateEnum.InitiatedEnum;
            Models.Message message = new Models.Message
            {
                Code = "xxx",
                MessageType = Models.Message.MessageTypeEnum.InfoEnum,
                Text = "Initiated the operation",
                Timestamp = DateTime.UtcNow.ToString()
            };
            Models.Result result = new Models.Result
            {
                Messages = new List<Models.Message>() { message }
            };
            opResult.ExecutionResult = result;
            opResult.RequestId = opHandle.RequestId;

            AsyncOperationResultStorage.opResultAsyncDict.Add(opHandle.HandleId, opResult);

            m_simulationTimer = new Timer(DoSimulation, null, 5000, 5000);
        }

        private void DoSimulation(object state)
        {
            var random = new Random();
            var values = Enum.GetValues(typeof(Models.OperationResult.ExecutionStateEnum));

            foreach (var handleId in AsyncOperationResultStorage.opResultAsyncDict.Keys)
            {
                var value = (Models.OperationResult.ExecutionStateEnum)values.GetValue(random.Next(values.Length));
                AsyncOperationResultStorage.opResultAsyncDict[handleId].ExecutionState = value;
            }
        }


        /// <summary>
        /// Creates a new submodel element at a specified path within submodel elements hierarchy
        /// </summary>
        /// <param name="parentSME">Parent Submodel Element</param>
        /// <param name="submodelElement">Requested Submodel Element to be added</param>
        /// <returns>Returns true, if successfully added</returns>
        internal bool PostSubmodelElementByPath(SubmodelElement parentSME, SubmodelElement submodelElement)
        {
            if (parentSME is SubmodelElementCollection parentSMEColl)
            {
                parentSMEColl.Add(submodelElement);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates an existing submodel element at a specified path within submodel elements hierarchy
        /// </summary>
        /// <param name="parent">Parent SME or Submodel</param>
        /// <param name="submodelElement">Submodel Element to be updated</param>
        /// <param name="existingSME">Existing submodel element from the server, that needs to be updated</param>
        /// <returns></returns>
        internal bool PutSubmodelElementByPath(object parent, SubmodelElement submodelElement, SubmodelElement existingSME)
        {
            if (parent is SubmodelElementCollection parentSMEColl)
            {
                int indexOfExistingSME = -1;
                for (int i = 0; i < parentSMEColl.value.Count; i++)
                {
                    if (existingSME.Equals(parentSMEColl.value[i].submodelElement))
                    {
                        indexOfExistingSME = i;
                        break;
                    }
                }
                parentSMEColl.value.RemoveAt(indexOfExistingSME);
                parentSMEColl.Insert(indexOfExistingSME, submodelElement);
                return true;
            }
            else if (parent is Submodel submodel)
            {
                int indexOfExistingSME = -1;
                for (int i = 0; i < submodel.submodelElements.Count; i++)
                {
                    if (existingSME.Equals(submodel.submodelElements[i].submodelElement))
                    {
                        indexOfExistingSME = i;
                        break;
                    }
                }
                submodel.submodelElements.RemoveAt(indexOfExistingSME);
                submodel.Insert(indexOfExistingSME, submodelElement);
                return true;
            }
            return false;
        }

        internal bool AddAsset(Asset body, FindAasReturn aasReturn)
        {
            var existingAsset = Packages[aasReturn.IPackage].AasEnv.FindAsset(body.identification);
            //asset is already present // Ideal case
            if (existingAsset != null)
            {
                Packages[aasReturn.IPackage].AasEnv.Assets.Remove(existingAsset);
            }
            Packages[aasReturn.IPackage].AasEnv.Assets.Add(body);

            //Change the assetRef in AAS
            aasReturn.AAS.assetRef = new AssetRef(new Reference(new Key("Asset", true, body.identification.idType, body.identification.id)));
            return true;
        }


    }

    /// <summary>
    /// The class consists of an AAS and a ENV package which it belongs to.
    /// </summary>
    public class FindAasReturn
    {
        /// <summary>
        /// Asset Administration Shell
        /// </summary>
        public AdminShell.AdministrationShell AAS { get; set; } = null;
        /// <summary>
        /// The package index of the AAS
        /// </summary>
        public int IPackage { get; set; } = -1;
    }

    /// <summary>
    /// Test class to store handle id and operation execution results
    /// </summary>
    public static class AsyncOperationResultStorage
    {
        /// <summary>
        /// HandleId vs Operation Result of the corresponding Opration
        /// </summary>
        public static Dictionary<string, Models.OperationResult> opResultAsyncDict = new Dictionary<string, Models.OperationResult>();
    }
}
