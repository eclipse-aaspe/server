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

using DataTransferObjects;
using DataTransferObjects.CommonDTOs;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.Exceptions;
using System.Collections.Generic;
using static AasCore.Aas3_0.Visitation;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers
{
    internal class ResponseValueTransformer : ITransformer<IDTO>
    {
        public IDTO? Transform(IClass? that) => that?.Transform(this);

        public IDTO TransformAdministrativeInformation(IAdministrativeInformation that)
        {
            throw new System.NotImplementedException();
        }

        public IDTO TransformAnnotatedRelationshipElement(IAnnotatedRelationshipElement that)
        {
            List<ISubmodelElementValue> annotations = null;
            if (that.Annotations != null)
            {
                annotations = new List<ISubmodelElementValue>();
                foreach (var element in that.Annotations)
                {
                    annotations.Add((ISubmodelElementValue) Transform(element));
                }
            }

            return new AnnotatedRelationshipElementValue(that.IdShort, (ReferenceDTO) Transform(that.First), (ReferenceDTO) Transform(that.Second), annotations);
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
            return new BasicEventElementValue(that.IdShort, (ReferenceDTO) Transform(that.Observed));
        }

        public IDTO TransformBlob(IBlob that)
        {
            return new BlobValue(that.IdShort, that.ContentType, that.Value);
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
                statements = new List<ISubmodelElementValue>();
                foreach (var element in that.Statements)
                {
                    statements.Add((ISubmodelElementValue) Transform(element));
                }
            }

            return new EntityValue(that.IdShort, that.EntityType, statements, that.GlobalAssetId);
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
            return new FileValue(that.IdShort, that.ContentType, that.Value);
        }

        internal List<KeyDTO>? TransformKeyList(List<IKey?>? keyList)
        {
            List<KeyDTO>? output = null;

            if (keyList != null)
            {
                output = new List<KeyDTO>();
                foreach (var key in keyList)
                {
                    output.Add((KeyDTO) Transform(key));
                }
            }

            return output;
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
            var langStrings = new List<KeyValuePair<string, string?>>();
            foreach (var langString in that.Value)
            {
                langStrings.Add(new KeyValuePair<string, string?>(langString.Language, langString.Text));
            }

            return new MultiLanguagePropertyValue(that.IdShort, langStrings);
        }

        public IDTO TransformOperation(IOperation that)
        {
            throw new InvalidSerializationModifierException("ValueOnly", that.GetType().Name);
        }

        public IDTO? TransformOperationVariable(IOperationVariable? that) => Transform(that?.Value);

        public IDTO TransformProperty(IProperty that)
        {
            return new PropertyValue(that.IdShort, that.Value);
        }

        public IDTO TransformQualifier(IQualifier that)
        {
            throw new System.NotImplementedException();
        }

        public IDTO TransformRange(IRange that)
        {
            return new RangeValue(that.IdShort, that.Min, that.Max);
        }

        public IDTO TransformReference(IReference that)
        {
            return new ReferenceDTO(that.Type, TransformKeyList(that.Keys), (ReferenceDTO) Transform(that.ReferredSemanticId));
        }

        public IDTO TransformReferenceElement(IReferenceElement that)
        {
            return new ReferenceElementValue(that.IdShort, (ReferenceDTO) Transform(that.Value));
        }

        public IDTO TransformRelationshipElement(IRelationshipElement that)
        {
            return new RelationshipElementValue(that.IdShort, (ReferenceDTO) Transform(that.First), (ReferenceDTO) Transform(that.Second));
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
                submodelElements = new List<ISubmodelElementValue>();
                foreach (var element in that.SubmodelElements)
                {
                    try
                    {
                        submodelElements.Add((ISubmodelElementValue)Transform(element));
                    }
                    catch (InvalidSerializationModifierException)
                    {
                        continue;
                    }
                }
            }

            return new SubmodelValue(submodelElements);
        }

        public IDTO TransformSubmodelElementCollection(ISubmodelElementCollection that)
        {
            List<ISubmodelElementValue> value = null;
            if (that.Value != null)
            {
                value = new List<ISubmodelElementValue>();
                foreach (var element in that.Value)
                {
                    try
                    {
                        value.Add((ISubmodelElementValue)Transform(element));
                    }
                    catch (InvalidSerializationModifierException)
                    {
                        continue;
                    }
                }
            }

            return new SubmodelElementCollectionValue(that.IdShort, value);
        }

        public IDTO TransformSubmodelElementList(ISubmodelElementList that)
        {
            List<ISubmodelElementValue> value = null;
            if (that.Value != null)
            {
                value = new List<ISubmodelElementValue>();
                foreach (var element in that.Value)
                {
                    value.Add((ISubmodelElementValue) Transform(element));
                }
            }

            return new SubmodelElementListValue(that.IdShort, value);
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