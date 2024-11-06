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

using DataTransferObjects.CommonDTOs;
using DataTransferObjects.MetadataDTOs;
using IO.Swagger.Lib.V3.Exceptions;
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

            throw new InvalidSerializationModifierException("metadata", source.GetType().Name);
        }

        private static ISubmodel Transform(SubmodelMetadata metadata)
        {
            return new Submodel(metadata.Id, TransformExtensions(metadata.Extensions), metadata.Category, metadata.IdShort, TransformLangStringNameTypeList(metadata.DisplayName), TransformLangStringTextTypeList(metadata.Description), TransformAdministrationInformation(metadata.Administration), metadata.Kind, TransformReference(metadata.SemanticId), TransformReferenceList(metadata.SupplementalSemanticIds), TransformQualifierList(metadata.Qualifiers), TransformEmbeddedDataSpecList(metadata.EmbeddedDataSpecifications));
        }



        private static IEntity Transform(EntityMetadata metadata)
        {
            return new Entity(metadata.EntityType, TransformExtensions(metadata.Extensions), metadata.Category, metadata.IdShort, TransformLangStringNameTypeList(metadata.DisplayName), TransformLangStringTextTypeList(metadata.Description), TransformReference(metadata.SemanticId), TransformReferenceList(metadata.SupplementalSemanticIds), TransformQualifierList(metadata.Qualifiers), TransformEmbeddedDataSpecList(metadata.EmbeddedDataSpecifications));
        }

        private static IAnnotatedRelationshipElement Transform(AnnotatedRelationshipElementMetadata metadata)
        {
            return new AnnotatedRelationshipElement(null, null, TransformExtensions(metadata.Extensions), metadata.Category, metadata.IdShort, TransformLangStringNameTypeList(metadata.DisplayName), TransformLangStringTextTypeList(metadata.Description), TransformReference(metadata.SemanticId), TransformReferenceList(metadata.SupplementalSemanticIds), TransformQualifierList(metadata.Qualifiers), TransformEmbeddedDataSpecList(metadata.EmbeddedDataSpecifications));
        }

        private static ISubmodelElementList Transform(SubmodelElementListMetadata metadata)
        {
            return new SubmodelElementList(metadata.TypeValueListElement, TransformExtensions(metadata.Extensions), metadata.Category, metadata.IdShort, TransformLangStringNameTypeList(metadata.DisplayName), TransformLangStringTextTypeList(metadata.Description), TransformReference(metadata.SemanticId), TransformReferenceList(metadata.SupplementalSemanticIds), TransformQualifierList(metadata.Qualifiers), TransformEmbeddedDataSpecList(metadata.EmbeddedDataSpecifications), metadata.OrderRelevant, TransformReference(metadata.SemanticIdListElement), metadata.ValueTypeListElement);
        }

        private static ISubmodelElementCollection Transform(SubmodelElementCollectionMetadata metadata)
        {
            return new SubmodelElementCollection(TransformExtensions(metadata.Extensions), metadata.Category, metadata.IdShort, TransformLangStringNameTypeList(metadata.DisplayName), TransformLangStringTextTypeList(metadata.Description), TransformReference(metadata.SemanticId), TransformReferenceList(metadata.SupplementalSemanticIds), TransformQualifierList(metadata.Qualifiers), TransformEmbeddedDataSpecList(metadata.EmbeddedDataSpecifications));
        }

        private static IRelationshipElement Transform(RelationshipElementMetadata metadata)
        {
            return new RelationshipElement(null, null, TransformExtensions(metadata.Extensions), metadata.Category, metadata.IdShort, TransformLangStringNameTypeList(metadata.DisplayName), TransformLangStringTextTypeList(metadata.Description), TransformReference(metadata.SemanticId), TransformReferenceList(metadata.SupplementalSemanticIds), TransformQualifierList(metadata.Qualifiers), TransformEmbeddedDataSpecList(metadata.EmbeddedDataSpecifications));
        }

        private static IReferenceElement Transform(ReferenceElementMetadata metadata)
        {
            return new ReferenceElement(TransformExtensions(metadata.Extensions), metadata.Category, metadata.IdShort, TransformLangStringNameTypeList(metadata.DisplayName), TransformLangStringTextTypeList(metadata.Description), TransformReference(metadata.SemanticId), TransformReferenceList(metadata.SupplementalSemanticIds), TransformQualifierList(metadata.Qualifiers), TransformEmbeddedDataSpecList(metadata.EmbeddedDataSpecifications));
        }

        private static IRange Transform(RangeMetadata metadata)
        {
            return new AasCore.Aas3_0.Range(metadata.ValueType, TransformExtensions(metadata.Extensions), metadata.Category, metadata.IdShort, TransformLangStringNameTypeList(metadata.DisplayName), TransformLangStringTextTypeList(metadata.Description), TransformReference(metadata.SemanticId), TransformReferenceList(metadata.SupplementalSemanticIds), TransformQualifierList(metadata.Qualifiers), TransformEmbeddedDataSpecList(metadata.EmbeddedDataSpecifications));
        }

        private static IFile Transform(FileMetadata metadata)
        {
            return new File(null, TransformExtensions(metadata.Extensions), metadata.Category, metadata.IdShort, TransformLangStringNameTypeList(metadata.DisplayName), TransformLangStringTextTypeList(metadata.Description), TransformReference(metadata.SemanticId), TransformReferenceList(metadata.SupplementalSemanticIds), TransformQualifierList(metadata.Qualifiers), TransformEmbeddedDataSpecList(metadata.EmbeddedDataSpecifications));
        }

        private static IBlob Transform(BlobMetadata metadata)
        {
            return new Blob(null, TransformExtensions(metadata.Extensions), metadata.Category, metadata.IdShort, TransformLangStringNameTypeList(metadata.DisplayName), TransformLangStringTextTypeList(metadata.Description), TransformReference(metadata.SemanticId), TransformReferenceList(metadata.SupplementalSemanticIds), TransformQualifierList(metadata.Qualifiers), TransformEmbeddedDataSpecList(metadata.EmbeddedDataSpecifications));
        }

        private static IBasicEventElement Transform(BasicEventElementMetadata metadata)
        {
            return new BasicEventElement(null, metadata.Direction, metadata.State, TransformExtensions(metadata.Extensions), metadata.Category, metadata.IdShort, TransformLangStringNameTypeList(metadata.DisplayName), TransformLangStringTextTypeList(metadata.Description), TransformReference(metadata.SemanticId), TransformReferenceList(metadata.SupplementalSemanticIds), TransformQualifierList(metadata.Qualifiers), TransformEmbeddedDataSpecList(metadata.EmbeddedDataSpecifications), metadata.MessageTopic, TransformReference(metadata.MessageBroker), metadata.LastUpdate, metadata.MinInterval, metadata.MaxInterval);
        }

        private static IMultiLanguageProperty Transform(MultiLanguagePropertyMetadata multiLanguagePropertyMetadata)
        {
            return new MultiLanguageProperty(TransformExtensions(multiLanguagePropertyMetadata.Extensions), multiLanguagePropertyMetadata.Category, multiLanguagePropertyMetadata.IdShort, TransformLangStringNameTypeList(multiLanguagePropertyMetadata.DisplayName), TransformLangStringTextTypeList(multiLanguagePropertyMetadata.Description), TransformReference(multiLanguagePropertyMetadata.SemanticId), TransformReferenceList(multiLanguagePropertyMetadata.SupplementalSemanticIds), TransformQualifierList(multiLanguagePropertyMetadata.Qualifiers), TransformEmbeddedDataSpecList(multiLanguagePropertyMetadata.EmbeddedDataSpecifications));
        }

        private static IProperty Transform(PropertyMetadata propertyMetadata)
        {
            return new Property(propertyMetadata.ValueType, TransformExtensions(propertyMetadata.Extensions), propertyMetadata.Category, propertyMetadata.IdShort, TransformLangStringNameTypeList(propertyMetadata.DisplayName), TransformLangStringTextTypeList(propertyMetadata.Description), TransformReference(propertyMetadata.SemanticId), TransformReferenceList(propertyMetadata.SupplementalSemanticIds), TransformQualifierList(propertyMetadata.Qualifiers), TransformEmbeddedDataSpecList(propertyMetadata.EmbeddedDataSpecifications));
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
            return new LangStringNameType(langString.Language, langString.Text);
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
            return new LangStringTextType(langString.Language, langString.Text);
        }

        private static List<IExtension> TransformExtensions(List<ExtensionDTO> extensions)
        {
            if (extensions == null)
                return null;
            var result = new List<IExtension>();
            foreach (var extension in extensions)
            {
                result.Add(new Extension(extension.Name, TransformReference(extension.SemanticId), TransformReferenceList(extension.SupplementalSemanticIds), extension.ValueType, extension.Value));
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
            return new Reference(referenceDTO.Type, TransformKeys(referenceDTO.Keys), TransformReference(referenceDTO.ReferredSemanticId));
        }

        private static List<IKey> TransformKeys(List<KeyDTO> keys)
        {
            if (keys == null) return null;

            var result = new List<IKey>();
            foreach (var key in keys)
            {
                result.Add(new Key(key.Type, key.Value));
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
            return new Qualifier(qualifierDTO.Type, qualifierDTO.ValueType, TransformReference(qualifierDTO.SemanticId), TransformReferenceList(qualifierDTO.SupplementalSemanticIds), qualifierDTO.Kind, qualifierDTO.Value, TransformReference(qualifierDTO.ValueId));
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
            return new EmbeddedDataSpecification(TransformReference(embDataSpecDTO.DataSpecification), null);
        }

        private static IAdministrativeInformation TransformAdministrationInformation(AdministrativeInformationDTO metadata)
        {
            if (metadata == null) return null;
            return new AdministrativeInformation(TransformEmbeddedDataSpecList(metadata.EmbeddedDataSpecifications), metadata.Version, metadata.Revision, TransformReference(metadata.Creator), metadata.TemplateId);
        }
    }
}
