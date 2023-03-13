using AasCore.Aas3_0_RC02;
using AasxCompatibilityModels;
using AasxServerStandardBib.Extenstions;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using ReferenceTypes = AasCore.Aas3_0_RC02.ReferenceTypes;

namespace Extenstions
{
    public static class ExtendISubmodelElement
    {
        public static string ValueAsText(this ISubmodelElement submodelElement)
        {
            //TODO:Need to check/test this logic again
            if (submodelElement is Property property)
            {
                return property.ValueAsText();
            }

            if (submodelElement is MultiLanguageProperty multiLanguageProperty)
            {
                return multiLanguageProperty.ValueAsText();
            }

            if (submodelElement is AasCore.Aas3_0_RC02.Range range)
            {
                return range.ValueAsText();
            }

            if (submodelElement is File file)
            {
                return file.ValueAsText();
            }

            return "";
        }

        public static Qualifier FindQualifierOfType(this ISubmodelElement submodelElement, string qualifierType)
        {
            if (submodelElement.Qualifiers == null || submodelElement.Qualifiers.Count == 0)
            {
                return null;
            }

            foreach (var qualifier in submodelElement.Qualifiers)
            {
                if (qualifier.Type.Equals(qualifierType, StringComparison.OrdinalIgnoreCase))
                {
                    return qualifier;
                }
            }

            return null;

        }

        public static Reference GetModelReference(this ISubmodelElement submodelElement, bool includeParents = true)
        {
            var keyList = new List<Key>();
            var keyType = ExtensionsUtil.GetKeyType(submodelElement);
            var key = new Key(keyType, submodelElement.IdShort);
            keyList.Add(key);

            //Keys for Parents
            var currentParent = submodelElement.Parent;
            while (includeParents && currentParent != null)
            {
                if (currentParent is IIdentifiable identifiable)
                {
                    var currentParentKey = new Key(ExtensionsUtil.GetKeyType(identifiable), identifiable.Id);
                    keyList.Add(currentParentKey);
                    currentParent = null;
                }
                else if (currentParent is IReferable referable)
                {
                    var currentParentKey = new Key(ExtensionsUtil.GetKeyType(referable), referable.IdShort);
                    keyList.Add(currentParentKey);
                    currentParent = referable.Parent;
                }

            }

            var outputReference = new Reference(ReferenceTypes.ModelReference, keyList);
            outputReference.ReferredSemanticId = submodelElement.SemanticId;
            return outputReference;
        }


        public static IEnumerable<T> FindDeep<T>(this ISubmodelElement submodelElement)
        {
            if (submodelElement is T)
            {
                yield return (T)submodelElement;
            }

            if (submodelElement is SubmodelElementCollection submodelElementCollection)
            {
                foreach (var collectionElement in submodelElementCollection.Value)
                {
                    yield return (T)collectionElement.FindDeep<T>();
                }
            }

            if (submodelElement is AnnotatedRelationshipElement annotatedRelationshipElement)
            {
                foreach (var annotation in annotatedRelationshipElement.Annotations)
                {
                    yield return (T)annotation.FindDeep<T>();
                }
            }

            if (submodelElement is Entity entity)
            {
                foreach (var statement in entity.Statements)
                {
                    yield return (T)statement.FindDeep<T>();
                }
            }

            if (submodelElement is Operation operation)
            {
                var variableCollection = new SubmodelElementCollection();
                variableCollection.Value = new List<ISubmodelElement>();
                foreach (var inputVariable in operation.InputVariables)
                {
                    variableCollection.Value.Add(inputVariable.Value);
                }
                foreach (var outputVariable in operation.OutputVariables)
                {
                    variableCollection.Value.Add(outputVariable.Value);
                }
                foreach (var inOutVariable in operation.InoutputVariables)
                {
                    variableCollection.Value.Add(inOutVariable.Value);
                }
                yield return (T)variableCollection.FindDeep<T>();
            }
        }

        public static ISubmodelElement ConvertFromV10(this ISubmodelElement submodelElement, AdminShellV10.SubmodelElement sourceSubmodelElement, bool shallowCopy = false)
        {
            ISubmodelElement outputSubmodelElement = null;
            if (sourceSubmodelElement != null)
            {
                if (sourceSubmodelElement is AdminShellV10.SubmodelElementCollection collection)
                {
                    var newSmeCollection = new SubmodelElementCollection();
                    outputSubmodelElement = newSmeCollection.ConvertFromV10(collection, shallowCopy);
                }
                else if (sourceSubmodelElement is AdminShellV10.Property sourceProperty)
                {
                    var newProperty = new Property(DataTypeDefXsd.String);
                    outputSubmodelElement = newProperty.ConvertFromV10(sourceProperty);
                }
                else if (sourceSubmodelElement is AdminShellV10.File sourceFile)
                {
                    var newFile = new File("");
                    outputSubmodelElement = newFile.ConvertFromV10(sourceFile);
                }
                else if (sourceSubmodelElement is AdminShellV10.Blob blob)
                {
                    var newBlob = new Blob("");
                    outputSubmodelElement = newBlob.ConvertFromV10(blob);
                }
                else if (sourceSubmodelElement is AdminShellV10.ReferenceElement sourceReferenceElement)
                {
                    outputSubmodelElement = new ReferenceElement();
                }
                else if (sourceSubmodelElement is AdminShellV10.RelationshipElement sourceRelationshipElement)
                {
                    var newFirst = ExtensionsUtil.ConvertReferenceFromV10(sourceRelationshipElement.first, ReferenceTypes.ModelReference);
                    var newSecond = ExtensionsUtil.ConvertReferenceFromV10(sourceRelationshipElement.second, ReferenceTypes.ModelReference);
                    outputSubmodelElement = new RelationshipElement(newFirst, newSecond);
                }

                if (sourceSubmodelElement is AdminShellV10.Operation sourceOperation)
                {
                    var newInputVariables = new List<OperationVariable>();
                    var newOutputVariables = new List<OperationVariable>();
                    if (sourceOperation.valueIn != null)
                    {

                        foreach (var inputVariable in sourceOperation.valueIn)
                        {
                            if (inputVariable.value.submodelElement != null)
                            {
                                ISubmodelElement newSubmodelElement = null;
                                newSubmodelElement = newSubmodelElement.ConvertFromV10(inputVariable.value.submodelElement);
                                var newOpVariable = new OperationVariable(newSubmodelElement);
                                newInputVariables.Add(newOpVariable);
                            }
                        }
                    }
                    if (sourceOperation.valueOut != null)
                    {
                        foreach (var outputVariable in sourceOperation.valueOut)
                        {
                            if (outputVariable.value.submodelElement != null)
                            {
                                ISubmodelElement newSubmodelElement = null;
                                newSubmodelElement = newSubmodelElement.ConvertFromV10(outputVariable.value.submodelElement);
                                var newOpVariable = new OperationVariable(newSubmodelElement);
                                newOutputVariables.Add(newOpVariable);
                            }
                        }
                    }

                    outputSubmodelElement = new Operation(inputVariables: newInputVariables, outputVariables: newOutputVariables);
                }


                outputSubmodelElement.BasicConversionFromV10(sourceSubmodelElement);
            }

            return outputSubmodelElement;
        }

        private static void BasicConversionFromV10(this ISubmodelElement submodelElement, AdminShellV10.SubmodelElement sourceSubmodelElement)
        {
            if (!string.IsNullOrEmpty(sourceSubmodelElement.idShort))
            {
                submodelElement.IdShort = sourceSubmodelElement.idShort;
            }

            if (!string.IsNullOrEmpty(sourceSubmodelElement.category))
            {
                submodelElement.Category = sourceSubmodelElement.category;
            }

            if (sourceSubmodelElement.description != null)
            {
                submodelElement.Description = ExtensionsUtil.ConvertDescriptionFromV10(sourceSubmodelElement.description);
            }

            if (sourceSubmodelElement.semanticId != null)
            {
                var keyList = new List<Key>();
                foreach (var refKey in sourceSubmodelElement.semanticId.Keys)
                {
                    var keyType = Stringification.KeyTypesFromString(refKey.type);
                    if (keyType != null)
                    {
                        keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                    }
                    else
                    {
                        Console.WriteLine($"KeyType value {refKey.type} not found.");
                    }
                }
                submodelElement.SemanticId = new Reference(ReferenceTypes.GlobalReference, keyList);
            }

            if (sourceSubmodelElement.kind != null)
            {
                if (sourceSubmodelElement.kind.IsInstance)
                {
                    submodelElement.Kind = ModelingKind.Instance;
                }
                else
                {
                    submodelElement.Kind = ModelingKind.Template;
                }
            }

            if (sourceSubmodelElement.qualifiers != null && sourceSubmodelElement.qualifiers.Count != 0)
            {
                if (submodelElement.Qualifiers == null && submodelElement.Qualifiers.Count != 0)
                {
                    submodelElement.Qualifiers = new List<Qualifier>();
                }

                foreach (var sourceQualifier in sourceSubmodelElement.qualifiers)
                {
                    var newQualifier = new Qualifier("", DataTypeDefXsd.String);
                    newQualifier = newQualifier.ConvertFromV10(sourceQualifier);
                    submodelElement.Qualifiers.Add(newQualifier);
                }
            }

            if (sourceSubmodelElement.hasDataSpecification != null)
            {
                //TODO: jtikekar : EmbeddedDataSpecification?? (as per old implementation)
                if (submodelElement.EmbeddedDataSpecifications == null)
                {
                    submodelElement.EmbeddedDataSpecifications = new List<EmbeddedDataSpecification>();
                }
                foreach (var dataSpecification in sourceSubmodelElement.hasDataSpecification.reference)
                {
                    submodelElement.EmbeddedDataSpecifications.Add(
                        new EmbeddedDataSpecification(
                            ExtensionsUtil.ConvertReferenceFromV10(dataSpecification, ReferenceTypes.GlobalReference),
                            null));
                }
            }
        }

        public static ISubmodelElement ConvertFromV20(this ISubmodelElement submodelElement, AdminShellV20.SubmodelElement sourceSubmodelElement, bool shallowCopy = false)
        {
            ISubmodelElement outputSubmodelElement = null;
            if (sourceSubmodelElement != null)
            {
                if (sourceSubmodelElement is AdminShellV20.SubmodelElementCollection collection)
                {
                    var newSmeCollection = new SubmodelElementCollection();
                    outputSubmodelElement = newSmeCollection.ConvertFromV20(collection, shallowCopy);
                }
                else if (sourceSubmodelElement is AdminShellV20.Property sourceProperty)
                {
                    var newProperty = new Property(DataTypeDefXsd.String);
                    outputSubmodelElement = newProperty.ConvertFromV20(sourceProperty);
                }
                else if (sourceSubmodelElement is AdminShellV20.MultiLanguageProperty sourceMultiLangProp)
                {
                    var newMultiLangProperty = new MultiLanguageProperty();
                    outputSubmodelElement = newMultiLangProperty.ConvertFromV20(sourceMultiLangProp);
                }
                else if (sourceSubmodelElement is AdminShellV20.Range sourceRange)
                {
                    var newRange = new AasCore.Aas3_0_RC02.Range(DataTypeDefXsd.String);
                    outputSubmodelElement = newRange.ConvertFromV20(sourceRange);
                }
                else if (sourceSubmodelElement is AdminShellV20.File sourceFile)
                {
                    var newFile = new File("");
                    outputSubmodelElement = newFile.ConvertFromV20(sourceFile);
                }
                else if (sourceSubmodelElement is AdminShellV20.Blob blob)
                {
                    var newBlob = new Blob("");
                    outputSubmodelElement = newBlob.ConvertFromV20(blob);
                }
                else if (sourceSubmodelElement is AdminShellV20.ReferenceElement sourceReferenceElement)
                {
                    var newReference = ExtensionsUtil.ConvertReferenceFromV20(sourceReferenceElement.value, ReferenceTypes.ModelReference);
                    outputSubmodelElement = new ReferenceElement(value: newReference);
                }
                else if (sourceSubmodelElement is AdminShellV20.AnnotatedRelationshipElement sourceAnnotedRelationshipElement)
                {
                    var newFirst = ExtensionsUtil.ConvertReferenceFromV20(sourceAnnotedRelationshipElement.first, ReferenceTypes.ModelReference);
                    var newSecond = ExtensionsUtil.ConvertReferenceFromV20(sourceAnnotedRelationshipElement.second, ReferenceTypes.ModelReference);
                    var newAnnotedRelElement = new AnnotatedRelationshipElement(newFirst, newSecond);
                    outputSubmodelElement = newAnnotedRelElement.ConvertAnnotationsFromV20(sourceAnnotedRelationshipElement);
                }
                else if (sourceSubmodelElement is AdminShellV20.RelationshipElement sourceRelationshipElement)
                {
                    var newFirst = ExtensionsUtil.ConvertReferenceFromV20(sourceRelationshipElement.first, ReferenceTypes.ModelReference);
                    var newSecond = ExtensionsUtil.ConvertReferenceFromV20(sourceRelationshipElement.second, ReferenceTypes.ModelReference);
                    outputSubmodelElement = new RelationshipElement(newFirst, newSecond);
                }
                else if (sourceSubmodelElement is AdminShellV20.BasicEvent sourceBasicEvent)
                {
                    var newObserved = ExtensionsUtil.ConvertReferenceFromV20(sourceBasicEvent.observed, ReferenceTypes.ModelReference);

                    outputSubmodelElement = new BasicEventElement(newObserved, Direction.Input, StateOfEvent.Off); //TODO: jtikekar default values of enums
                }
                else if (sourceSubmodelElement is AdminShellV20.Entity sourceEntity)
                {
                    var entityType = Stringification.EntityTypeFromString(sourceEntity.entityType);
                    var newEntity = new Entity((EntityType)entityType);
                    outputSubmodelElement = newEntity.ConvertFromV20(sourceEntity);
                }
                else if (sourceSubmodelElement is AdminShellV20.Operation sourceOperation)
                {
                    var newInputVariables = new List<OperationVariable>();
                    var newOutputVariables = new List<OperationVariable>();
                    var newInOutVariables = new List<OperationVariable>();
                    if (sourceOperation.inputVariable != null)
                    {

                        foreach (var inputVariable in sourceOperation.inputVariable)
                        {
                            if (inputVariable.value.submodelElement != null)
                            {
                                ISubmodelElement newSubmodelElement = null;
                                newSubmodelElement = newSubmodelElement.ConvertFromV20(inputVariable.value.submodelElement);
                                var newOpVariable = new OperationVariable(newSubmodelElement);
                                newInputVariables.Add(newOpVariable);
                            }
                        }
                    }
                    if (sourceOperation.outputVariable != null)
                    {
                        foreach (var outputVariable in sourceOperation.outputVariable)
                        {
                            if (outputVariable.value.submodelElement != null)
                            {
                                ISubmodelElement newSubmodelElement = null;
                                newSubmodelElement = newSubmodelElement.ConvertFromV20(outputVariable.value.submodelElement);
                                var newOpVariable = new OperationVariable(newSubmodelElement);
                                newOutputVariables.Add(newOpVariable);
                            }
                        }
                    }

                    if (sourceOperation.inoutputVariable != null)
                    {
                        foreach (var inOutVariable in sourceOperation.inoutputVariable)
                        {
                            if (inOutVariable.value.submodelElement != null)
                            {
                                ISubmodelElement newSubmodelElement = null;
                                newSubmodelElement = newSubmodelElement.ConvertFromV20(inOutVariable.value.submodelElement);
                                var newOpVariable = new OperationVariable(newSubmodelElement);
                                newInOutVariables.Add(newOpVariable);
                            }
                        }
                    }

                    outputSubmodelElement = new Operation(inputVariables: newInputVariables, outputVariables: newOutputVariables, inoutputVariables: newInOutVariables);
                }

                outputSubmodelElement.BasicConversionFromV20(sourceSubmodelElement);
            }

            return outputSubmodelElement;
        }

        private static void BasicConversionFromV20(this ISubmodelElement submodelElement, AdminShellV20.SubmodelElement sourceSubmodelElement)
        {
            if (!string.IsNullOrEmpty(sourceSubmodelElement.idShort))
            {
                submodelElement.IdShort = sourceSubmodelElement.idShort;
            }

            if (!string.IsNullOrEmpty(sourceSubmodelElement.category))
            {
                submodelElement.Category = sourceSubmodelElement.category;
            }

            if (sourceSubmodelElement.description != null)
            {
                submodelElement.Description = ExtensionsUtil.ConvertDescriptionFromV20(sourceSubmodelElement.description);
            }

            if (sourceSubmodelElement.semanticId != null)
            {
                var keyList = new List<Key>();
                foreach (var refKey in sourceSubmodelElement.semanticId.Keys)
                {
                    var keyType = Stringification.KeyTypesFromString(refKey.type);
                    if (keyType != null)
                    {
                        keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                    }
                    else
                    {
                        Console.WriteLine($"KeyType value {refKey.type} not found.");
                    }
                }
                submodelElement.SemanticId = new Reference(ReferenceTypes.GlobalReference, keyList);
            }

            if (sourceSubmodelElement.kind != null)
            {
                if (sourceSubmodelElement.kind.IsInstance)
                {
                    submodelElement.Kind = ModelingKind.Instance;
                }
                else
                {
                    submodelElement.Kind = ModelingKind.Template;
                }
            }

            if (sourceSubmodelElement.qualifiers != null && sourceSubmodelElement.qualifiers.Count != 0)
            {
                if (submodelElement.Qualifiers == null || submodelElement.Qualifiers.Count == 0)
                {
                    submodelElement.Qualifiers = new List<Qualifier>();
                }

                foreach (var sourceQualifier in sourceSubmodelElement.qualifiers)
                {
                    var newQualifier = new Qualifier("", DataTypeDefXsd.String);
                    newQualifier = newQualifier.ConvertFromV20(sourceQualifier);
                    submodelElement.Qualifiers.Add(newQualifier);
                }
            }

            if (sourceSubmodelElement.hasDataSpecification != null)
            {
                //TODO: jtikekar : EmbeddedDataSpecification?? (as per old implementation)
                if (submodelElement.EmbeddedDataSpecifications == null)
                    submodelElement.EmbeddedDataSpecifications = new List<EmbeddedDataSpecification>();

                //TODO: jtikekar: DataSpecificationContent?? (as per old implementation)
                foreach (var sourceDataSpec in sourceSubmodelElement.hasDataSpecification)
                {
                    submodelElement.EmbeddedDataSpecifications.Add(
                        new EmbeddedDataSpecification(
                            ExtensionsUtil.ConvertReferenceFromV20(sourceDataSpec.dataSpecification, ReferenceTypes.GlobalReference),
                            null));
                }
            }
        }

        public static void RecurseOnReferables(this List<ISubmodelElement> submodelElements, object state, List<IReferable> parents,
                Func<object, List<IReferable>, IReferable, bool> lambda)
        {
            if (lambda == null)
                return;
            if (parents == null)
                parents = new List<IReferable>();

            // over all elements
            foreach (var submodelElement in submodelElements)
            {
                var current = submodelElement;
                if (current == null)
                    continue;

                // call lambda for this element
                // AND decide, if to recurse!
                var goDeeper = lambda(state, parents, current);

                if (goDeeper)
                {
                    // add to parents
                    parents.Add(current);

                    // dive into?
                    if (current is SubmodelElementCollection smc)
                        smc.Value?.RecurseOnReferables(state, parents, lambda);

                    if (current is Entity ent)
                        ent.Statements?.RecurseOnReferables(state, parents, lambda);

                    if (current is Operation operation)
                    {
                        SubmodelElementCollection opVariableCollection = new SubmodelElementCollection();
                        foreach (var inputVariable in operation.InputVariables)
                        {
                            opVariableCollection.Value.Add(inputVariable.Value);
                        }

                        foreach (var outputVariable in operation.OutputVariables)
                        {
                            opVariableCollection.Value.Add(outputVariable.Value);
                        }

                        foreach (var inOutVariable in operation.InoutputVariables)
                        {
                            opVariableCollection.Value.Add(inOutVariable.Value);
                        }

                        opVariableCollection.Value.RecurseOnReferables(state, parents, lambda);
                    }

                    if (current is AnnotatedRelationshipElement annotatedRelationshipElement)
                    {
                        var annotationElements = new List<ISubmodelElement>();
                        foreach (var annotation in annotatedRelationshipElement.Annotations)
                        {
                            annotationElements.Add(annotation);
                        }
                        annotationElements.RecurseOnReferables(state, parents, lambda);
                    }

                    // remove from parents
                    parents.RemoveAt(parents.Count - 1);
                }
            }
        }

        public static void RecurseOnSubmodelElements(this List<ISubmodelElement> submodelElements, object state, List<ISubmodelElement> parents, Action<object, List<ISubmodelElement>, ISubmodelElement> lambda)
        {
            // trivial
            if (lambda == null)
                return;
            if (parents == null)
                parents = new List<ISubmodelElement>();

            // over all elements
            foreach (var smw in submodelElements)
            {
                var current = smw;
                if (current == null)
                    continue;

                // call lambda for this element
                lambda(state, parents, current);

                // add to parents
                parents.Add(current);

                // dive into?
                if (current is SubmodelElementCollection smc)
                    smc.Value?.RecurseOnSubmodelElements(state, parents, lambda);

                if (current is Entity ent)
                    ent.Statements?.RecurseOnSubmodelElements(state, parents, lambda);

                if (current is Operation operation)
                {
                    SubmodelElementCollection opVariableCollection = new SubmodelElementCollection();
                    foreach (var inputVariable in operation.InputVariables)
                    {
                        opVariableCollection.Value.Add(inputVariable.Value);
                    }

                    foreach (var outputVariable in operation.OutputVariables)
                    {
                        opVariableCollection.Value.Add(outputVariable.Value);
                    }

                    foreach (var inOutVariable in operation.InoutputVariables)
                    {
                        opVariableCollection.Value.Add(inOutVariable.Value);
                    }

                    opVariableCollection.Value.RecurseOnSubmodelElements(state, parents, lambda);
                }

                if (current is AnnotatedRelationshipElement annotatedRelationshipElement)
                {
                    var annotationElements = new List<ISubmodelElement>();
                    foreach (var annotation in annotatedRelationshipElement.Annotations)
                    {
                        annotationElements.Add(annotation);
                    }
                    annotationElements.RecurseOnSubmodelElements(state, parents, lambda);
                }

                // remove from parents
                parents.RemoveAt(parents.Count - 1);
            }
        }

        public static IEnumerable<T> FindAllSemanticIdAs<T>(this List<ISubmodelElement> submodelELements, string semanticId) where T : ISubmodelElement
        {
            foreach (var submodelElement in submodelELements)
            {
                if (submodelElement != null && submodelElement is T && submodelElement.SemanticId != null)
                {
                    if (submodelElement.SemanticId.Matches(semanticId))
                    {
                        yield return (T)submodelElement;
                    }
                }
            }
        }

        public static T FindFirstSemanticIdAs<T>(this List<ISubmodelElement> submodelELements, string semanticId) where T : ISubmodelElement
        {
            return submodelELements.FindAllSemanticIdAs<T>(semanticId).FirstOrDefault();
        }

        public static T FindFirstAnySemanticIdAs<T>(this List<ISubmodelElement> submodelELements, string[] semanticIds) where T : ISubmodelElement
        {
            if (semanticIds == null)
                return default;
            foreach (var semanticId in semanticIds)
            {
                var found = submodelELements.FindFirstSemanticIdAs<T>(semanticId);
                if (found != null)
                    return found;
            }
            return default;
        }

        public static IEnumerable<T> FindAllSemanticId<T>(this List<ISubmodelElement> submodelElements, string[] allowedSemanticIds, bool invertedAllowed = false) where T : ISubmodelElement
        {
            if (allowedSemanticIds == null || allowedSemanticIds.Length < 1)
                yield break;

            foreach (var submodelElement in submodelElements)
            {
                if (submodelElement == null || !(submodelElement is T))
                    continue;

                if (submodelElement.SemanticId == null || submodelElement.SemanticId.Keys.Count < 1)
                {
                    if (invertedAllowed)
                        yield return (T)submodelElement;
                    continue;
                }

                var found = false;
                foreach (var semanticId in allowedSemanticIds)
                    if (submodelElement.SemanticId.Matches(semanticId))
                    {
                        found = true;
                        break;
                    }

                if (invertedAllowed)
                    found = !found;

                if (found)
                    yield return (T)submodelElement;
            }
        }

        public static T FindFirstAnySemanticId<T>(this List<ISubmodelElement> submodelElements, string[] allowedSemanticIds, bool invertAllowed = false) where T : ISubmodelElement
        {
            return submodelElements.FindAllSemanticId<T>(allowedSemanticIds, invertAllowed).FirstOrDefault();
        }

    }
}
