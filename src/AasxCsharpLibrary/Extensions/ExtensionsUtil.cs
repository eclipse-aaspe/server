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

using AasxCompatibilityModels;
using AdminShellNS.Extensions;
using System;
using System.Collections.Generic;

namespace Extensions;

public static class ExtensionsUtil
{
    public static Reference? ConvertReferenceFromV10(AdminShellV10.Reference sourceReference, ReferenceTypes referenceTypes)
    {
        Reference? outputReference = null;
        if (sourceReference != null && !sourceReference.IsEmpty)
        {
            var keyList = new List<IKey>();
            foreach (var refKey in sourceReference.Keys)
            {
                var keyType = Stringification.KeyTypesFromString(refKey.type);
                if (keyType != null)
                {
                    keyList.Add(new Key((KeyTypes) keyType, refKey.value));
                }
                else
                {
                    Console.WriteLine($"KeyType value {refKey.type} not found.");
                }
            }

            outputReference = new Reference(referenceTypes, keyList);
        }

        return outputReference;
    }

    public static Reference? ConvertReferenceFromV20(AdminShellV20.Reference sourceReference, ReferenceTypes referenceTypes)
    {
        Reference? outputReference = null;
        if (sourceReference != null && !sourceReference.IsEmpty)
        {
            var keyList = new List<IKey>();
            foreach (var refKey in sourceReference.Keys)
            {
                // Fix, as Asset does not exist anymore
                if (refKey.type?.Trim().Equals("Asset", StringComparison.InvariantCultureIgnoreCase) == true)
                    refKey.type = "GlobalReference";

                var keyType = Stringification.KeyTypesFromString(refKey.type);
                if (keyType != null)
                {
                    keyList.Add(new Key((KeyTypes) keyType, refKey.value));
                }
                else
                {
                    Console.WriteLine($"KeyType value {refKey.type} not found.");
                }
            }

            outputReference = new Reference(referenceTypes, keyList);
        }

        return outputReference;
    }

    internal static List<ILangStringTextType> ConvertDescriptionFromV10(AdminShellV10.Description sourceDescription)
    {
        if (!sourceDescription.langString.IsNullOrEmpty())
        {
            var newLangStrList = new List<ILangStringTextType>();
            foreach (var ls in sourceDescription.langString)
            {
                newLangStrList.Add(new LangStringTextType(ls.lang, ls.str));
            }

            return new List<ILangStringTextType>(newLangStrList);
        }

        return null;
    }

    internal static List<ILangStringTextType> ConvertDescriptionFromV20(AdminShellV20.Description sourceDescription)
    {
        if (!sourceDescription.langString.IsNullOrEmpty())
        {
            var newLangStrList = new List<ILangStringTextType>();
            foreach (var ls in sourceDescription.langString)
            {
                newLangStrList.Add(new LangStringTextType(ls.lang, ls.str));
            }

            return new List<ILangStringTextType>(newLangStrList);
        }

        return null;
    }

    internal static KeyTypes GetKeyType(IClass aasElement) =>
        aasElement switch
        {
            AssetAdministrationShell     => KeyTypes.AssetAdministrationShell,
            Submodel                     => KeyTypes.Submodel,
            ConceptDescription           => KeyTypes.ConceptDescription,
            SubmodelElementCollection    => KeyTypes.SubmodelElementCollection,
            SubmodelElementList          => KeyTypes.SubmodelElementList,
            BasicEventElement            => KeyTypes.BasicEventElement,
            Blob                         => KeyTypes.Blob,
            Entity                       => KeyTypes.Entity,
            File                         => KeyTypes.File,
            MultiLanguageProperty        => KeyTypes.MultiLanguageProperty,
            Property                     => KeyTypes.Property,
            Operation                    => KeyTypes.Operation,
            AasCore.Aas3_0.Range         => KeyTypes.Range,
            ReferenceElement             => KeyTypes.ReferenceElement,
            RelationshipElement          => KeyTypes.RelationshipElement,
            AnnotatedRelationshipElement => KeyTypes.AnnotatedRelationshipElement,
            IIdentifiable                => KeyTypes.Identifiable,
            IReferable                   => KeyTypes.Referable,
            Reference                    => KeyTypes.GlobalReference, // TODO (jtikekar, 2023-09-04): what about model reference
            _                            => KeyTypes.SubmodelElement  // default case
        };
}