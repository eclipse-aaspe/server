/********************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
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
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.MetadataMappers;

using System.Linq;

public static class RequestMetadataMapper
{
    public static IClass? Map(IMetadataDTO source) =>
        source switch
        {
            PropertyMetadata propertyMetadata                                   => Transform(propertyMetadata),
            MultiLanguagePropertyMetadata multiLanguagePropertyMetadata         => Transform(multiLanguagePropertyMetadata),
            BasicEventElementMetadata basicEventElementMetadata                 => Transform(basicEventElementMetadata),
            BlobMetadata blobMetadata                                           => Transform(blobMetadata),
            FileMetadata fileMetadata                                           => Transform(fileMetadata),
            RangeMetadata rangeMetadata                                         => Transform(rangeMetadata),
            ReferenceElementMetadata referenceElementMetadata                   => Transform(referenceElementMetadata),
            RelationshipElementMetadata relationshipElementMetadata             => Transform(relationshipElementMetadata),
            SubmodelElementCollectionMetadata submodelElementCollectionMetadata => Transform(submodelElementCollectionMetadata),
            SubmodelElementListMetadata submodelElementListMetadata             => Transform(submodelElementListMetadata),
            AnnotatedRelationshipElementMetadata annotationElementMetadata      => Transform(annotationElementMetadata),
            EntityMetadata entityMetadata                                       => Transform(entityMetadata),
            SubmodelMetadata submodelMetadata                                   => Transform(submodelMetadata),
            _                                                                   => null
        };

    private static ISubmodel? Transform(SubmodelMetadata metadata)
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

        return new Submodel(metadata.id, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName),
                            TransformLangStringTextTypeList(metadata.description), TransformAdministrationInformation(metadata.administration), metadata.kind,
                            TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers),
                            TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), submodelElements);
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

        return new Entity(metadata.entityType, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort,
                          TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId),
                          TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers),
                          TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), statements);
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

        return new AnnotatedRelationshipElement(null, null, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort,
                                                TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description),
                                                TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds),
                                                TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), annotations);
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

        return new SubmodelElementList(metadata.typeValueListElement, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort,
                                       TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description),
                                       TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds),
                                       TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), metadata.orderRelevant,
                                       TransformReference(metadata.semanticIdListElement), metadata.valueTypeListElement, value);
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

        return new SubmodelElementCollection(TransformExtensions(metadata.extensions), metadata.category, metadata.idShort,
                                             TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description),
                                             TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds),
                                             TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), value);
    }

    private static IRelationshipElement Transform(RelationshipElementMetadata metadata)
    {
        return new RelationshipElement(null, null, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort,
                                       TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description),
                                       TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds),
                                       TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications));
    }

    private static IReferenceElement Transform(ReferenceElementMetadata metadata)
    {
        return new ReferenceElement(TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName),
                                    TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId),
                                    TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers),
                                    TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications));
    }

    private static IRange Transform(RangeMetadata metadata)
    {
        return new AasCore.Aas3_0.Range(metadata.valueType, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort,
                                        TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description),
                                        TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds),
                                        TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications));
    }

    private static IFile Transform(FileMetadata metadata)
    {
        return new File(null, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName),
                        TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId),
                        TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers),
                        TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications));
    }

    private static IBlob Transform(BlobMetadata metadata)
    {
        return new Blob(null, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort, TransformLangStringNameTypeList(metadata.displayName),
                        TransformLangStringTextTypeList(metadata.description), TransformReference(metadata.semanticId),
                        TransformReferenceList(metadata.supplementalSemanticIds), TransformQualifierList(metadata.qualifiers),
                        TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications));
    }

    private static IBasicEventElement Transform(BasicEventElementMetadata metadata)
    {
        return new BasicEventElement(null, metadata.direction, metadata.state, TransformExtensions(metadata.extensions), metadata.category, metadata.idShort,
                                     TransformLangStringNameTypeList(metadata.displayName), TransformLangStringTextTypeList(metadata.description),
                                     TransformReference(metadata.semanticId), TransformReferenceList(metadata.supplementalSemanticIds),
                                     TransformQualifierList(metadata.qualifiers), TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), metadata.messageTopic,
                                     TransformReference(metadata.messageBroker), metadata.lastUpdate, metadata.minInterval, metadata.maxInterval);
    }

    private static IMultiLanguageProperty Transform(MultiLanguagePropertyMetadata multiLanguagePropertyMetadata) =>
        new MultiLanguageProperty(TransformExtensions(multiLanguagePropertyMetadata.extensions), multiLanguagePropertyMetadata.category,
                                  multiLanguagePropertyMetadata.idShort, TransformLangStringNameTypeList(multiLanguagePropertyMetadata.displayName),
                                  TransformLangStringTextTypeList(multiLanguagePropertyMetadata.description),
                                  TransformReference(multiLanguagePropertyMetadata.semanticId),
                                  TransformReferenceList(multiLanguagePropertyMetadata.supplementalSemanticIds),
                                  TransformQualifierList(multiLanguagePropertyMetadata.qualifiers),
                                  TransformEmbeddedDataSpecList(multiLanguagePropertyMetadata.embeddedDataSpecifications));

    private static IProperty Transform(PropertyMetadata propertyMetadata) =>
        new Property(propertyMetadata.valueType, TransformExtensions(propertyMetadata.extensions), propertyMetadata.category, propertyMetadata.idShort,
                     TransformLangStringNameTypeList(propertyMetadata.displayName), TransformLangStringTextTypeList(propertyMetadata.description),
                     TransformReference(propertyMetadata.semanticId), TransformReferenceList(propertyMetadata.supplementalSemanticIds),
                     TransformQualifierList(propertyMetadata.qualifiers), TransformEmbeddedDataSpecList(propertyMetadata.embeddedDataSpecifications));

    private static List<ILangStringNameType> TransformLangStringNameTypeList(List<LangStringNameTypeDTO> langStringNameTypeList)
    {
        return langStringNameTypeList.Select(langString => TransformLangStringNameType(langString)).ToList();
    }

    private static ILangStringNameType TransformLangStringNameType(LangStringNameTypeDTO langString) => new LangStringNameType(langString.language, langString.text);

    private static List<ILangStringTextType> TransformLangStringTextTypeList(List<LangStringTextTypeDTO> langStringTextTypeList) =>
        langStringTextTypeList.Select(langString => TransformLangStringTextType(langString)).ToList();

    private static ILangStringTextType TransformLangStringTextType(LangStringTextTypeDTO langString) => new LangStringTextType(langString.language, langString.text);

    private static List<IExtension> TransformExtensions(List<ExtensionDTO> extensions) => extensions
                                                                                          .Select(extension =>
                                                                                                      new Extension(extension.name, TransformReference(extension.semanticId),
                                                                                                                    TransformReferenceList(extension.supplementalSemanticIds),
                                                                                                                    extension.valueType, extension.value)).Cast<IExtension>()
                                                                                          .ToList();

    private static List<IReference> TransformReferenceList(List<ReferenceDTO> references)
    {
        return references.Select(reference => TransformReference(reference)).ToList();
    }

    private static IReference TransformReference(ReferenceDTO referenceDTO)
    {
        var transformedKeys = TransformKeys(referenceDTO.keys);
        var transformedSemanticId = referenceDTO.referredSemanticId != null ? 
                                        TransformReference(referenceDTO.referredSemanticId) : 
                                        null;

        return new Reference(referenceDTO.type, transformedKeys, transformedSemanticId);
    }

    private static List<IKey>? TransformKeys(List<KeyDTO>? keys) => keys?.Select(key => new Key(key.type, key.value)).Cast<IKey>().ToList();

    private static List<IQualifier> TransformQualifierList(List<QualifierDTO> qualifiers) => qualifiers.Select(qualifier => TransformQualifier(qualifier)).ToList();

    private static IQualifier TransformQualifier(QualifierDTO qualifierDTO) =>
        new Qualifier(qualifierDTO.type, qualifierDTO.valueType, TransformReference(qualifierDTO.semanticId),
                      TransformReferenceList(qualifierDTO.supplementalSemanticIds), qualifierDTO.kind, qualifierDTO.value, TransformReference(qualifierDTO.valueId));

    private static List<IEmbeddedDataSpecification> TransformEmbeddedDataSpecList(List<EmbeddedDataSpecificationDTO> embeddedDataSpecifications) =>
        embeddedDataSpecifications.Select(embDataSpec => TransformEmbeddedDataSpecification(embDataSpec)).ToList();

    private static IEmbeddedDataSpecification TransformEmbeddedDataSpecification(EmbeddedDataSpecificationDTO embDataSpecDTO) =>
        new EmbeddedDataSpecification(TransformReference(embDataSpecDTO.dataSpecification), null);

    private static IAdministrativeInformation TransformAdministrationInformation(AdministrativeInformationDTO metadata) =>
        new AdministrativeInformation(TransformEmbeddedDataSpecList(metadata.embeddedDataSpecifications), metadata.version, metadata.revision,
                                      TransformReference(metadata.creator), metadata.templateId);
}