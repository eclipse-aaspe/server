using DataTransferObjects.CommonDTOs;
using DataTransferObjects.MetadataDTOs;
using System.Collections.Generic;
using System.Linq;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.MetadataMappers;

/// <inheritdoc cref="IRequestMetadataMapper"/>
public class RequestMetadataMapper : IRequestMetadataMapper
{
    /// <inheritdoc />
    public IClass Map(IMetadataDTO source)
    {
        return source switch
        {
            PropertyMetadata propertyMetadata => Transform(propertyMetadata),
            MultiLanguagePropertyMetadata multiLanguagePropertyMetadata => Transform(multiLanguagePropertyMetadata),
            BasicEventElementMetadata basicEventElementMetadata => Transform(basicEventElementMetadata),
            BlobMetadata blobMetadata => Transform(blobMetadata),
            FileMetadata fileMetadata => Transform(fileMetadata),
            RangeMetadata rangeMetadata => Transform(rangeMetadata),
            ReferenceElementMetadata referenceElementMetadata => Transform(referenceElementMetadata),
            RelationshipElementMetadata relationshipElementMetadata => Transform(relationshipElementMetadata),
            SubmodelElementCollectionMetadata submodelElementCollectionMetadata => Transform(submodelElementCollectionMetadata),
            SubmodelElementListMetadata submodelElementListMetadata => Transform(submodelElementListMetadata),
            AnnotatedRelationshipElementMetadata annotationElementMetadata => Transform(annotationElementMetadata),
            EntityMetadata entityMetadata => Transform(entityMetadata),
            SubmodelMetadata submodelMetadata => Transform(submodelMetadata),
            _ => null
        };
    }

    private ISubmodel Transform(SubmodelMetadata metadata)
    {
        List<ISubmodelElement> submodelElements = null;
        if (metadata.submodelElements != null)
        {
            submodelElements = metadata.submodelElements.Select(element => (ISubmodelElement) Map(element)).ToList();
        }

        return new Submodel(metadata.id, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName),
            TransformLangStringTextTypeList(metadata.description), TransformAdministrationInformation(metadata.administration), metadata.kind,
            TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers),
            TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), submodelElements);
    }


    private  IEntity Transform(EntityMetadata metadata)
    {
        List<ISubmodelElement> statements = null;
        if (metadata.statements != null)
        {
            statements = metadata.statements.Select(element => (ISubmodelElement) Map(element)).ToList();
        }

        return new Entity(metadata.entityType, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName),
            TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds),
            TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), statements);
    }

    private  IAnnotatedRelationshipElement Transform(AnnotatedRelationshipElementMetadata metadata)
    {
        List<IDataElement> annotations = null;
        if (metadata.annotations != null)
        {
            annotations = metadata.annotations.Select(element => (IDataElement) Map(element)).ToList();
        }

        return new AnnotatedRelationshipElement(null, null, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort,
            TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId),
            TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers),
            TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), annotations);
    }

    private  ISubmodelElementList Transform(SubmodelElementListMetadata metadata)
    {
        List<ISubmodelElement> value = null;
        if (metadata.value != null)
        {
            value = metadata.value.Select(element => (ISubmodelElement) Map(element)).ToList();
        }

        return new SubmodelElementList(metadata.typeValueListElement, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort,
            TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId),
            TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers),
            TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), metadata.orderRelevant, TransformReference(metadata.semanticIdListElement),
            metadata.valueTypeListElement, value);
    }

    private  ISubmodelElementCollection Transform(SubmodelElementCollectionMetadata metadata)
    {
        List<ISubmodelElement> value = null;
        if (metadata.value != null)
        {
            value = metadata.value.Select(element => (ISubmodelElement) Map(element)).ToList();
        }

        return new SubmodelElementCollection(TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName),
            TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds),
            TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), value);
    }

    private static IRelationshipElement Transform(RelationshipElementMetadata metadata)
    {
        return new RelationshipElement(null, null, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort,
            TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId),
            TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers),
            TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications));
    }

    private static IReferenceElement Transform(ReferenceElementMetadata metadata)
    {
        return new ReferenceElement(TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName),
            TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds),
            TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications));
    }

    private static IRange Transform(RangeMetadata metadata)
    {
        return new Range(metadata.valueType, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort,
            TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId),
            TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers),
            TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications));
    }

    private static IFile Transform(FileMetadata metadata)
    {
        return new File(null, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName),
            TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds),
            TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications));
    }

    private static IBlob Transform(BlobMetadata metadata)
    {
        return new Blob(null, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName),
            TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds),
            TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications));
    }

    private static IBasicEventElement Transform(BasicEventElementMetadata metadata)
    {
        return new BasicEventElement(null, metadata.direction, metadata.state, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort,
            TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId),
            TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers),
            TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), metadata.messageTopic, TransformReference(metadata.messageBroker), metadata.lastUpdate,
            metadata.minInterval, metadata.maxInterval);
    }

    private static IMultiLanguageProperty Transform(MultiLanguagePropertyMetadata multiLanguagePropertyMetadata)
    {
        return new MultiLanguageProperty(TransformExtensions(multiLanguagePropertyMetadata.extensions), multiLanguagePropertyMetadata.category,
            multiLanguagePropertyMetadata.idShort, TransformLangStringNameTypeList(multiLanguagePropertyMetadata.displayName),
            TransformLangStringTextTypeList(multiLanguagePropertyMetadata.description), TransformReference(multiLanguagePropertyMetadata.semanticId),
            TransformReferenceList(multiLanguagePropertyMetadata.supplementalSemanticIds), TransformQualifierList(multiLanguagePropertyMetadata.qualifiers),
            TransformEmbeddedDataSpecList(multiLanguagePropertyMetadata.embeddedDataSpecifications));
    }

    private static IProperty Transform(PropertyMetadata propertyMetadata)
    {
        return new Property(propertyMetadata.valueType, TransformExtensions(propertyMetadata.extensions), propertyMetadata.category, propertyMetadata.idShort,
            TransformLangStringNameTypeList(propertyMetadata.displayName), TransformLangStringTextTypeList(propertyMetadata.description),
            TransformReference(propertyMetadata.semanticId), TransformReferenceList(propertyMetadata.supplementalSemanticIds), TransformQualifierList(propertyMetadata.qualifiers),
            TransformEmbeddedDataSpecList(propertyMetadata.embeddedDataSpecifications));
    }

    private static List<ILangStringNameType> TransformLangStringNameTypeList(IEnumerable<LangStringNameTypeDTO> langStringNameTypeList)
    {
        return langStringNameTypeList?.Select(TransformLangStringNameType).ToList();
    }

    private static ILangStringNameType TransformLangStringNameType(LangStringNameTypeDTO langString)
    {
        return new LangStringNameType(langString.language, langString.text);
    }

    private static List<ILangStringTextType> TransformLangStringTextTypeList(IEnumerable<LangStringTextTypeDTO> langStringTextTypeList)
    {
        return langStringTextTypeList?.Select(TransformLangStringTextType).ToList();
    }

    private static ILangStringTextType TransformLangStringTextType(LangStringTextTypeDTO langString)
    {
        return new LangStringTextType(langString.language, langString.text);
    }

    private static List<IExtension> TransformExtensions(IEnumerable<ExtensionDTO> extensions)
    {
        return extensions?.Select(extension => new Extension(extension.name, TransformReference(extension.semanticId), TransformReferenceList(extension.supplementalSemanticIds),
            extension.valueType, extension.value)).Cast<IExtension>().ToList();
    }

    private static List<IReference> TransformReferenceList(IEnumerable<ReferenceDTO> references)
    {
        return references?.Select(TransformReference).ToList();
    }

    private static IReference TransformReference(ReferenceDTO referenceDTO)
    {
        return referenceDTO == null ? null : new Reference(referenceDTO.type, TransformKeys(referenceDTO.keys), TransformReference(referenceDTO.referredSemanticId));
    }

    private static List<IKey> TransformKeys(IEnumerable<KeyDTO> keys)
    {
        return keys?.Select(key => new Key(key.type, key.value)).Cast<IKey>().ToList();
    }

    private static List<IQualifier> TransformQualifierList(IEnumerable<QualifierDTO> qualifiers)
    {
        return qualifiers?.Select(TransformQualifier).ToList();
    }

    private static IQualifier TransformQualifier(QualifierDTO qualifierDTO)
    {
        return qualifierDTO == null
            ? null
            : new Qualifier(qualifierDTO.type, qualifierDTO.valueType, TransformReference(qualifierDTO.semanticId), TransformReferenceList(qualifierDTO.supplementalSemanticIds),
                qualifierDTO.kind, qualifierDTO.value, TransformReference(qualifierDTO.valueId));
    }

    private static List<IEmbeddedDataSpecification> TransformEmbeddedDataSpecList(IEnumerable<EmbeddedDataSpecificationDTO> embeddedDataSpecifications)
    {
        return embeddedDataSpecifications?.Select(TransformEmbeddedDataSpecification).ToList();
    }

    private static IEmbeddedDataSpecification TransformEmbeddedDataSpecification(EmbeddedDataSpecificationDTO embDataSpecDTO)
    {
        return embDataSpecDTO == null ? null : new EmbeddedDataSpecification(TransformReference(embDataSpecDTO.dataSpecification), null);
    }

    private static IAdministrativeInformation TransformAdministrationInformation(AdministrativeInformationDTO metadata)
    {
        return metadata == null
            ? null
            : new AdministrativeInformation(TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), metadata.version, metadata.revision,
                TransformReference(metadata.creator), metadata.templateId);
    }
}