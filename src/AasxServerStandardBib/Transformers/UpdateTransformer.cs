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

using AasxServerStandardBib.Exceptions;
using System.Linq;
using static AasCore.Aas3_0.Visitation;

namespace AasxServerStandardBib.Transformers
{
    //internal class UpdateTransformer : AbstractTransformerWithContext<IClass, IClass>
    internal class UpdateTransformer : AbstractVisitorWithContext<IClass>
    {
        public override void VisitAdministrativeInformation(IAdministrativeInformation that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitAnnotatedRelationshipElement(IAnnotatedRelationshipElement that, IClass context)
        {
            if (context is IAnnotatedRelationshipElement target)
            {
                //As per the specifications, check for smes first. If any of the resource not available, do not change the server
                if (target.Annotations != null && target.Annotations.Any())
                {
                    //First check if number of elements in the server's resource are greater than or equal to the request's resource
                    if (that.Annotations.Count < target.Annotations.Count)
                    {
                        throw new InvalidNumberOfChildElementsException("Submodel : " + that.IdShort, target.Annotations.Count, that.Annotations.Count);
                    }
                    for (int i = 0; i < target.Annotations.Count; i++)
                    {
                        Update.ToUpdateObject(that.Annotations[i], target.Annotations[i]);
                    }
                }

                if (target.Extensions != null)
                    that.Extensions = target.Extensions;
                if (target.Category != null)
                    that.Category = target.Category;
                if (target.IdShort != null)
                    that.IdShort = target.IdShort;
                if (target.DisplayName != null)
                    that.DisplayName = target.DisplayName;
                if (target.Description != null)
                    that.Description = target.Description;
                if (target.SemanticId != null)
                    that.SemanticId = target.SemanticId;
                if (target.SupplementalSemanticIds != null)
                    that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                if (target.Qualifiers != null)
                    that.Qualifiers = target.Qualifiers;
                if (target.EmbeddedDataSpecifications != null)
                    that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
                if (target.First != null)
                    that.First = target.First;
                if (target.Second != null)
                    that.Second = target.Second;
            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(AnnotatedRelationshipElement).Name, context.GetType().Name);
            }
        }

        public override void VisitAssetAdministrationShell(IAssetAdministrationShell that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitAssetInformation(IAssetInformation that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitBasicEventElement(IBasicEventElement that, IClass context)
        {
            if (context is IBasicEventElement target)
            {
                if (target.Extensions != null)
                    that.Extensions = target.Extensions;
                if (target.Category != null)
                    that.Category = target.Category;
                if (target.IdShort != null)
                    that.IdShort = target.IdShort;
                if (target.DisplayName != null)
                    that.DisplayName = target.DisplayName;
                if (target.Description != null)
                    that.Description = target.Description;
                if (target.SemanticId != null)
                    that.SemanticId = target.SemanticId;
                if (target.SupplementalSemanticIds != null)
                    that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                if (target.Qualifiers != null)
                    that.Qualifiers = target.Qualifiers;
                if (target.EmbeddedDataSpecifications != null)
                    that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
                if (target.Observed != null)
                    that.Observed = target.Observed;
                that.Direction = target.Direction;
                that.State = target.State;
                if (target.MessageTopic != null)
                    that.MessageTopic = target.MessageTopic;
                if (target.MessageBroker != null)
                    that.MessageBroker = target.MessageBroker;
                if (target.LastUpdate != null)
                    that.LastUpdate = target.LastUpdate;
                if (target.MinInterval != null)
                    that.MinInterval = target.MinInterval;
                if (target.MaxInterval != null)
                    that.MaxInterval = target.MaxInterval;
            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(BasicEventElement).Name, context.GetType().Name);
            }
        }

        public override void VisitBlob(IBlob that, IClass context)
        {
            if (context is IBlob target)
            {
                if (target.Extensions != null)
                    that.Extensions = target.Extensions;
                if (target.Category != null)
                    that.Category = target.Category;
                if (target.IdShort != null)
                    that.IdShort = target.IdShort;
                if (target.DisplayName != null)
                    that.DisplayName = target.DisplayName;
                if (target.Description != null)
                    that.Description = target.Description;
                if (target.SemanticId != null)
                    that.SemanticId = target.SemanticId;
                if (target.SupplementalSemanticIds != null)
                    that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                if (target.Qualifiers != null)
                    that.Qualifiers = target.Qualifiers;
                if (target.EmbeddedDataSpecifications != null)
                    that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
                if (target.ContentType != null)
                    that.ContentType = target.ContentType;
                if (target.Value != null)
                    that.Value = target.Value;
            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(Blob).Name, context.GetType().Name);
            }
        }

        public override void VisitCapability(ICapability that, IClass context)
        {
            if (context is ICapability target)
            {
                if (target.Extensions != null)
                    that.Extensions = target.Extensions;
                if (target.Category != null)
                    that.Category = target.Category;
                if (target.IdShort != null)
                    that.IdShort = target.IdShort;
                if (target.DisplayName != null)
                    that.DisplayName = target.DisplayName;
                if (target.Description != null)
                    that.Description = target.Description;
                if (target.SemanticId != null)
                    that.SemanticId = target.SemanticId;
                if (target.SupplementalSemanticIds != null)
                    that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                if (target.Qualifiers != null)
                    that.Qualifiers = target.Qualifiers;
                if (target.EmbeddedDataSpecifications != null)
                    that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(Capability).Name, context.GetType().Name);
            }
        }

        public override void VisitConceptDescription(IConceptDescription that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitDataSpecificationIec61360(IDataSpecificationIec61360 that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitEmbeddedDataSpecification(IEmbeddedDataSpecification that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitEntity(IEntity that, IClass context)
        {
            if (context is IEntity target)
            {
                //As per the specifications, check for smes first. If any of the resource not available, do not change the server
                if (target.Statements != null && target.Statements.Any())
                {
                    //First check if number of elements in the server's resource are greater than or equal to the request's resource
                    if (that.Statements.Count < target.Statements.Count)
                    {
                        throw new InvalidNumberOfChildElementsException("Entity : " + that.IdShort, target.Statements.Count, that.Statements.Count);
                    }
                    for (int i = 0; i < target.Statements.Count; i++)
                    {
                        Update.ToUpdateObject(that.Statements[i], target.Statements[i]);
                    }
                }

                if (target.Extensions != null)
                    that.Extensions = target.Extensions;
                if (target.Category != null)
                    that.Category = target.Category;
                if (target.IdShort != null)
                    that.IdShort = target.IdShort;
                if (target.DisplayName != null)
                    that.DisplayName = target.DisplayName;
                if (target.Description != null)
                    that.Description = target.Description;
                if (target.SemanticId != null)
                    that.SemanticId = target.SemanticId;
                if (target.SupplementalSemanticIds != null)
                    that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                if (target.Qualifiers != null)
                    that.Qualifiers = target.Qualifiers;
                if (target.EmbeddedDataSpecifications != null)
                    that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
                that.EntityType = target.EntityType;
                if (target.GlobalAssetId != null)
                    that.GlobalAssetId = target.GlobalAssetId;
                if (target.SpecificAssetIds != null)
                    that.SpecificAssetIds = target.SpecificAssetIds;
            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(Entity).Name, context.GetType().Name);
            }
        }

        public override void VisitEnvironment(IEnvironment that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitEventPayload(IEventPayload that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitExtension(IExtension that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitFile(IFile that, IClass context)
        {
            if (context is IFile target)
            {
                if (target.Extensions != null)
                    that.Extensions = target.Extensions;
                if (target.Category != null)
                    that.Category = target.Category;
                if (target.IdShort != null)
                    that.IdShort = target.IdShort;
                if (target.DisplayName != null)
                    that.DisplayName = target.DisplayName;
                if (target.Description != null)
                    that.Description = target.Description;
                if (target.SemanticId != null)
                    that.SemanticId = target.SemanticId;
                if (target.SupplementalSemanticIds != null)
                    that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                if (target.Qualifiers != null)
                    that.Qualifiers = target.Qualifiers;
                if (target.EmbeddedDataSpecifications != null)
                    that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
                if (target.ContentType != null)
                    that.ContentType = target.ContentType;
                if (target.Value != null)
                    that.Value = target.Value;
            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(File).Name, context.GetType().Name);
            }
        }

        public override void VisitKey(IKey that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitLangStringDefinitionTypeIec61360(ILangStringDefinitionTypeIec61360 that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitLangStringNameType(ILangStringNameType that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitLangStringPreferredNameTypeIec61360(ILangStringPreferredNameTypeIec61360 that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitLangStringShortNameTypeIec61360(ILangStringShortNameTypeIec61360 that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitLangStringTextType(ILangStringTextType that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitLevelType(ILevelType that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitMultiLanguageProperty(IMultiLanguageProperty that, IClass context)
        {
            if (context is IMultiLanguageProperty target)
            {
                if (target.Extensions != null)
                    that.Extensions = target.Extensions;
                if (target.Category != null)
                    that.Category = target.Category;
                if (target.IdShort != null)
                    that.IdShort = target.IdShort;
                if (target.DisplayName != null)
                    that.DisplayName = target.DisplayName;
                if (target.Description != null)
                    that.Description = target.Description;
                if (target.SemanticId != null)
                    that.SemanticId = target.SemanticId;
                if (target.SupplementalSemanticIds != null)
                    that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                if (target.Qualifiers != null)
                    that.Qualifiers = target.Qualifiers;
                if (target.EmbeddedDataSpecifications != null)
                    that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
                if (target.ValueId != null)
                    that.ValueId = target.ValueId;
                if (target.Value != null)
                    that.Value = target.Value;
            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(MultiLanguageProperty).Name, context.GetType().Name);
            }
        }

        public override void VisitOperation(IOperation that, IClass context)
        {
            if (context is IOperation target)
            {
                if (target.InputVariables != null && target.InputVariables.Any())
                {
                    //First check if number of elements in the server's resource are greater than or equal to the request's resource
                    if (that.InputVariables.Count < target.InputVariables.Count)
                    {
                        throw new InvalidNumberOfChildElementsException("Operation : " + that.IdShort, target.InputVariables.Count, that.InputVariables.Count);
                    }
                    for (int i = 0; i < target.InputVariables.Count; i++)
                    {
                        Update.ToUpdateObject(that.InputVariables[i], target.InputVariables[i]);
                    }
                }

                if (target.OutputVariables != null && target.OutputVariables.Any())
                {
                    //First check if number of elements in the server's resource are greater than or equal to the request's resource
                    if (that.OutputVariables.Count < target.OutputVariables.Count)
                    {
                        throw new InvalidNumberOfChildElementsException("Operation : " + that.IdShort, target.OutputVariables.Count, that.OutputVariables.Count);
                    }
                    for (int i = 0; i < target.OutputVariables.Count; i++)
                    {
                        Update.ToUpdateObject(that.OutputVariables[i], target.OutputVariables[i]);
                    }
                }

                if (target.InoutputVariables != null && target.InoutputVariables.Any())
                {
                    //First check if number of elements in the server's resource are greater than or equal to the request's resource
                    if (that.InoutputVariables.Count < target.InoutputVariables.Count)
                    {
                        throw new InvalidNumberOfChildElementsException("Operation : " + that.IdShort, target.InoutputVariables.Count, that.InoutputVariables.Count);
                    }
                    for (int i = 0; i < target.InoutputVariables.Count; i++)
                    {
                        Update.ToUpdateObject(that.InoutputVariables[i], target.InoutputVariables[i]);
                    }
                }

                if (target.Extensions != null)
                    that.Extensions = target.Extensions;
                if (target.Category != null)
                    that.Category = target.Category;
                if (target.IdShort != null)
                    that.IdShort = target.IdShort;
                if (target.DisplayName != null)
                    that.DisplayName = target.DisplayName;
                if (target.Description != null)
                    that.Description = target.Description;
                if (target.SemanticId != null)
                    that.SemanticId = target.SemanticId;
                if (target.SupplementalSemanticIds != null)
                    that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                if (target.Qualifiers != null)
                    that.Qualifiers = target.Qualifiers;
                if (target.EmbeddedDataSpecifications != null)
                    that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;

            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(Operation).Name, context.GetType().Name);
            }
        }

        public override void VisitOperationVariable(IOperationVariable that, IClass context)
        {
            if (context is IOperationVariable target)
            {
                if (target.Value != null)
                {
                    Update.ToUpdateObject(that.Value, target.Value);
                }
            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(OperationVariable).Name, context.GetType().Name);
            }
        }

        public override void VisitProperty(IProperty that, IClass context)
        {
            if (context is Property target)
            {
                if (target.Extensions != null)
                    that.Extensions = target.Extensions;
                if (target.Category != null)
                    that.Category = target.Category;
                if (target.IdShort != null)
                    that.IdShort = target.IdShort;
                if (target.DisplayName != null)
                    that.DisplayName = target.DisplayName;
                if (target.Description != null)
                    that.Description = target.Description;
                if (target.SemanticId != null)
                    that.SemanticId = target.SemanticId;
                if (target.SupplementalSemanticIds != null)
                    that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                if (target.Qualifiers != null)
                    that.Qualifiers = target.Qualifiers;
                if (target.EmbeddedDataSpecifications != null)
                    that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
                that.ValueType = target.ValueType;
                if (target.Value != null)
                    that.Value = target.Value;
                if (target.ValueId != null)
                    that.ValueId = target.ValueId;
            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(Property).Name, context.GetType().Name);
            }
        }

        public override void VisitQualifier(IQualifier that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitRange(IRange that, IClass context)
        {
            if (context is IRange target)
            {
                if (target.Extensions != null)
                    that.Extensions = target.Extensions;
                if (target.Category != null)
                    that.Category = target.Category;
                if (target.IdShort != null)
                    that.IdShort = target.IdShort;
                if (target.DisplayName != null)
                    that.DisplayName = target.DisplayName;
                if (target.Description != null)
                    that.Description = target.Description;
                if (target.SemanticId != null)
                    that.SemanticId = target.SemanticId;
                if (target.SupplementalSemanticIds != null)
                    that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                if (target.Qualifiers != null)
                    that.Qualifiers = target.Qualifiers;
                if (target.EmbeddedDataSpecifications != null)
                    that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
                that.ValueType = target.ValueType;
                if (target.Min != null)
                    that.Min = target.Min;
                if (target.Max != null)
                    that.Max = target.Max;
            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(Range).Name, context.GetType().Name);
            }
        }

        public override void VisitReference(IReference that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitReferenceElement(IReferenceElement that, IClass context)
        {
            if (context is IReferenceElement target)
            {
                if (target.Extensions != null)
                    that.Extensions = target.Extensions;
                if (target.Category != null)
                    that.Category = target.Category;
                if (target.IdShort != null)
                    that.IdShort = target.IdShort;
                if (target.DisplayName != null)
                    that.DisplayName = target.DisplayName;
                if (target.Description != null)
                    that.Description = target.Description;
                if (target.SemanticId != null)
                    that.SemanticId = target.SemanticId;
                if (target.SupplementalSemanticIds != null)
                    that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                if (target.Qualifiers != null)
                    that.Qualifiers = target.Qualifiers;
                if (target.EmbeddedDataSpecifications != null)
                    that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
                if (target.Value != null)
                    that.Value = target.Value;
            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(ReferenceElement).Name, context.GetType().Name);
            }
        }

        public override void VisitRelationshipElement(IRelationshipElement that, IClass context)
        {
            if (context is IRelationshipElement target)
            {
                if (target.Extensions != null)
                    that.Extensions = target.Extensions;
                if (target.Category != null)
                    that.Category = target.Category;
                if (target.IdShort != null)
                    that.IdShort = target.IdShort;
                if (target.DisplayName != null)
                    that.DisplayName = target.DisplayName;
                if (target.Description != null)
                    that.Description = target.Description;
                if (target.SemanticId != null)
                    that.SemanticId = target.SemanticId;
                if (target.SupplementalSemanticIds != null)
                    that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                if (target.Qualifiers != null)
                    that.Qualifiers = target.Qualifiers;
                if (target.EmbeddedDataSpecifications != null)
                    that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
                if (target.First != null)
                    that.First = target.First;
                if (target.Second != null)
                    that.Second = target.Second;
            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(RelationshipElement).Name, context.GetType().Name);
            }
        }

        public override void VisitResource(IResource that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitSpecificAssetId(ISpecificAssetId that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitSubmodel(ISubmodel that, IClass context)
        {
            if (context is Submodel target)
            {
                //As per the specifications, check for smes first. If any of the resource not available, do not change the server
                if (target.SubmodelElements != null && target.SubmodelElements.Any())
                {
                    //First check if number of elements in the server's resource are greater than or equal to the request's resource
                    if (that.SubmodelElements.Count < target.SubmodelElements.Count)
                    {
                        throw new InvalidNumberOfChildElementsException("Submodel : " + that.IdShort, target.SubmodelElements.Count, that.SubmodelElements.Count);
                    }
                    for (int i = 0; i < target.SubmodelElements.Count; i++)
                    {
                        Update.ToUpdateObject(that.SubmodelElements[i], target.SubmodelElements[i]);
                    }
                }

                if (target.Extensions != null) that.Extensions = target.Extensions;
                if (target.Category != null) that.Category = target.Category;
                if (target.IdShort != null)
                    that.IdShort = target.IdShort;
                if (target.DisplayName != null)
                    that.DisplayName = target.DisplayName;
                if (target.Description != null)
                    that.Description = target.Description;
                if (target.Administration != null)
                    that.Administration = target.Administration;
                if (target.Id != null)
                    that.Id = target.Id;
                if (target.Kind != null)
                    that.Kind = target.Kind;
                if (target.SemanticId != null)
                    that.SemanticId = target.SemanticId;
                if (target.SupplementalSemanticIds != null)
                    that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                if (target.Qualifiers != null)
                    that.Qualifiers = target.Qualifiers;
                if (target.EmbeddedDataSpecifications != null)
                    that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(Submodel).Name, context.GetType().Name);
            }
        }

        public override void VisitSubmodelElementCollection(ISubmodelElementCollection that, IClass context)
        {
            if (context is ISubmodelElementCollection target)
            {
                //As per the specifications, check for smes first. If any of the resource not available, do not change the server
                if (target.Value != null && target.Value.Any())
                {
                    //First check if number of elements in the server's resource are greater than or equal to the request's resource
                    if (that.Value.Count < target.Value.Count)
                    {
                        throw new InvalidNumberOfChildElementsException("SubmodelElementCollection : " + that.IdShort, target.Value.Count, that.Value.Count);
                    }
                    for (int i = 0; i < target.Value.Count; i++)
                    {
                        Update.ToUpdateObject(that.Value[i], target.Value[i]);
                    }
                }

                if (target.Extensions != null)
                    that.Extensions = target.Extensions;
                if (target.Category != null)
                    that.Category = target.Category;
                if (target.IdShort != null)
                    that.IdShort = target.IdShort;
                if (target.DisplayName != null)
                    that.DisplayName = target.DisplayName;
                if (target.Description != null)
                    that.Description = target.Description;
                if (target.SemanticId != null)
                    that.SemanticId = target.SemanticId;
                if (target.SupplementalSemanticIds != null)
                    that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                if (target.Qualifiers != null)
                    that.Qualifiers = target.Qualifiers;
                if (target.EmbeddedDataSpecifications != null)
                    that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(SubmodelElementCollection).Name, context.GetType().Name);
            }
        }

        public override void VisitSubmodelElementList(ISubmodelElementList that, IClass context)
        {
            if (context is ISubmodelElementList target)
            {
                //As per the specifications, check for smes first. If any of the resource not available, do not change the server
                if (target.Value != null && target.Value.Any())
                {
                    //First check if number of elements in the server's resource are greater than or equal to the request's resource
                    if (that.Value.Count < target.Value.Count)
                    {
                        throw new InvalidNumberOfChildElementsException("SubmodelElementList : " + that.IdShort, target.Value.Count, that.Value.Count);
                    }
                    for (int i = 0; i < target.Value.Count; i++)
                    {
                        Update.ToUpdateObject(that.Value[i], target.Value[i]);
                    }
                }

                if (target.Extensions != null)
                    that.Extensions = target.Extensions;
                if (target.Category != null)
                    that.Category = target.Category;
                if (target.IdShort != null)
                    that.IdShort = target.IdShort;
                if (target.DisplayName != null)
                    that.DisplayName = target.DisplayName;
                if (target.Description != null)
                    that.Description = target.Description;
                if (target.SemanticId != null)
                    that.SemanticId = target.SemanticId;
                if (target.SupplementalSemanticIds != null)
                    that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                if (target.Qualifiers != null)
                    that.Qualifiers = target.Qualifiers;
                if (target.EmbeddedDataSpecifications != null)
                    that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
                that.TypeValueListElement = target.TypeValueListElement;
                if (target.OrderRelevant != null)
                    that.OrderRelevant = target.OrderRelevant;
                if (target.SemanticIdListElement != null)
                    that.SemanticIdListElement = target.SemanticIdListElement;
                that.ValueTypeListElement = target.ValueTypeListElement;

            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(SubmodelElementList).Name, context.GetType().Name);
            }
        }

        public override void VisitValueList(IValueList that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitValueReferencePair(IValueReferencePair that, IClass context)
        {
            throw new System.NotImplementedException();
        }
    }
}
