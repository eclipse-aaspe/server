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
using DataTransferObjects.ValueDTOs;
using System;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers
{
    public static class RequestValueMapper
    {
        public static IClass? Map(IValueDTO source)
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

        private static IClass? Transform(BasicEventElementValue valueDTO)
        {
            return new BasicEventElement(TransformReference(valueDTO.Observed), Direction.Output, StateOfEvent.On, idShort: valueDTO.IdShort);
        }

        private static IClass Transform(SubmodelValue valueDTO)
        {
            List<ISubmodelElement> submodelElements = null;
            if (valueDTO.SubmodelElements != null)
            {
                submodelElements = new List<ISubmodelElement>();
                foreach (var element in valueDTO.SubmodelElements)
                {
                    submodelElements.Add((ISubmodelElement)Map(element));
                }
            }

            return new Submodel(null, submodelElements: submodelElements);
        }

        private static IClass Transform(RangeValue valueDTO)
        {
            return new AasCore.Aas3_0.Range(DataTypeDefXsd.String, idShort: valueDTO.IdShort, min: valueDTO.Min, max: valueDTO.Max);
        }

        private static IClass Transform(SubmodelElementListValue valueDTO)
        {
            List<ISubmodelElement> value = null;
            if (valueDTO.Value != null)
            {
                value = new List<ISubmodelElement>();
                foreach (var element in valueDTO.Value)
                {
                    value.Add((ISubmodelElement)Map(element));
                }
            }

            return new SubmodelElementList(AasSubmodelElements.SubmodelElement, idShort: valueDTO.IdShort, value: value);
        }

        private static IClass Transform(MultiLanguagePropertyValue valueDTO)
        {
            var value = new List<ILangStringTextType>();
            foreach (var langString in valueDTO.LangStrings)
            {
                value.Add(new LangStringTextType(langString.Key, langString.Value));
            }
            return new MultiLanguageProperty(idShort: valueDTO.IdShort, value: value);
        }

        private static IClass Transform(SubmodelElementCollectionValue valueDTO)
        {
            List<ISubmodelElement> value = null;
            if (valueDTO.Value != null)
            {
                value = new List<ISubmodelElement>();
                foreach (var element in valueDTO.Value)
                {
                    value.Add((ISubmodelElement)Map(element));
                }
            }

            return new SubmodelElementCollection(idShort: valueDTO.IdShort, value: value);
        }

        private static IClass Transform(EntityValue valueDTO)
        {
            List<ISubmodelElement> statements = null;
            if (valueDTO.Statements != null)
            {
                statements = new List<ISubmodelElement>();
                foreach (var element in valueDTO.Statements)
                {
                    statements.Add((ISubmodelElement)Map(element));
                }
            }
            return new Entity(valueDTO.EntityType, idShort: valueDTO.IdShort, statements: statements, globalAssetId: valueDTO.GlobalAssetId);
        }

        private static IClass Transform(ReferenceElementValue valueDTO)
        {
            return new ReferenceElement(idShort: valueDTO.IdShort, value: TransformReference(valueDTO.Value));
        }

        private static IClass Transform(RelationshipElementValue valueDTO)
        {
            return new RelationshipElement(TransformReference(valueDTO.First), TransformReference(valueDTO.Second), idShort: valueDTO.IdShort);
        }

        private static IClass? Transform(AnnotatedRelationshipElementValue valueDTO)
        {
            List<IDataElement> annotations = null;
            if (valueDTO.Annotations != null)
            {
                annotations = new List<IDataElement>();
                foreach (var element in valueDTO.Annotations)
                {
                    annotations.Add((IDataElement)Map(element));
                }
            }
            return new AnnotatedRelationshipElement(TransformReference(valueDTO.First), TransformReference(valueDTO.Second), idShort: valueDTO.IdShort, annotations: annotations);
        }

        private static IClass Transform(FileValue valueDTO)
        {
            return new File(valueDTO.ContentType, idShort: valueDTO.IdShort, value: valueDTO.Value);
        }

        private static IClass Transform(BlobValue valueDTO)
        {
            return new Blob(valueDTO.ContentType, idShort: valueDTO.IdShort, value: valueDTO.Value);
        }

        private static IClass Transform(PropertyValue valueDTO)
        {
            return new Property(DataTypeDefXsd.String, idShort: valueDTO.IdShort, value: valueDTO.Value);
        }

        private static IReference? TransformReference(ReferenceDTO referenceDTO)
        {
            if (referenceDTO == null)
                return null;
            return new Reference(referenceDTO.Type, TransformKeys(referenceDTO.Keys), TransformReference(referenceDTO.ReferredSemanticId));
        }

        private static List<IKey>? TransformKeys(List<KeyDTO>? keys)
        {
            if (keys == null) return null;

            var result = new List<IKey>();
            foreach (var key in keys)
            {
                result.Add(new Key(key.Type, key.Value));
            }

            return result;
        }
    }
}
