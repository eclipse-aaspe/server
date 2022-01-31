using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using AdminShellNS;
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

        //TODO: delete the cd Ref from corresponding AAS
        internal bool DeleteConceptDescription(string cdIdentifier)
        {
            bool deleted = false;
            var conceptDescription = FindConceptDescription(cdIdentifier, out int packageIndex);
            if (conceptDescription != null)
            {
                Packages[packageIndex].AasEnv.ConceptDescriptions.Remove(conceptDescription);
                deleted = true;
            }

            return deleted;
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

            packageIndex = 0;
            return null;
        }

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

        internal bool DeleteSubmodelElementByPath(SubmodelElement submodelElement, SubmodelElement parent)
        {
            if (parent is SubmodelElementCollection parentColl)
            {
                parentColl.value.Remove(submodelElement);
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

        internal Submodel FindSubmodel(string submodelIdentifier)
        {
            if (Packages[0] == null || Packages[0].AasEnv == null || string.IsNullOrEmpty(submodelIdentifier))
                return null;

            //TODO: Check again
            // via handle
            //var specialHandles = CreateHandlesFromRawUrl(rawUrl);
            //var handleId = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(smid, specialHandles);
            //if (handleId != null && handleId.identification != null)
            //    return Packages[0].AasEnv.FindSubmodel(handleId.identification);

            // no, iterate & find
            foreach (var submodel in Packages[0].AasEnv.Submodels)
            {
                if (submodel != null && submodel.identification.id != null && submodel.identification.id.Trim().ToLower() == submodelIdentifier.Trim().ToLower())
                    return submodel;
            }

            // no
            return null;
        }

        /// <summary>
        /// If AAS exists, it is updated. If not, added.
        /// Similar to HTTP PUT
        /// </summary>
        /// <param name="aas"></param>
        /// <returns></returns>
        internal bool AddAas(AdministrationShell aas)
        {
            bool emptyPackageAvailable = false;
            int emptyPackageIndex = -1;
            for (int envi = 0; envi < Packages.Length; envi++)
            {
                if (Packages[envi] != null)
                {
                    var existingAas = Packages[envi].AasEnv.FindAAS(aas.identification);
                    if (existingAas != null)
                    {
                        Packages[envi].AasEnv.AdministrationShells.Remove(existingAas);
                        Packages[envi].AasEnv.AdministrationShells.Add(aas);
                        return true;
                    }
                }
                else
                {
                    if (!emptyPackageAvailable)
                    {
                        emptyPackageAvailable = true;
                        emptyPackageIndex = envi;
                        break;      //Added to avoid unnecessary iterations
                    }

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

        internal bool AddConceptDescription(ConceptDescription conceptDescription)
        {
            bool emptyPackageAvailable = false;
            int emptyPackageIndex = -1;
            for (int envi = 0; envi < Packages.Length; envi++)
            {
                if (Packages[envi] != null)
                {
                    var existingCD = Packages[envi].AasEnv.FindConceptDescription(conceptDescription.identification);
                    if (existingCD != null)
                    {
                        Packages[envi].AasEnv.ConceptDescriptions.Remove(existingCD);
                        Packages[envi].AasEnv.ConceptDescriptions.Add(conceptDescription);
                        return true;
                    }
                }
                else
                {
                    //TODO: This logic is not in the old server.
                    if (!emptyPackageAvailable)
                    {
                        emptyPackageAvailable = true;
                        emptyPackageIndex = envi;
                        break;      //Added to avoid unnecessary iterations
                    }

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

        private bool CompareIsCaseOf(List<Reference> isCaseOf1, List<Reference> isCaseOf2)
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
                object output = GetValueOnly(obj, level);
                var jsonOutput = JsonConvert.SerializeObject(output, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                return jsonOutput;
            }

            if (contractResolver != null)
                settings.ContractResolver = contractResolver;
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);

            return json;
        }

        private object GetValueOnly(object obj, string level)
        {
            try
            {
                if (obj is Submodel submodel)
                {
                    List<object> values = new List<object>();
                    foreach (var smElement in submodel.submodelElements)
                    {
                        object value = GetValueOnly(smElement.submodelElement, level);
                        values.Add(value);
                    }
                    return values;
                }
                else if (obj is SubmodelElementCollection collection)
                {
                    if (level.Equals("deep", StringComparison.OrdinalIgnoreCase))
                    {
                        List<object> values = new List<object>();
                        foreach (var smElement in collection.value)
                        {
                            object value = GetValueOnly(smElement.submodelElement, level);
                            values.Add(value);
                        }
                        return values;
                    }
                }
                else if (obj is SubmodelElement submodelElement)
                {
                    return submodelElement.ToValueOnlySerialization();
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
                idShortPath.Add(idShortPath.Last() + "." + collection.idShort);
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
                idShortPath.Add(idShortPath.Last() + "." + entity.idShort);
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
            else
            {
                Console.WriteLine("Error: Object not handled for the Reference type modifier.");
                return obj;
            }
        }

        internal bool AddSubmodel(Submodel submodel)
        {
            bool emptyPackageAvailable = false;
            int emptyPackageIndex = -1;
            for (int envi = 0; envi < Packages.Length; envi++)
            {
                if (Packages[envi] != null)
                {
                    var existingSubmodel = Packages[envi].AasEnv.FindSubmodel(submodel.identification);
                    if (existingSubmodel != null)
                    {
                        Packages[envi].AasEnv.Submodels.Remove(existingSubmodel);
                        Packages[envi].AasEnv.Submodels.Add(submodel);
                        return true;
                    }
                }
                else
                {
                    //TODO: This logic is not in the old server.
                    if (!emptyPackageAvailable)
                    {
                        emptyPackageAvailable = true;
                        emptyPackageIndex = envi;
                        break;      //Added to avoid unnecessary iterations
                    }

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

        internal bool AddSubmodelElement(Submodel submodel, SubmodelElement submodelElement)
        {
            var existingSmEle = submodel.FindSubmodelElementWrapper(submodelElement.idShort);
            //Check if submodel element already exists in the submodel
            if (existingSmEle != null)
            {
                int indexOfExistingSmw = submodel.submodelElements.IndexOf(existingSmEle);
                submodel.submodelElements.RemoveAt(indexOfExistingSmw);
                submodel.Insert(indexOfExistingSmw, submodelElement);
                submodel.SetAllParents(DateTime.UtcNow);
                return true;
            }
            else
            {
                submodel.Add(submodelElement);
                submodel.SetAllParents(DateTime.UtcNow);
                return true;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="submodel"></param>
        /// <param name="idShortPath">e.g. SMEColl_idShort.SME_idShort</param>
        /// <param name="parent">Parent of SME, bzw. SMEColl</param>
        /// <returns></returns>
        internal SubmodelElement FindSubmodelElementByPath(Submodel submodel, string idShortPath, out SubmodelElement parent)
        {
            parent = null;
            if (idShortPath.Contains('.'))
            {
                string[] idShorts = idShortPath.Split('.', 2);
                foreach (var smeWrapper in submodel.submodelElements)
                {
                    var submodelElement = smeWrapper.submodelElement;
                    if (submodelElement.idShort.Equals(idShorts[0]))
                    {
                        if (submodelElement is SubmodelElementCollection collection)
                        {
                            return FindSubmodelElementByPath(collection, idShorts[1], out parent);
                        }
                    }
                }
            }
            else
            {
                var smeWrapper = submodel.FindSubmodelElementWrapper(idShortPath);
                if (smeWrapper != null)
                    return smeWrapper.submodelElement;
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

        internal SubmodelElement FindSubmodelElementByPath(SubmodelElementCollection smeColl, string idShortPath, out SubmodelElement parent)
        {
            parent = null;
            string[] idShorts = idShortPath.Split('.', 2);
            foreach (var smeWrapper in smeColl.value)
            {
                var submodelElement = smeWrapper.submodelElement;
                if (submodelElement.idShort.Equals(idShorts[0]))
                {
                    if (submodelElement is SubmodelElementCollection collection)
                    {
                        parent = collection;
                        FindSubmodelElementByPath(collection, idShorts[1], out parent);
                    }
                    else if (submodelElement is SubmodelElement)
                    {
                        parent = smeColl;
                        return submodelElement;
                    }
                }
            }

            return null;
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
        /// Adds the submodel element to the SMECollection
        /// </summary>
        /// <param name="parentSME"></param>
        /// <param name="submodelElement"></param>
        /// <returns></returns>
        internal bool AddSubmodelElement(SubmodelElement parentSME, SubmodelElement submodelElement)
        {

            if (parentSME is SubmodelElementCollection parentSMEColl)
            {
                var existingSmEle = parentSMEColl.FindFirstIdShort(submodelElement.idShort);
                //Check if submodel element already exists in the collection
                if (existingSmEle != null)
                {
                    int indexOfExistingSmw = parentSMEColl.value.IndexOf(existingSmEle);
                    parentSMEColl.value.RemoveAt(indexOfExistingSmw);
                    parentSMEColl.Insert(indexOfExistingSmw, submodelElement);
                }
                else
                {
                    parentSMEColl.Add(submodelElement);

                }

                submodelElement.SetAllTimeStamps(DateTime.UtcNow);
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
        public AdminShell.AdministrationShell AAS { get; set; } = null;
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
