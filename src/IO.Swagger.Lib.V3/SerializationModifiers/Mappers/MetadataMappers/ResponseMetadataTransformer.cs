using DataTransferObjects;
using DataTransferObjects.CommonDTOs;
using DataTransferObjects.MetadataDTOs;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using static AasCore.Aas3_0.Visitation;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.MetadataMappers
{
    internal class ResponseMetadataTransformer : ITransformer<IDTO>
    {
        public IDTO Transform(IClass that)
        {
            if (that == null) return null;
            return that.Transform(this);
        }

        public IDTO TransformAdministrativeInformation(IAdministrativeInformation that)
        {
            if (that == null) return null;
            return new AdministrativeInformationDTO(TransformEmbeddedDataSpecList(that.EmbeddedDataSpecifications), that.Version, that.Revision, (ReferenceDTO)Transform(that.Creator), that.TemplateId);
        }

        public IDTO TransformAnnotatedRelationshipElement(IAnnotatedRelationshipElement that)
        {
            if (that == null) return null;
            List<ISubmodelElementMetadata> smeMetadataList = null;
            if (!that.Annotations.IsNullOrEmpty())
            {
                smeMetadataList = new List<ISubmodelElementMetadata>();
                foreach (var submodelElement in that.Annotations)
                {
                    smeMetadataList.Add((ISubmodelElementMetadata)Transform(submodelElement));
                }
            }

            return new AnnotatedRelationshipElementMetadata(TransformExtensionList(that.Extensions), that.Category, that.IdShort, TransformLangStringNameTypeList(that.DisplayName), TransformLangStringTextTypeList(that.Description), (ReferenceDTO)Transform(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), TransformQualifierList(that.Qualifiers), TransformEmbeddedDataSpecList(that.EmbeddedDataSpecifications), smeMetadataList);
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
            if (that == null) return null;
            return new BasicEventElementMetadata(that.Direction, that.State, TransformExtensionList(that.Extensions), that.Category, that.IdShort, TransformLangStringNameTypeList(that.DisplayName), TransformLangStringTextTypeList(that.Description), (ReferenceDTO)Transform(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), TransformQualifierList(that.Qualifiers), TransformEmbeddedDataSpecList(that.EmbeddedDataSpecifications), that.MessageTopic, (ReferenceDTO)Transform(that.MessageBroker), that.LastUpdate, that.MinInterval, that.MaxInterval);
        }

        public IDTO TransformBlob(IBlob that)
        {
            if (that == null)
                return null;
            return new BlobMetadata(TransformExtensionList(that.Extensions), that.Category, that.IdShort, TransformLangStringNameTypeList(that.DisplayName), TransformLangStringTextTypeList(that.Description), (ReferenceDTO)Transform(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), TransformQualifierList(that.Qualifiers), TransformEmbeddedDataSpecList(that.EmbeddedDataSpecifications));
        }

        public IDTO TransformCapability(ICapability that)
        {
            throw new System.NotImplementedException();
        }

        public IDTO TransformConceptDescription(IConceptDescription that)
        {
            throw new System.NotImplementedException();
        }

        public IDTO TransformDataSpecificationIec61360(IDataSpecificationIec61360 that)
        {
            throw new System.NotImplementedException();
        }

        internal List<EmbeddedDataSpecificationDTO> TransformEmbeddedDataSpecList(List<IEmbeddedDataSpecification> that)
        {
            List<EmbeddedDataSpecificationDTO> output = null;
            if (!that.IsNullOrEmpty())
            {
                output = new List<EmbeddedDataSpecificationDTO>();
                foreach (var item in that)
                {
                    output.Add((EmbeddedDataSpecificationDTO)Transform(item));
                }
            }

            return output;
        }
        public IDTO TransformEmbeddedDataSpecification(IEmbeddedDataSpecification that)
        {
            if (that == null) return null;
            return new EmbeddedDataSpecificationDTO((ReferenceDTO)Transform(that.DataSpecification));
        }

        public IDTO TransformEntity(IEntity that)
        {
            if (that == null) return null;
            List<ISubmodelElementMetadata> smeMetadataList = null;
            if (!that.Statements.IsNullOrEmpty())
            {
                smeMetadataList = new List<ISubmodelElementMetadata>();
                foreach (var submodelElement in that.Statements)
                {
                    smeMetadataList.Add((ISubmodelElementMetadata)Transform(submodelElement));
                }
            }
            return new EntityMetadata(that.EntityType, TransformExtensionList(that.Extensions), that.Category, that.IdShort, TransformLangStringNameTypeList(that.DisplayName), TransformLangStringTextTypeList(that.Description), (ReferenceDTO)Transform(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), TransformQualifierList(that.Qualifiers), TransformEmbeddedDataSpecList(that.EmbeddedDataSpecifications), smeMetadataList);
        }

        public IDTO TransformEnvironment(IEnvironment that)
        {
            throw new System.NotImplementedException();
        }

        public IDTO TransformEventPayload(IEventPayload that)
        {
            throw new System.NotImplementedException();
        }

        public List<ExtensionDTO> TransformExtensionList(List<IExtension> that)
        {
            List<ExtensionDTO> extensions = null;
            if (that != null)
            {
                extensions = new List<ExtensionDTO>();
                foreach (var extensionElement in that)
                    extensions.Add((ExtensionDTO)Transform(extensionElement));
            }

            return extensions;
        }

        public IDTO TransformExtension(IExtension that)
        {
            return new ExtensionDTO(that.Name, (ReferenceDTO)Transform(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), that.ValueType, that.Value, TransformReferenceList(that.RefersTo));
        }

        public IDTO TransformFile(IFile that)
        {
            if (that == null)
                return null;
            return new FileMetadata(TransformExtensionList(that.Extensions), that.Category, that.IdShort, TransformLangStringNameTypeList(that.DisplayName), TransformLangStringTextTypeList(that.Description), (ReferenceDTO)Transform(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), TransformQualifierList(that.Qualifiers), TransformEmbeddedDataSpecList(that.EmbeddedDataSpecifications));
        }

        internal List<KeyDTO> TransformKeyList(List<IKey> keyList)
        {
            List<KeyDTO> output = null;

            if (keyList != null)
            {
                output = new List<KeyDTO>();
                foreach (var key in keyList)
                {
                    output.Add((KeyDTO)Transform(key));
                }
            }

            return output;
        }
        public IDTO TransformKey(IKey that)
        {
            if (that == null) return null;
            return new KeyDTO(that.Type, that.Value);
        }

        public IDTO TransformLangStringDefinitionTypeIec61360(ILangStringDefinitionTypeIec61360 that)
        {
            throw new System.NotImplementedException();
        }

        public List<LangStringNameTypeDTO> TransformLangStringNameTypeList(List<ILangStringNameType> that)
        {
            List<LangStringNameTypeDTO> langStrings = null;
            if (that != null)
            {
                langStrings = new List<LangStringNameTypeDTO>();
                foreach (var langString in that)
                    langStrings.Add((LangStringNameTypeDTO)Transform(langString));
            }

            return langStrings;
        }

        public IDTO TransformLangStringNameType(ILangStringNameType that)
        {
            if (that == null)
                return null;
            return new LangStringNameTypeDTO(that.Language, that.Text);
        }

        public IDTO TransformLangStringPreferredNameTypeIec61360(ILangStringPreferredNameTypeIec61360 that)
        {
            throw new System.NotImplementedException();
        }

        public IDTO TransformLangStringShortNameTypeIec61360(ILangStringShortNameTypeIec61360 that)
        {
            throw new System.NotImplementedException();
        }

        public List<LangStringTextTypeDTO> TransformLangStringTextTypeList(List<ILangStringTextType> that)
        {
            List<LangStringTextTypeDTO> langStrings = null;
            if (that != null)
            {
                langStrings = new List<LangStringTextTypeDTO>();
                foreach (var langString in that)
                    langStrings.Add((LangStringTextTypeDTO)Transform(langString));
            }

            return langStrings;
        }

        public IDTO TransformLangStringTextType(ILangStringTextType that)
        {
            if (that == null)
                return null;
            return new LangStringTextTypeDTO(that.Language, that.Text);
        }

        public IDTO TransformLevelType(ILevelType that)
        {
            throw new System.NotImplementedException();
        }

        public IDTO TransformMultiLanguageProperty(IMultiLanguageProperty that)
        {
            if (that == null)
                return null;
            return new MultiLanguagePropertyMetadata(TransformExtensionList(that.Extensions), that.Category, that.IdShort, TransformLangStringNameTypeList(that.DisplayName), TransformLangStringTextTypeList(that.Description), (ReferenceDTO)Transform(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), TransformQualifierList(that.Qualifiers), TransformEmbeddedDataSpecList(that.EmbeddedDataSpecifications));
        }

        public IDTO TransformOperation(IOperation that)
        {
            if (that == null) return null;
            List<IMetadataDTO> inputVarMetadataList = null;
            if (!that.InputVariables.IsNullOrEmpty())
            {
                inputVarMetadataList = new List<IMetadataDTO>();
                foreach (var submodelElement in that.InputVariables)
                {
                    inputVarMetadataList.Add((IMetadataDTO)Transform(submodelElement));
                }
            }

            List<IMetadataDTO> outputVarMetadataList = null;
            if (!that.OutputVariables.IsNullOrEmpty())
            {
                outputVarMetadataList = new List<IMetadataDTO>();
                foreach (var submodelElement in that.OutputVariables)
                {
                    outputVarMetadataList.Add((IMetadataDTO)Transform(submodelElement));
                }
            }

            List<IMetadataDTO> inOutVarMetadataList = null;
            if (!that.InoutputVariables.IsNullOrEmpty())
            {
                inOutVarMetadataList = new List<IMetadataDTO>();
                foreach (var submodelElement in that.InoutputVariables)
                {
                    inOutVarMetadataList.Add((IMetadataDTO)Transform(submodelElement));
                }
            }

            return new OperationMetadata(TransformExtensionList(that.Extensions), that.Category, that.IdShort, TransformLangStringNameTypeList(that.DisplayName), TransformLangStringTextTypeList(that.Description), (ReferenceDTO)Transform(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), TransformQualifierList(that.Qualifiers), TransformEmbeddedDataSpecList(that.EmbeddedDataSpecifications), inputVarMetadataList, outputVarMetadataList, inOutVarMetadataList);
        }

        public IDTO TransformOperationVariable(IOperationVariable that)
        {
            return Transform(that.Value);
        }

        public IDTO TransformProperty(IProperty that)
        {
            if (that == null)
                return null;
            return new PropertyMetadata(that.ValueType, TransformExtensionList(that.Extensions), that.Category, that.IdShort, TransformLangStringNameTypeList(that.DisplayName), TransformLangStringTextTypeList(that.Description), (ReferenceDTO)TransformReference(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), TransformQualifierList(that.Qualifiers), TransformEmbeddedDataSpecList(that.EmbeddedDataSpecifications));
        }

        internal List<QualifierDTO> TransformQualifierList(List<IQualifier> qualifierList)
        {
            List<QualifierDTO> output = null;
            if (!qualifierList.IsNullOrEmpty())
            {
                output = new List<QualifierDTO>();
                foreach (var qualifier in qualifierList)
                {
                    output.Add((QualifierDTO)Transform(qualifier));
                }
            }

            return output;
        }

        public IDTO TransformQualifier(IQualifier that)
        {
            if (that == null)
                return null;
            return new QualifierDTO(that.Type, that.ValueType, (ReferenceDTO)Transform(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), that.Kind, that.Value, (ReferenceDTO)Transform(that.ValueId));
        }

        public IDTO TransformRange(IRange that)
        {
            if (that == null)
                return null;
            return new RangeMetadata(that.ValueType, TransformExtensionList(that.Extensions), that.Category, that.IdShort, TransformLangStringNameTypeList(that.DisplayName), TransformLangStringTextTypeList(that.Description), (ReferenceDTO)Transform(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), TransformQualifierList(that.Qualifiers), TransformEmbeddedDataSpecList(that.EmbeddedDataSpecifications));
        }

        internal List<ReferenceDTO> TransformReferenceList(List<IReference> that)
        {
            List<ReferenceDTO> output = null;
            if (!that.IsNullOrEmpty())
            {
                output = new List<ReferenceDTO>();
                foreach (var reference in that)
                    output.Add((ReferenceDTO)Transform(reference));
            }

            return output;
        }
        public IDTO TransformReference(IReference that)
        {
            if (that == null)
                return null;
            return new ReferenceDTO(that.Type, TransformKeyList(that.Keys), (ReferenceDTO)Transform(that.ReferredSemanticId));
        }

        public IDTO TransformReferenceElement(IReferenceElement that)
        {
            if (that == null)
                return null;
            return new ReferenceElementMetadata(TransformExtensionList(that.Extensions), that.Category, that.IdShort, TransformLangStringNameTypeList(that.DisplayName), TransformLangStringTextTypeList(that.Description), (ReferenceDTO)Transform(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), TransformQualifierList(that.Qualifiers), TransformEmbeddedDataSpecList(that.EmbeddedDataSpecifications));
        }

        public IDTO TransformRelationshipElement(IRelationshipElement that)
        {
            if (that == null)
                return null;
            return new RelationshipElementMetadata(TransformExtensionList(that.Extensions), that.Category, that.IdShort, TransformLangStringNameTypeList(that.DisplayName), TransformLangStringTextTypeList(that.Description), (ReferenceDTO)Transform(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), TransformQualifierList(that.Qualifiers), TransformEmbeddedDataSpecList(that.EmbeddedDataSpecifications));
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
            if (that == null) return null;
            List<ISubmodelElementMetadata> smeMetadataList = null;
            if (!that.SubmodelElements.IsNullOrEmpty())
            {
                smeMetadataList = new List<ISubmodelElementMetadata>();
                foreach (var submodelElement in that.SubmodelElements)
                {
                    smeMetadataList.Add((ISubmodelElementMetadata)Transform(submodelElement));
                }
            }

            return new SubmodelMetadata(that.Id, TransformExtensionList(that.Extensions), that.Category, that.IdShort, TransformLangStringNameTypeList(that.DisplayName), TransformLangStringTextTypeList(that.Description), (AdministrativeInformationDTO)Transform(that.Administration), that.Kind, (ReferenceDTO)Transform(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), TransformQualifierList(that.Qualifiers), TransformEmbeddedDataSpecList(that.EmbeddedDataSpecifications), smeMetadataList);
        }

        public IDTO TransformSubmodelElementCollection(ISubmodelElementCollection that)
        {
            List<ISubmodelElementMetadata> smeMetadataList = null;
            if (!that.Value.IsNullOrEmpty())
            {
                smeMetadataList = new List<ISubmodelElementMetadata>();
                foreach (var submodelElement in that.Value)
                {
                    smeMetadataList.Add((ISubmodelElementMetadata)Transform(submodelElement));
                }
            }
            return new SubmodelElementCollectionMetadata(TransformExtensionList(that.Extensions), that.Category, that.IdShort, TransformLangStringNameTypeList(that.DisplayName), TransformLangStringTextTypeList(that.Description), (ReferenceDTO)Transform(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), TransformQualifierList(that.Qualifiers), TransformEmbeddedDataSpecList(that.EmbeddedDataSpecifications), smeMetadataList);
        }

        public IDTO TransformSubmodelElementList(ISubmodelElementList that)
        {
            if (that == null) return null;
            List<ISubmodelElementMetadata> smeMetadataList = null;
            if (!that.Value.IsNullOrEmpty())
            {
                smeMetadataList = new List<ISubmodelElementMetadata>();
                foreach (var submodelElement in that.Value)
                {
                    smeMetadataList.Add((ISubmodelElementMetadata)Transform(submodelElement));
                }
            }
            return new SubmodelElementListMetadata(that.TypeValueListElement, TransformExtensionList(that.Extensions), that.Category, that.IdShort, TransformLangStringNameTypeList(that.DisplayName), TransformLangStringTextTypeList(that.Description), (ReferenceDTO)Transform(that.SemanticId), TransformReferenceList(that.SupplementalSemanticIds), TransformQualifierList(that.Qualifiers), TransformEmbeddedDataSpecList(that.EmbeddedDataSpecifications), that.OrderRelevant, (ReferenceDTO)Transform(that.SemanticIdListElement), that.ValueTypeListElement, smeMetadataList);
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
}
