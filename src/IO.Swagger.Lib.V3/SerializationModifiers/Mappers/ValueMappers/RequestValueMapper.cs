using DataTransferObjects.CommonDTOs;
using DataTransferObjects.ValueDTOs;
using System;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers
{
    public static class RequestValueMapper
    {
        public static IClass Map(IValueDTO source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            switch (source)
            {
                case PropertyValue propertyValue:
                    {
                        return Transform(propertyValue);
                    }
                case MultiLanguagePropertyValue multiLanguagePropertyValue:
                    {
                        return Transform(multiLanguagePropertyValue);
                    }
                case RangeValue rangeValue:
                    {
                        return Transform(rangeValue);
                    }
                case BlobValue blobValue:
                    {
                        return Transform(blobValue);
                    }
                case FileValue fileValue:
                    {
                        return Transform(fileValue);
                    }
                case AnnotatedRelationshipElementValue annotationElementValue:
                    {
                        return Transform(annotationElementValue);
                    }
                case RelationshipElementValue relationshipElementValue:
                    {
                        return Transform(relationshipElementValue);
                    }
                case ReferenceElementValue referenceElementValue:
                    {
                        return Transform(referenceElementValue);
                    }
                case BasicEventElementValue basicEventElementValue:
                    {
                        return Transform(basicEventElementValue);
                    }
                case EntityValue entityValue:
                    {
                        return Transform(entityValue);
                    }
                case SubmodelElementCollectionValue submodelElementCollectionValue:
                    {
                        return Transform(submodelElementCollectionValue);
                    }
                case SubmodelElementListValue submodelElementListValue:
                    {
                        return Transform(submodelElementListValue);
                    }
                case SubmodelValue submodelValue:
                    {
                        return Transform(submodelValue);
                    }
                default:
                    {
                        throw new NotImplementedException();
                    }
            }

        }

        private static IClass Transform(BasicEventElementValue valueDTO)
        {
            return new BasicEventElement(TransformReference(valueDTO.observed), Direction.Output, StateOfEvent.On, idShort: valueDTO.idShort);
        }

        private static IClass Transform(SubmodelValue valueDTO)
        {
            List<ISubmodelElement> submodelElements = null;
            if (valueDTO.submodelElements != null)
            {
                submodelElements = new List<ISubmodelElement>();
                foreach (var element in valueDTO.submodelElements)
                {
                    submodelElements.Add((ISubmodelElement)Map(element));
                }
            }

            return new Submodel(null, submodelElements: submodelElements);
        }

        private static IClass Transform(RangeValue valueDTO)
        {
            return new AasCore.Aas3_0.Range(DataTypeDefXsd.String, idShort: valueDTO.idShort, min: valueDTO.min, max: valueDTO.max);
        }

        private static IClass Transform(SubmodelElementListValue valueDTO)
        {
            List<ISubmodelElement> value = null;
            if (valueDTO.value != null)
            {
                value = new List<ISubmodelElement>();
                foreach (var element in valueDTO.value)
                {
                    value.Add((IDataElement)Map(element));
                }
            }

            return new SubmodelElementList(AasSubmodelElements.SubmodelElement, idShort: valueDTO.idShort, value: value);
        }

        private static IClass Transform(MultiLanguagePropertyValue valueDTO)
        {
            var value = new List<ILangStringTextType>();
            foreach (var langString in valueDTO.langStrings)
            {
                value.Add(new LangStringTextType(langString.Key, langString.Value));
            }
            return new MultiLanguageProperty(idShort: valueDTO.idShort, value: value);
        }

        private static IClass Transform(SubmodelElementCollectionValue valueDTO)
        {
            List<ISubmodelElement> value = null;
            if (valueDTO.value != null)
            {
                value = new List<ISubmodelElement>();
                foreach (var element in valueDTO.value)
                {
                    value.Add((ISubmodelElement)Map(element));
                }
            }

            return new SubmodelElementCollection(idShort: valueDTO.idShort, value: value);
        }

        private static IClass Transform(EntityValue valueDTO)
        {
            List<ISubmodelElement> statements = null;
            if (valueDTO.statements != null)
            {
                statements = new List<ISubmodelElement>();
                foreach (var element in valueDTO.statements)
                {
                    statements.Add((ISubmodelElement)Map(element));
                }
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

        private static IClass Transform(AnnotatedRelationshipElementValue valueDTO)
        {
            List<IDataElement> annotations = null;
            if (valueDTO.annotations != null)
            {
                annotations = new List<IDataElement>();
                foreach (var element in valueDTO.annotations)
                {
                    annotations.Add((IDataElement)Map(element));
                }
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
    }
}
