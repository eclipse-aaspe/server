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

using IO.Swagger.Lib.V3.Exceptions;
using System.Collections.Generic;
using static AasCore.Aas3_0.Visitation;

namespace IO.Swagger.Lib.V3.SerializationModifiers.PathModifier;

/// <inheritdoc />
public class PathTransformer : ITransformerWithContext<PathModifierContext, List<string>>
{
    /// <inheritdoc />
    public List<string> Transform(IClass that, PathModifierContext context) => that.Transform(this, context);

    /// <inheritdoc />
    public List<string> TransformAdministrativeInformation(IAdministrativeInformation that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformAssetAdministrationShell(IAssetAdministrationShell that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformAssetInformation(IAssetInformation that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformConceptDescription(IConceptDescription that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformDataSpecificationIec61360(IDataSpecificationIec61360 that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformEmbeddedDataSpecification(IEmbeddedDataSpecification that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformEnvironment(IEnvironment that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformEventPayload(IEventPayload that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformExtension(IExtension that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformKey(IKey that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformLangStringDefinitionTypeIec61360(ILangStringDefinitionTypeIec61360 that, PathModifierContext context) =>
        throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformLangStringNameType(ILangStringNameType that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformLangStringPreferredNameTypeIec61360(ILangStringPreferredNameTypeIec61360 that, PathModifierContext context) =>
        throw new System.NotImplementedException();


    /// <inheritdoc />
    public List<string> TransformLangStringShortNameTypeIec61360(ILangStringShortNameTypeIec61360 that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformLangStringTextType(ILangStringTextType that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformLevelType(ILevelType that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformQualifier(IQualifier that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformReference(IReference that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformResource(IResource that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformSpecificAssetId(ISpecificAssetId that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformValueList(IValueList that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformValueReferencePair(IValueReferencePair that, PathModifierContext context) => throw new System.NotImplementedException();

    /// <inheritdoc />
    public List<string> TransformAnnotatedRelationshipElement(IAnnotatedRelationshipElement that, PathModifierContext context) =>
        TransformWithChildren(context, that.IdShort, that.Annotations);

    /// <inheritdoc />
    public List<string> TransformBasicEventElement(IBasicEventElement that, PathModifierContext context) => TransformWithPath(that, context, that.IdShort);

    /// <inheritdoc />
    public List<string> TransformBlob(IBlob that, PathModifierContext context) => TransformWithPath(that, context, that.IdShort);

    /// <inheritdoc />
    public List<string> TransformCapability(ICapability that, PathModifierContext context) => TransformWithPath(that, context, that.IdShort);

    /// <inheritdoc />
    public List<string> TransformEntity(IEntity that, PathModifierContext context) => TransformWithChildren(context, that.IdShort, that.Statements);

    /// <inheritdoc />
    public List<string> TransformFile(IFile that, PathModifierContext context) => TransformWithPath(that, context, that.IdShort);

    /// <inheritdoc />
    public List<string> TransformMultiLanguageProperty(IMultiLanguageProperty that, PathModifierContext context) => TransformWithPath(that, context, that.IdShort);

    /// <inheritdoc />
    public List<string> TransformOperation(IOperation that, PathModifierContext context)
    {
        if (that.IdShort != null)
        {
            context.IdShortPaths?.Add(that.IdShort);
        }

        TransformOperationVariables(that.InputVariables, context, that.IdShort);
        TransformOperationVariables(that.OutputVariables, context, that.IdShort);
        TransformOperationVariables(that.InoutputVariables, context, that.IdShort);

        return context.IdShortPaths ?? [];
    }

    /// <inheritdoc />
    public List<string> TransformOperationVariable(IOperationVariable that, PathModifierContext context) => TransformWithPath(that, context, that.Value.IdShort);

    /// <inheritdoc />
    public List<string> TransformProperty(IProperty that, PathModifierContext context) => TransformWithPath(that, context, that.IdShort);

    /// <inheritdoc />
    public List<string> TransformRange(IRange that, PathModifierContext context) => TransformWithPath(that, context, that.IdShort);

    /// <inheritdoc />
    public List<string> TransformReferenceElement(IReferenceElement that, PathModifierContext context) => TransformWithPath(that, context, that.IdShort);

    /// <inheritdoc />
    public List<string> TransformRelationshipElement(IRelationshipElement that, PathModifierContext context) => TransformWithPath(that, context, that.IdShort);

    /// <inheritdoc />
    public List<string> TransformSubmodel(ISubmodel? that, PathModifierContext context)
    {
        context.IdShortPaths?.Add(that?.IdShort ?? string.Empty);

        if (that?.SubmodelElements == null)
        {
            return context.IdShortPaths ?? [];
        }

        foreach (var element in that.SubmodelElements)
        {
            context.ParentPath = that.IdShort ?? string.Empty;
            Transform(element, context);
        }

        return context.IdShortPaths ?? [];
    }

    /// <inheritdoc />
    public List<string> TransformSubmodelElementCollection(ISubmodelElementCollection that, PathModifierContext context) =>
        TransformWithChildren(context, that.IdShort, that.Value);

    /// <inheritdoc />
    public List<string> TransformSubmodelElementList(ISubmodelElementList that, PathModifierContext context)
    {
        if (that.Value == null)
        {
            return context.IdShortPaths ?? [];
        }

        for (var i = 0; i < that.Value.Count; i++)
        {
            context.IdShortPaths?.Add(string.IsNullOrEmpty(context.ParentPath) ? $"{that.IdShort}[{i}]" : $"{context.ParentPath}.{that.IdShort}[{i}]");
        }

        return context.IdShortPaths ?? [];
    }

    private List<string> TransformWithChildren(PathModifierContext context, string? idShort, IEnumerable<IClass>? children)
    {
        if (context.IdShortPaths == null)
        {
            return [string.Empty];
        }

        if (context.IdShortPaths.Count == 0)
        {
            context.IdShortPaths.Add(idShort ?? string.Empty);
        }
        else
        {
            context.IdShortPaths.Add($"{context.ParentPath}.{idShort}");
        }

        if (children == null)
        {
            return context.IdShortPaths;
        }

        var currentParentPath = string.IsNullOrEmpty(context.ParentPath) ? idShort : $"{context.ParentPath}.{idShort}";
        foreach (var item in children)
        {
            context.ParentPath = currentParentPath;
            Transform(item, context);
        }

        return context.IdShortPaths;
    }

    private static List<string> TransformWithPath<T>(T that, PathModifierContext context, string? idShort)
    {
        if (context.ParentPath == null)
        {
            throw new InvalidSerializationModifierException("Path", that.GetType().Name);
        }

        context.IdShortPaths?.Add($"{context.ParentPath}.{idShort}");
        return context.IdShortPaths ?? [];
    }

    private void TransformOperationVariables(IEnumerable<IOperationVariable>? variables, PathModifierContext context, string? parentPath)
    {
        if (variables == null)
        {
            return;
        }

        foreach (var element in variables)
        {
            context.ParentPath = parentPath;
            Transform(element, context);
        }
    }
}