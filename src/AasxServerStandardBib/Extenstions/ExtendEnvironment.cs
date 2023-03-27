using AasCore.Aas3_0_RC02;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Extenstions
{
    public static class ExtendEnvironment
    {
        #region Environment

#if !DoNotUseAasxCompatibilityModels

        public static AasCore.Aas3_0_RC02.Environment ConvertFromV10(this AasCore.Aas3_0_RC02.Environment environment, AasxCompatibilityModels.AdminShellV10.AdministrationShellEnv sourceEnvironement)
        {
            //Convert Administration Shells
            if (sourceEnvironement.AdministrationShells != null)
            {
                if (environment.AssetAdministrationShells == null)
                {
                    environment.AssetAdministrationShells = new List<AssetAdministrationShell>();
                }
                foreach (var sourceAas in sourceEnvironement.AdministrationShells)
                {
                    var sourceAsset = sourceEnvironement?.FindAsset(sourceAas.assetRef);
                    if (sourceAsset != null)
                    {
                        var newAssetInformation = new AssetInformation(AssetKind.Instance);
                        newAssetInformation = newAssetInformation.ConvertFromV10(sourceAsset);

                        var newAas = new AssetAdministrationShell(id: sourceAas.identification.id, newAssetInformation);
                        newAas = newAas.ConvertFromV10(sourceAas);

                        environment.AssetAdministrationShells.Add(newAas);
                    }

                }
            }

            //Convert Submodels
            if (sourceEnvironement.Submodels != null)
            {
                if (environment.Submodels == null)
                {
                    environment.Submodels = new List<Submodel>();
                }
                foreach (var sourceSubmodel in sourceEnvironement.Submodels)
                {
                    var newSubmodel = new Submodel(sourceSubmodel.identification.id);
                    newSubmodel = newSubmodel.ConvertFromV10(sourceSubmodel);
                    environment.Submodels.Add(newSubmodel);
                }
            }

            if (sourceEnvironement.ConceptDescriptions != null)
            {
                if (environment.ConceptDescriptions == null)
                {
                    environment.ConceptDescriptions = new List<ConceptDescription>();
                }
                foreach (var sourceConceptDescription in sourceEnvironement.ConceptDescriptions)
                {
                    var newConceptDescription = new ConceptDescription(sourceConceptDescription.identification.id);
                    newConceptDescription = newConceptDescription.ConvertFromV10(sourceConceptDescription);
                    environment.ConceptDescriptions.Add(newConceptDescription);
                }
            }

            return environment;
        }


        public static AasCore.Aas3_0_RC02.Environment ConvertFromV20(this AasCore.Aas3_0_RC02.Environment environment, AasxCompatibilityModels.AdminShellV20.AdministrationShellEnv sourceEnvironement)
        {
            //Convert Administration Shells
            if (sourceEnvironement.AdministrationShells != null)
            {
                if (environment.AssetAdministrationShells == null)
                {
                    environment.AssetAdministrationShells = new List<AssetAdministrationShell>();
                }
                foreach (var sourceAas in sourceEnvironement.AdministrationShells)
                {
                    var sourceAsset = sourceEnvironement?.FindAsset(sourceAas.assetRef);
                    if (sourceAsset != null)
                    {
                        var newAssetInformation = new AssetInformation(AssetKind.Instance);
                        newAssetInformation = newAssetInformation.ConvertFromV20(sourceAsset);

                        var newAas = new AssetAdministrationShell(id: sourceAas.identification.id, newAssetInformation);
                        newAas = newAas.ConvertFromV20(sourceAas);

                        environment.AssetAdministrationShells.Add(newAas);
                    }

                }
            }

            //Convert Submodels
            if (sourceEnvironement.Submodels != null)
            {
                if (environment.Submodels == null)
                {
                    environment.Submodels = new List<Submodel>();
                }
                foreach (var sourceSubmodel in sourceEnvironement.Submodels)
                {
                    var newSubmodel = new Submodel(sourceSubmodel.identification.id);
                    newSubmodel = newSubmodel.ConvertFromV20(sourceSubmodel);
                    environment.Submodels.Add(newSubmodel);
                }
            }

            if (sourceEnvironement.ConceptDescriptions != null)
            {
                if (environment.ConceptDescriptions == null)
                {
                    environment.ConceptDescriptions = new List<ConceptDescription>();
                }
                foreach (var sourceConceptDescription in sourceEnvironement.ConceptDescriptions)
                {
                    var newConceptDescription = new ConceptDescription(sourceConceptDescription.identification.id);
                    newConceptDescription = newConceptDescription.ConvertFromV20(sourceConceptDescription);
                    environment.ConceptDescriptions.Add(newConceptDescription);
                }
            }

            return environment;
        }

#endif

        //TODO:jtikekar to test
        public static AasCore.Aas3_0_RC02.Environment CreateFromExistingEnvironment(this AasCore.Aas3_0_RC02.Environment environment, AasCore.Aas3_0_RC02.Environment sourceEnvironment, List<AssetAdministrationShell> filterForAas = null, List<AssetInformation> filterForAssets = null, List<Submodel> filterForSubmodel = null, List<ConceptDescription> filterForConceptDescriptions = null)
        {
            if (filterForAas == null)
            {
                filterForAas = new List<AssetAdministrationShell>();
            }

            if (filterForAssets == null)
            {
                filterForAssets = new List<AssetInformation>();
            }

            if (filterForSubmodel == null)
            {
                filterForSubmodel = new List<Submodel>();
            }

            if (filterForConceptDescriptions == null)
            {
                filterForConceptDescriptions = new List<ConceptDescription>();
            }

            //var outputEnvironment = new AasCore.Aas3_0_RC02.Environment();  

            //Copy AssetAdministrationShells
            foreach (var aas in sourceEnvironment.AssetAdministrationShells)
            {
                if (filterForAas.Contains(aas))
                {
                    environment.AssetAdministrationShells.Add(aas);

                    if (aas.Submodels != null && aas.Submodels.Count > 0)
                    {
                        foreach (var submodelReference in aas.Submodels)
                        {
                            var submodel = sourceEnvironment.FindSubmodel(submodelReference);
                            if (submodel != null)
                            {
                                filterForSubmodel.Add(submodel);
                            }
                        }
                    }
                }
            }

            //Copy Submodel
            foreach (var submodel in sourceEnvironment.Submodels)
            {
                if (filterForSubmodel.Contains(submodel))
                {
                    environment.Submodels.Add(submodel);

                    //Find Used CDs
                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, submodel.SubmodelElements, ref filterForConceptDescriptions);
                }
            }

            //Copy ConceptDescription
            foreach (var conceptDescription in sourceEnvironment.ConceptDescriptions)
            {
                if (filterForConceptDescriptions.Contains(conceptDescription))
                {
                    environment.ConceptDescriptions.Add(conceptDescription);
                }
            }

            return environment;

        }

        public static void CreateFromExistingEnvRecurseForCDs(this AasCore.Aas3_0_RC02.Environment environment, AasCore.Aas3_0_RC02.Environment sourceEnvironment, List<ISubmodelElement> submodelElements, ref List<ConceptDescription> filterForConceptDescription)
        {
            if (submodelElements == null || submodelElements.Count == 0 || filterForConceptDescription == null || filterForConceptDescription.Count == 0)
            {
                return;
            }

            foreach (var submodelElement in submodelElements)
            {
                if (submodelElement == null)
                {
                    return;
                }

                if (submodelElement.SemanticId != null)
                {
                    var conceptDescription = sourceEnvironment.FindConceptDescriptionByReference(submodelElement.SemanticId);
                    if (conceptDescription != null)
                    {
                        filterForConceptDescription.Add(conceptDescription);
                    }
                }

                if (submodelElement is SubmodelElementCollection smeColl)
                {
                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, smeColl.Value, ref filterForConceptDescription);
                }

                if (submodelElement is SubmodelElementList smeList)
                {
                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, smeList.Value, ref filterForConceptDescription);
                }

                if (submodelElement is Entity entity)
                {
                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, entity.Statements, ref filterForConceptDescription);
                }

                if (submodelElement is AnnotatedRelationshipElement annotatedRelationshipElement)
                {
                    var annotedELements = new List<ISubmodelElement>();
                    foreach (var annotation in annotatedRelationshipElement.Annotations)
                    {
                        annotedELements.Add(annotation);
                    }
                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, annotedELements, ref filterForConceptDescription);
                }

                if (submodelElement is Operation operation)
                {
                    var operationELements = new List<ISubmodelElement>();
                    foreach (var inputVariable in operation.InputVariables)
                    {
                        operationELements.Add(inputVariable.Value);
                    }

                    foreach (var outputVariable in operation.OutputVariables)
                    {
                        operationELements.Add(outputVariable.Value);
                    }

                    foreach (var inOutVariable in operation.InoutputVariables)
                    {
                        operationELements.Add(inOutVariable.Value);
                    }

                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, operationELements, ref filterForConceptDescription);

                }
            }
        }

        public static JsonWriter SerialiazeJsonToStream(this AasCore.Aas3_0_RC02.Environment environment, StreamWriter streamWriter, bool leaveJsonWriterOpen = false)
        {
            streamWriter.AutoFlush = true;

            JsonSerializer serializer = new JsonSerializer()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                Formatting = Newtonsoft.Json.Formatting.Indented
            };

            JsonWriter writer = new JsonTextWriter(streamWriter);
            serializer.Serialize(writer, environment);
            if (leaveJsonWriterOpen)
                return writer;
            writer.Close();
            return null;
        }


        #endregion

        #region Submodel Queries
        public static Submodel FindSubmodel(this AasCore.Aas3_0_RC02.Environment environment, Reference submodelReference)
        {
            if (submodelReference == null)
            {
                return null;
            }

            if (submodelReference.Keys.Count != 1) // Can have only one reference key
            {
                return null;
            }

            var key = submodelReference.Keys[0];
            if (key.Type != KeyTypes.Submodel)
            {
                return null;
            }

            var submodels = environment.Submodels.Where(s => s.Id.Equals(key.Value, StringComparison.OrdinalIgnoreCase));
            if (submodels.Any())
            {
                return submodels.First();
            }

            return null;
        }

        public static Submodel FindSubmodelById(this AasCore.Aas3_0_RC02.Environment environment, string submodelId)
        {
            if (string.IsNullOrEmpty(submodelId))
            {
                return null;
            }

            var submodels = environment.Submodels.Where(s => s.Id.Equals(submodelId));
            if (submodels.Any())
            {
                return submodels.First();
            }

            return null;
        }

        public static IEnumerable<Submodel> FindAllSubmodelsGroupedByAAS(this AasCore.Aas3_0_RC02.Environment environment, Func<AssetAdministrationShell, Submodel, bool> p = null)
        {
            if (environment.AssetAdministrationShells == null || environment.Submodels == null)
                yield break;
            foreach (var aas in environment.AssetAdministrationShells)
            {
                if (aas?.Submodels == null)
                    continue;
                foreach (var submodelReference in aas.Submodels)
                {
                    var submodel = environment.FindSubmodel(submodelReference);
                    if (submodel != null && (p == null || p(aas, submodel)))
                        yield return submodel;
                }
            }
        }

        public static IEnumerable<Submodel> FindAllSubmodelBySemanticId(this AasCore.Aas3_0_RC02.Environment environment, string semanticId)
        {
            if (semanticId == null)
                yield break;

            foreach (var submodel in environment.Submodels)
                if (true == submodel.SemanticId?.Matches(semanticId))
                    yield return submodel;
        }

        #endregion

        #region AssetAdministrationShell Queries
        public static AssetAdministrationShell FindAasWithSubmodelId(this AasCore.Aas3_0_RC02.Environment environment, string submodelId)
        {
            if (submodelId == null)
            {
                return null;
            }

            var aas = environment.AssetAdministrationShells.Where(a => (a.Submodels?.Where(s => s.Matches(submodelId)).First()) != null).First();

            return aas;
        }

        public static AssetAdministrationShell FindAasById(this AasCore.Aas3_0_RC02.Environment environment, string aasId)
        {
            if (string.IsNullOrEmpty(aasId))
            {
                return null;
            }

            var aasList = environment.AssetAdministrationShells.Where(a => a.Id.Equals(aasId));
            if(aasList.Any())
            {
                return aasList.First();
            }

            return null;
        }

        #endregion

        #region ConceptDescription Queries

        public static ConceptDescription FindConceptDescriptionById(this AasCore.Aas3_0_RC02.Environment environment, string conceptDescriptionId)
        {
            if (string.IsNullOrEmpty(conceptDescriptionId))
            {
                return null;
            }

            var conceptDescriptionList = environment.ConceptDescriptions.Where(c => c.Id.Equals(conceptDescriptionId));
            if(conceptDescriptionList.Any())
            {
                return conceptDescriptionList.First();  
            }

            return null;
        }

        public static ConceptDescription FindConceptDescriptionByReference(this AasCore.Aas3_0_RC02.Environment environment, Reference reference)
        {
            if (reference == null)
            {
                return null;
            }

            return environment.FindConceptDescriptionById(reference.GetAsIdentifier());
        }

        #endregion

        #region Referable Queries

        //TODO: jtikekar Need to test
        public static IReferable FindReferableByReference(this AasCore.Aas3_0_RC02.Environment environment, Reference reference, int keyIndex = 0, List<ISubmodelElement> submodelElementList = null)
        {
            var keyList = reference.Keys;

            if (keyList == null || keyList.Count == 0)
            {
                return null;
            }

            var firstKeyType = keyList[keyIndex].Type;
            var firstKeyId = keyList[keyIndex].Value;

            switch (firstKeyType)
            {
                case KeyTypes.AssetAdministrationShell:
                    {
                        var aas = environment.FindAasById(firstKeyId);

                        //Not found or already at the end of our search
                        if (aas == null || keyIndex >= keyList.Count - 1)
                        {
                            return aas;
                        }

                        return environment.FindReferableByReference(reference, ++keyIndex);

                    }

                case KeyTypes.ConceptDescription:
                    {
                        return environment.FindConceptDescriptionById(firstKeyId);
                    }

                case KeyTypes.Submodel:
                    {
                        var submodel = environment.FindSubmodelById(firstKeyId);
                        if (submodel == null || keyIndex >= keyList.Count - 1)
                        {
                            return submodel;
                        }

                        return environment.FindReferableByReference(reference, ++keyIndex, submodel.SubmodelElements);
                    }
                case KeyTypes.SubmodelElementCollection:
                case KeyTypes.Property:
                case KeyTypes.SubmodelElement:
                    {
                        if (submodelElementList != null)
                        {
                            var submodelElement = submodelElementList.Where(sme => sme.IdShort.Equals(keyList[keyIndex].Value, StringComparison.OrdinalIgnoreCase)).First();

                            //This is required element
                            if (keyIndex + 1 >= keyList.Count)
                            {
                                return submodelElement;
                            }

                            //Recurse again
                            if (submodelElement != null && submodelElement is SubmodelElementCollection smeColl)
                            {
                                return environment.FindReferableByReference(reference, ++keyIndex, smeColl.Value);
                            }

                            if (submodelElement != null && submodelElement is SubmodelElementList smeList)
                            {
                                return environment.FindReferableByReference(reference, ++keyIndex, smeList.Value);
                            }
                        }
                        break;
                    }
            }

            //Nothing in this environment
            return null;

        }

        #endregion



    }
}
