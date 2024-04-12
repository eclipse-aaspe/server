using DataTransferObjects;
using DataTransferObjects.CommonDTOs;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.Exceptions;
using System.Collections.Generic;
using System.Linq;
using static AasCore.Aas3_0.Visitation;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;

internal class ResponseValueTransformer : ITransformer<IDTO>, IResponseValueTransformer
{
    public IDTO Transform(IClass that)
    {
        return that?.Transform(this);
    }

    public IDTO TransformAdministrativeInformation(IAdministrativeInformation that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformAnnotatedRelationshipElement(IAnnotatedRelationshipElement that)
    {
        List<ISubmodelElementValue> annotations = null;
        if (that is {Annotations: not null})
        {
            annotations = that.Annotations.Select(element => (ISubmodelElementValue) Transform(element)).ToList();
        }
        else
        {
            return null;
        }

        return new AnnotatedRelationshipElementValue(that.IdShort ?? string.Empty, (ReferenceDTO) Transform(that.First), (ReferenceDTO) Transform(that.Second), annotations);
    }

    public IDTO TransformAssetAdministrationShell(IAssetAdministrationShell that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformAssetInformation(IAssetInformation that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformBasicEventElement(IBasicEventElement that)
    {
        return new BasicEventElementValue(that.IdShort ?? string.Empty, (ReferenceDTO) Transform(that.Observed));
    }

    public IDTO TransformBlob(IBlob that)
    {
        return new BlobValue(that.IdShort ?? string.Empty, that.ContentType, that.Value);
    }

    public IDTO TransformCapability(ICapability that)
    {
        throw new InvalidSerializationModifierException("ValueOnly", that.GetType().Name);
    }

    public IDTO TransformConceptDescription(IConceptDescription that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformDataSpecificationIec61360(IDataSpecificationIec61360 that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformEmbeddedDataSpecification(IEmbeddedDataSpecification that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformEntity(IEntity that)
    {
        List<ISubmodelElementValue> statements = null;
        if (that.Statements != null)
        {
            statements = that.Statements.Select(element => (ISubmodelElementValue) Transform(element)).ToList();
        }

        return new EntityValue(that.IdShort ?? string.Empty, that.EntityType, statements, that.GlobalAssetId);
    }

    public IDTO TransformEnvironment(IEnvironment that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformEventPayload(IEventPayload that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformExtension(IExtension that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformFile(IFile that)
    {
        return new FileValue(that.IdShort ?? string.Empty, that.ContentType, that.Value);
    }

    private List<KeyDTO> TransformKeyList(IEnumerable<IKey> keyList)
    {
        return keyList?.Select(key => (KeyDTO) Transform(key)).ToList();
    }

    public IDTO TransformKey(IKey that)
    {
        return new KeyDTO(that.Type, that.Value);
    }

    public IDTO TransformLangStringDefinitionTypeIec61360(ILangStringDefinitionTypeIec61360 that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformLangStringNameType(ILangStringNameType that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformLangStringPreferredNameTypeIec61360(ILangStringPreferredNameTypeIec61360 that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformLangStringShortNameTypeIec61360(ILangStringShortNameTypeIec61360 that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformLangStringTextType(ILangStringTextType that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformLevelType(ILevelType that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformMultiLanguageProperty(IMultiLanguageProperty that)
    {
        var langStrings = new List<KeyValuePair<string, string>>();
        if (that.Value != null)
        {
            langStrings.AddRange(that.Value.Select(langString => new KeyValuePair<string, string>(langString.Language, langString.Text)));
        }

        return new MultiLanguagePropertyValue(that.IdShort ?? string.Empty, langStrings);
    }

    public IDTO TransformOperation(IOperation that)
    {
        List<ISubmodelElementValue> inputVariables = null;
        List<ISubmodelElementValue> outputVariables = null;
        List<ISubmodelElementValue> inoutputVariables = null;
        if (that.InputVariables != null)
        {
            inputVariables = that.InputVariables.Select(inputVariable => (ISubmodelElementValue) Transform(inputVariable)).ToList();
        }

        if (that.OutputVariables != null)
        {
            outputVariables = that.OutputVariables.Select(outputVariable => (ISubmodelElementValue) Transform(outputVariable)).ToList();
        }

        if (that.InoutputVariables != null)
        {
            inoutputVariables = that.InoutputVariables.Select(inoutputVariable => (ISubmodelElementValue) Transform(inoutputVariable)).ToList();
        }

        return new OperationValue(that.IdShort ?? string.Empty, inputVariables, outputVariables, inoutputVariables);
    }

    public IDTO TransformOperationVariable(IOperationVariable that)
    {
        return Transform(that.Value);
    }

    public IDTO TransformProperty(IProperty that)
    {
        return new PropertyValue(that.IdShort ?? string.Empty, that.Value);
    }

    public IDTO TransformQualifier(IQualifier that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformRange(IRange that)
    {
        return new RangeValue(that.IdShort ?? string.Empty, that.Min, that.Max);
    }

    public IDTO TransformReference(IReference that)
    {
        if (that.ReferredSemanticId != null)
        {
            return new ReferenceDTO(that.Type, TransformKeyList(that.Keys), (ReferenceDTO) Transform(
                that.ReferredSemanticId));
        }

        return null;
    }

    public IDTO TransformReferenceElement(IReferenceElement that)
    {
        return that.Value != null ? new ReferenceElementValue(that.IdShort ?? string.Empty, (ReferenceDTO) Transform(that.Value)) : null;
    }

    public IDTO TransformRelationshipElement(IRelationshipElement that)
    {
        return new RelationshipElementValue(that.IdShort ?? string.Empty, (ReferenceDTO) Transform(that.First), (ReferenceDTO) Transform(that.Second));
    }

    public IDTO TransformResource(IResource that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformSpecificAssetId(ISpecificAssetId that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformSubmodel(ISubmodel that)
    {
        List<ISubmodelElementValue> submodelElements = null;
        if (that.SubmodelElements != null)
        {
            submodelElements = that.SubmodelElements.Select(element => (ISubmodelElementValue) Transform(element)).ToList();
        }

        return new SubmodelValue(submodelElements);
    }

    public IDTO TransformSubmodelElementCollection(ISubmodelElementCollection that)
    {
        List<ISubmodelElementValue> value = null;
        if (that.Value != null)
        {
            value = that.Value.Select(element => (ISubmodelElementValue) Transform(element)).ToList();
        }

        return new SubmodelElementCollectionValue(that.IdShort ?? string.Empty, value);
    }

    public IDTO TransformSubmodelElementList(ISubmodelElementList that)
    {
        List<ISubmodelElementValue> value = null;
        if (that.Value != null)
        {
            value = that.Value.Select(element => (ISubmodelElementValue) Transform(element)).ToList();
        }

        return new SubmodelElementListValue(that.IdShort ?? string.Empty, value);
    }

    public IDTO TransformValueList(IValueList that)
    {
        throw new System.NotImplementedException();
    }

    public IDTO TransformValueReferencePair(IValueReferencePair that)
    {
        throw new System.NotImplementedException();
    }
}