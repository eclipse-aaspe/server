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

using IO.Swagger.Lib.V3.Exceptions;
using System.Collections.Generic;
using static AasCore.Aas3_0.Visitation;

namespace IO.Swagger.Lib.V3.SerializationModifiers.PathModifier
{
    public class PathTransformer : ITransformerWithContext<PathModifierContext, List<string>>
    {
        public List<string> Transform(IClass? that, PathModifierContext context)
        {
            return that.Transform(this, context);
        }

        public List<string> TransformAdministrativeInformation(IAdministrativeInformation that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformAnnotatedRelationshipElement(IAnnotatedRelationshipElement that, PathModifierContext context)
        {
            if (context.IsRoot && !context.IsGetAllSmes)
                throw new InvalidSerializationModifierException("Path", that.GetType().Name);
            if (string.IsNullOrEmpty(context.ParentPath))
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add($"{context.ParentPath}.{that.IdShort}");
            }
            return context.IdShortPaths;
        }

        public List<string> TransformAssetAdministrationShell(IAssetAdministrationShell that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformAssetInformation(IAssetInformation that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformBasicEventElement(IBasicEventElement that, PathModifierContext context)
        {
            if (context.IsRoot)
                throw new InvalidSerializationModifierException("Path", that.GetType().Name);
            if (string.IsNullOrEmpty(context.ParentPath))
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add($"{context.ParentPath}.{that.IdShort}");
            }
            return context.IdShortPaths;
        }

        public List<string> TransformBlob(IBlob that, PathModifierContext context)
        {
            if (context.IsRoot)
                throw new InvalidSerializationModifierException("Path", that.GetType().Name);
            if (string.IsNullOrEmpty(context.ParentPath))
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add($"{context.ParentPath}.{that.IdShort}");
            }
            return context.IdShortPaths;
        }

        public List<string> TransformCapability(ICapability that, PathModifierContext context)
        {
            if (context.IsRoot)
                throw new InvalidSerializationModifierException("Path", that.GetType().Name);
            if (string.IsNullOrEmpty(context.ParentPath))
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add($"{context.ParentPath}.{that.IdShort}");
            }
            return context.IdShortPaths;
        }

        public List<string> TransformConceptDescription(IConceptDescription that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformDataSpecificationIec61360(IDataSpecificationIec61360 that, PathModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> TransformEmbeddedDataSpecification(IEmbeddedDataSpecification that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformEntity(IEntity that, PathModifierContext context)
        {
            if (context.IdShortPaths.Count == 0 || string.IsNullOrEmpty(context.ParentPath))
            {
                context.IdShortPaths.Add(that.IdShort ?? string.Empty);
            }
            else
            {
                context.IdShortPaths.Add($"{context.ParentPath}.{that.IdShort}");
            }

            if (that.Statements != null)
            {
                var currentParentPath = string.IsNullOrEmpty(context.ParentPath) ? that.IdShort : $"{context.ParentPath}.{that.IdShort}";
                foreach (var item in that.Statements)
                {
                    context.IsRoot = false;
                    context.ParentPath = currentParentPath;
                    Transform(item, context);
                }
            }

            return context.IdShortPaths;
        }

        public List<string> TransformEnvironment(IEnvironment that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformEventPayload(IEventPayload that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformExtension(IExtension that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformFile(IFile that, PathModifierContext context)
        {
            if (context.IsRoot)
                throw new InvalidSerializationModifierException("Path", that.GetType().Name);
            if (string.IsNullOrEmpty(context.ParentPath))
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add($"{context.ParentPath}.{that.IdShort}");
            }
            return context.IdShortPaths;
        }

        public List<string> TransformKey(IKey that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformLangStringDefinitionTypeIec61360(ILangStringDefinitionTypeIec61360 that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformLangStringNameType(ILangStringNameType that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformLangStringPreferredNameTypeIec61360(ILangStringPreferredNameTypeIec61360 that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformLangStringShortNameTypeIec61360(ILangStringShortNameTypeIec61360 that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformLangStringTextType(ILangStringTextType that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformLevelType(ILevelType that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformMultiLanguageProperty(IMultiLanguageProperty that, PathModifierContext context)
        {
            if (context.IsRoot)
                throw new InvalidSerializationModifierException("Path", that.GetType().Name);
            if (string.IsNullOrEmpty(context.ParentPath))
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add($"{context.ParentPath}.{that.IdShort}");
            }
            return context.IdShortPaths;
        }

        public List<string> TransformOperation(IOperation that, PathModifierContext context)
        {
            if (context.IsRoot && !context.IsGetAllSmes)
                throw new InvalidSerializationModifierException("Path", that.GetType().Name);
            if (string.IsNullOrEmpty(context.ParentPath))
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add($"{context.ParentPath}.{that.IdShort}");
            }
            return context.IdShortPaths;
        }

        public List<string> TransformOperationVariable(IOperationVariable that, PathModifierContext context)
        {
            if (context.IsRoot)
                throw new InvalidSerializationModifierException("Path", that.GetType().Name);
            if (string.IsNullOrEmpty(context.ParentPath))
            {
                context.IdShortPaths.Add(that.Value.IdShort);
            }
            else
            {
                context.IdShortPaths.Add($"{context.ParentPath}.{that.Value.IdShort}");
            }
            return context.IdShortPaths;
        }

        public List<string> TransformProperty(IProperty that, PathModifierContext context)
        {
            if (context.IsRoot && !context.IsGetAllSmes)
                throw new InvalidSerializationModifierException("Path", that.GetType().Name);
            if (string.IsNullOrEmpty(context.ParentPath))
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add($"{context.ParentPath}.{that.IdShort}");
            }
            return context.IdShortPaths;
        }

        public List<string> TransformQualifier(IQualifier that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformRange(IRange that, PathModifierContext context)
        {
            if (context.IsRoot)
                throw new InvalidSerializationModifierException("Path", that.GetType().Name);
            if (string.IsNullOrEmpty(context.ParentPath))
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add($"{context.ParentPath}.{that.IdShort}");
            }
            return context.IdShortPaths;
        }

        public List<string> TransformReference(IReference that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformReferenceElement(IReferenceElement that, PathModifierContext context)
        {
            if (context.IsRoot)
                throw new InvalidSerializationModifierException("Path", that.GetType().Name);
            if (string.IsNullOrEmpty(context.ParentPath))
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add($"{context.ParentPath}.{that.IdShort}");
            }
            return context.IdShortPaths;
        }

        public List<string> TransformRelationshipElement(IRelationshipElement that, PathModifierContext context)
        {
            if (context.IsRoot)
                throw new InvalidSerializationModifierException("Path", that.GetType().Name);
            if (string.IsNullOrEmpty(context.ParentPath))
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add($"{context.ParentPath}.{that.IdShort}");
            }
            return context.IdShortPaths;
        }

        public List<string> TransformResource(IResource that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformSpecificAssetId(ISpecificAssetId that, PathModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> TransformSubmodel(ISubmodel? that, PathModifierContext context)
        {
            if (that.SubmodelElements != null)
            {
                foreach (var element in that.SubmodelElements)
                {
                    context.IsRoot = false;
                    Transform(element, context);
                }
            }

            return context.IdShortPaths;
        }

        public List<string> TransformSubmodelElementCollection(ISubmodelElementCollection that, PathModifierContext context)
        {
            if (context.IdShortPaths.Count == 0 || string.IsNullOrEmpty(context.ParentPath))
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add($"{context.ParentPath}.{that.IdShort}");
            }

            if (that.Value != null)
            {
                var currentParentPath = string.IsNullOrEmpty(context.ParentPath) ? that.IdShort : $"{context.ParentPath}.{that.IdShort}";
                foreach (var item in that.Value)
                {
                    context.IsRoot = false ;
                    context.ParentPath = currentParentPath;
                    Transform(item, context);
                }
            }

            return context.IdShortPaths;
        }

        public List<string> TransformSubmodelElementList(ISubmodelElementList that, PathModifierContext context)
        {
            if (context.IdShortPaths.Count == 0 || string.IsNullOrEmpty(context.ParentPath))
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add($"{context.ParentPath}.{that.IdShort}");
            }

            if (that.Value != null)
            {
                for (var i = 0; i < that.Value.Count; i++)
                {
                    if (string.IsNullOrEmpty(context.ParentPath))
                    {
                        //No need of prefix
                        context.IdShortPaths.Add(that.IdShort + $"[{i}]");
                    }
                    else
                    {
                        context.IdShortPaths.Add($"{context.ParentPath}.{that.IdShort}" + $"[{i}]");
                    }
                }
            }

            return context.IdShortPaths;
        }

        public List<string> TransformValueList(IValueList that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        public List<string> TransformValueReferencePair(IValueReferencePair that, PathModifierContext context)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }
    }
}
