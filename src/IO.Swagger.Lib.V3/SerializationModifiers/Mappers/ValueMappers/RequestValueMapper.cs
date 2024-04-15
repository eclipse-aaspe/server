using DataTransferObjects.CommonDTOs;
using DataTransferObjects.ValueDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using DataTransferObjects;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers
{
    /// <inheritdoc />
    public class RequestValueMapper : IRequestValueMapper
    {
        /// <inheritdoc />
        public IClass Map(IValueDTO source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return source switch
            {
                PropertyValue propertyValue => Transform(propertyValue),
                MultiLanguagePropertyValue multiLanguagePropertyValue => Transform(multiLanguagePropertyValue),
                RangeValue rangeValue => Transform(rangeValue),
                BlobValue blobValue => Transform(blobValue),
                FileValue fileValue => Transform(fileValue),
                AnnotatedRelationshipElementValue annotationElementValue => Transform(annotationElementValue),
                RelationshipElementValue relationshipElementValue => Transform(relationshipElementValue),
                ReferenceElementValue referenceElementValue => Transform(referenceElementValue),
                BasicEventElementValue basicEventElementValue => Transform(basicEventElementValue),
                EntityValue entityValue => Transform(entityValue),
                SubmodelElementCollectionValue submodelElementCollectionValue => Transform(submodelElementCollectionValue),
                SubmodelElementListValue submodelElementListValue => Transform(submodelElementListValue),
                SubmodelValue submodelValue => Transform(submodelValue),
                _ => throw new NotImplementedException()
            };
        }

        public IDTO Map(IClass source)
        {
            throw new NotImplementedException(); //TODO: this seems to be the main problem
        }

        private static IClass Transform(BasicEventElementValue valueDTO)
        {
            return new BasicEventElement(TransformReference(valueDTO.observed), Direction.Output, StateOfEvent.On, idShort: valueDTO.idShort);
        }

        private IClass Transform(SubmodelValue valueDTO)
        {
            List<ISubmodelElement> submodelElements = null;
            if (valueDTO.submodelElements != null)
            {
                submodelElements = valueDTO.submodelElements.Select(element => (ISubmodelElement) Map(element)).ToList();
            }

            return new Submodel(null, submodelElements: submodelElements);
        }

        private static IClass Transform(RangeValue valueDTO)
        {
            return new AasCore.Aas3_0.Range(DataTypeDefXsd.String, idShort: valueDTO.idShort, min: valueDTO.min, max: valueDTO.max);
        }

        private IClass Transform(SubmodelElementListValue valueDTO)
        {
            List<ISubmodelElement> value = null;
            if (valueDTO.value != null)
            {
                value = valueDTO.value.Select(element => (ISubmodelElement) Map(element)).ToList();
            }

            return new SubmodelElementList(AasSubmodelElements.SubmodelElement, idShort: valueDTO.idShort, value: value);
        }

        private static IClass Transform(MultiLanguagePropertyValue valueDTO)
        {
            var value = valueDTO.langStrings.Select(langString => new LangStringTextType(langString.Key, langString.Value)).Cast<ILangStringTextType>().ToList();
            return new MultiLanguageProperty(idShort: valueDTO.idShort, value: value);
        }

        private IClass Transform(SubmodelElementCollectionValue valueDTO)
        {
            List<ISubmodelElement> value = null;
            if (valueDTO.value != null)
            {
                value = valueDTO.value.Select(element => (ISubmodelElement) Map(element)).ToList();
            }

            return new SubmodelElementCollection(idShort: valueDTO.idShort, value: value);
        }

        private IClass Transform(EntityValue valueDTO)
        {
            List<ISubmodelElement> statements = null;
            if (valueDTO.statements != null)
            {
                statements = valueDTO.statements.Select(element => (ISubmodelElement) Map(element)).ToList();
            }

            return new Entity(valueDTO.entityType, idShort: valueDTO.idShort, statements: statements, globalAssetId: valueDTO.globalAssetId);
        }

        private static IClass Transform(ReferenceElementValue valueDTO)
        {
            return new ReferenceElement(idShort: valueDTO.idShort, value: TransformReference(valueDTO.value));
        }

        private static IClass Transform(RelationshipElementValue valueDTO)
        {
            return new RelationshipElement(TransformReference(valueDTO.first), TransformReference(valueDTO.second), idShort: valueDTO.idShort);
        }

        private IClass Transform(AnnotatedRelationshipElementValue valueDTO)
        {
            List<IDataElement> annotations = null;
            if (valueDTO.annotations != null)
            {
                annotations = valueDTO.annotations.Select(element => (IDataElement) Map(element)).ToList();
            }

            return new AnnotatedRelationshipElement(TransformReference(valueDTO.first), TransformReference(valueDTO.second), idShort: valueDTO.idShort, annotations: annotations);
        }

        private static IClass Transform(FileValue valueDTO)
        {
            return new File(valueDTO.contentType, idShort: valueDTO.idShort, value: valueDTO.value);
        }

        private static IClass Transform(BlobValue valueDTO)
        {
            return new Blob(valueDTO.contentType, idShort: valueDTO.idShort, value: valueDTO.value);
        }

        private static IClass Transform(PropertyValue valueDTO)
        {
            return new Property(DataTypeDefXsd.String, idShort: valueDTO.idShort, value: valueDTO.value);
        }

        private static IReference TransformReference(ReferenceDTO referenceDTO)
        {
            return referenceDTO == null ? null : new Reference(referenceDTO.type, TransformKeys(referenceDTO.keys), TransformReference(referenceDTO.referredSemanticId));
        }

        private static List<IKey> TransformKeys(IEnumerable<KeyDTO> keys)
        {
            return keys?.Select(key => new Key(key.type, key.value)).Cast<IKey>().ToList();
        }
    }
}