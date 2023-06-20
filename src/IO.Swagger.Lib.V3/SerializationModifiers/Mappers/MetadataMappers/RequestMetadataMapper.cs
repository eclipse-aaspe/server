using DataTransferObjects.CommonDTOs;
using DataTransferObjects.MetadataDTOs;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.MetadataMappers
{
    public static class RequestMetadataMapper
    {
        public static IClass Map(IMetadataDTO source)
        {
            if (source is PropertyMetadata propertyMetadata)
                return Transform(propertyMetadata);
            if (source is MultiLanguagePropertyMetadata multiLanguagePropertyMetadata)
                return Transform(multiLanguagePropertyMetadata);
            if (source is BasicEventElementMetadata basicEventElementMetadata)
                return Transform(basicEventElementMetadata);
            if (source is BlobMetadata blobMetadata)
                return Transform(blobMetadata);
            if (source is FileMetadata fileMetadata)
                return Transform(fileMetadata);
            if (source is RangeMetadata rangeMetadata)
                return Transform(rangeMetadata);
            if (source is ReferenceElementMetadata referenceElementMetadata)
                return Transform(referenceElementMetadata);
            if (source is RelationshipElementMetadata relationshipElementMetadata)
                return Transform(relationshipElementMetadata);
            if (source is SubmodelElementCollectionMetadata submodelElementCollectionMetadata)
                return Transform(submodelElementCollectionMetadata);
            if (source is SubmodelElementListMetadata submodelElementListMetadata)
                return Transform(submodelElementListMetadata);
            if (source is AnnotatedRelationshipElementMetadata annotationElementMetadata)
                return Transform(annotationElementMetadata);
            if (source is EntityMetadata entityMetadata)
                return Transform(entityMetadata);
            if (source is SubmodelMetadata submodelMetadata)
                return Transform(submodelMetadata);

            return null;
        }

        private static ISubmodel Transform(SubmodelMetadata metadata)
        {
            List<ISubmodelElement> submodelElements = null;
            if (metadata.submodelElements != null)
            {
                submodelElements = new List<ISubmodelElement>();
                foreach (var element in metadata.submodelElements)
                {
                    submodelElements.Add((ISubmodelElement)Map(element));
                }
            }
            return new Submodel(metadata.id, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformAdministrationInformation(metadata.administration), metadata.kind, TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), submodelElements);
        }



        private static IEntity Transform(EntityMetadata metadata)
        {
            List<ISubmodelElement> statements = null;
            if (metadata.statements != null)
            {
                statements = new List<ISubmodelElement>();
                foreach (var element in metadata.statements)
                {
                    statements.Add((ISubmodelElement)Map(element));
                }
            }
            return new Entity(metadata.entityType, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), statements);
        }

        private static IAnnotatedRelationshipElement Transform(AnnotatedRelationshipElementMetadata metadata)
        {
            List<IDataElement> annotations = null;
            if (metadata.annotations != null)
            {
                annotations = new List<IDataElement>();
                foreach (var element in metadata.annotations)
                {
                    annotations.Add((IDataElement)Map(element));
                }
            }
            return new AnnotatedRelationshipElement(null, null, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), annotations);
        }

        private static ISubmodelElementList Transform(SubmodelElementListMetadata metadata)
        {
            List<ISubmodelElement> value = null;
            if (metadata.value != null)
            {
                value = new List<ISubmodelElement>();
                foreach (var element in metadata.value)
                {
                    value.Add((ISubmodelElement)Map(element));
                }
            }
            return new SubmodelElementList(metadata.typeValueListElement, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), metadata.orderRelevant, TransformReference(metadata.semanticIdListElement), metadata.valueTypeListElement, value);
        }

        private static ISubmodelElementCollection Transform(SubmodelElementCollectionMetadata metadata)
        {
            List<ISubmodelElement> value = null;
            if (metadata.value != null)
            {
                value = new List<ISubmodelElement>();
                foreach (var element in metadata.value)
                {
                    value.Add((ISubmodelElement)Map(element));
                }
            }
            return new SubmodelElementCollection(TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), value);
        }

        private static IRelationshipElement Transform(RelationshipElementMetadata metadata)
        {
            return new RelationshipElement(null, null, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications));
        }

        private static IReferenceElement Transform(ReferenceElementMetadata metadata)
        {
            return new ReferenceElement(TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications));
        }

        private static IRange Transform(RangeMetadata metadata)
        {
            return new AasCore.Aas3_0.Range(metadata.valueType, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications));
        }

        private static IFile Transform(FileMetadata metadata)
        {
            return new File(null, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications));
        }

        private static IBlob Transform(BlobMetadata metadata)
        {
            return new Blob(null, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications));
        }

        private static IBasicEventElement Transform(BasicEventElementMetadata metadata)
        {
            return new BasicEventElement(null, metadata.direction, metadata.state, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), metadata.messageTopic, TransformReference(metadata.messageBroker), metadata.lastUpdate, metadata.minInterval, metadata.maxInterval);
        }

        private static IMultiLanguageProperty Transform(MultiLanguagePropertyMetadata multiLanguagePropertyMetadata)
        {
            return new MultiLanguageProperty(TransformExtensions(multiLanguagePropertyMetadata.extensions), multiLanguagePropertyMetadata.category, multiLanguagePropertyMetadata.idShort, TransformLangStringNameTypeList(multiLanguagePropertyMetadata.displayName), TransformLangStringTextTypeList(multiLanguagePropertyMetadata.description), TransformReference(multiLanguagePropertyMetadata.semanticId), TransformReferenceList(multiLanguagePropertyMetadata.supplementalSemanticIds), TransformQualifierList(multiLanguagePropertyMetadata.qualifiers), TransformEmbeddedDataSpecList(multiLanguagePropertyMetadata.embeddedDataSpecifications));
        }

        private static IProperty Transform(PropertyMetadata propertyMetadata)
        {
            return new Property(propertyMetadata.valueType, TransformExtensions(propertyMetadata.extensions), propertyMetadata.category, propertyMetadata.idShort, TransformLangStringNameTypeList(propertyMetadata.displayName), TransformLangStringTextTypeList(propertyMetadata.description), TransformReference(propertyMetadata.semanticId), TransformReferenceList(propertyMetadata.supplementalSemanticIds), TransformQualifierList(propertyMetadata.qualifiers), TransformEmbeddedDataSpecList(propertyMetadata.embeddedDataSpecifications));
        }

        private static List<ILangStringNameType> TransformLangStringNameTypeList(List<LangStringNameTypeDTO> langStringNameTypeList)
        {
            if (langStringNameTypeList == null) return null;
            var result = new List<ILangStringNameType>();
            foreach (var langString in langStringNameTypeList)
            {
                result.Add(TransformLangStringNameType(langString));
            }

            return result;
        }

        private static ILangStringNameType TransformLangStringNameType(LangStringNameTypeDTO langString)
        {
            return new LangStringNameType(langString.language, langString.text);
        }

        private static List<ILangStringTextType> TransformLangStringTextTypeList(List<LangStringTextTypeDTO> langStringTextTypeList)
        {
            if (langStringTextTypeList == null) return null;
            var result = new List<ILangStringTextType>();
            foreach (var langString in langStringTextTypeList)
            {
                result.Add(TransformLangStringTextType(langString));
            }

            return result;
        }

        private static ILangStringTextType TransformLangStringTextType(LangStringTextTypeDTO langString)
        {
            return new LangStringTextType(langString.language, langString.text);
        }

        private static List<IExtension> TransformExtensions(List<ExtensionDTO> extensions)
        {
            if (extensions == null)
                return null;
            var result = new List<IExtension>();
            foreach (var extension in extensions)
            {
                result.Add(new Extension(extension.name, TransformReference(extension.semanticId), TransformReferenceList(extension.supplementalSemanticIds), extension.valueType, extension.value));
            }

            return result;
        }

        private static List<IReference> TransformReferenceList(List<ReferenceDTO> references)
        {
            if (references == null) return null;
            var result = new List<IReference>();
            foreach (var reference in references)
            {
                result.Add(TransformReference(reference));
            }

            return result;
        }

        private static IReference TransformReference(ReferenceDTO referenceDTO)
        {
            if (referenceDTO == null)
                return null;
            return new Reference(referenceDTO.type, TransformKeys(referenceDTO.keys), TransformReference(referenceDTO.referredSemanticId));
        }

        private static List<IKey> TransformKeys(List<KeyDTO> keys)
        {
            if (keys == null) return null;

            var result = new List<IKey>();
            foreach (var key in keys)
            {
                result.Add(new Key(key.type, key.value));
            }

            return result;
        }

        private static List<IQualifier> TransformQualifierList(List<QualifierDTO> qualifiers)
        {
            if (qualifiers == null) return null;
            var result = new List<IQualifier>();
            foreach (var qualifier in qualifiers)
            {
                result.Add(TransformQualifier(qualifier));
            }

            return result;
        }

        private static IQualifier TransformQualifier(QualifierDTO qualifierDTO)
        {
            if (qualifierDTO == null)
                return null;
            return new Qualifier(qualifierDTO.type, qualifierDTO.valueType, TransformReference(qualifierDTO.semanticId), TransformReferenceList(qualifierDTO.supplementalSemanticIds), qualifierDTO.kind, qualifierDTO.value, TransformReference(qualifierDTO.valueId));
        }

        private static List<IEmbeddedDataSpecification> TransformEmbeddedDataSpecList(List<EmbeddedDataSpecificationDTO> embeddedDataSpecifications)
        {
            if (embeddedDataSpecifications == null) return null;
            var result = new List<IEmbeddedDataSpecification>();
            foreach (var embDataSpec in embeddedDataSpecifications)
            {
                result.Add(TransformEmbeddedDataSpecification(embDataSpec));
            }

            return result;
        }

        private static IEmbeddedDataSpecification TransformEmbeddedDataSpecification(EmbeddedDataSpecificationDTO embDataSpecDTO)
        {
            if (embDataSpecDTO == null)
                return null;
            return new EmbeddedDataSpecification(TransformReference(embDataSpecDTO.dataSpecification), null);
        }

        private static IAdministrativeInformation TransformAdministrationInformation(AdministrativeInformationDTO metadata)
        {
            if (metadata == null) return null;
            return new AdministrativeInformation(TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), metadata.version, metadata.revision, TransformReference(metadata.creator), metadata.templateId);
        }
    }
}
