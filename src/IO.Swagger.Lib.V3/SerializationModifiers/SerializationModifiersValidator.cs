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


using DataTransferObjects.MetadataDTOs;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Models;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.SerializationModifiers
{
    public static class SerializationModifiersValidator
    {
        //As per new APIs, content is not handled here
        public static void Validate(object? resource, LevelEnum level, ExtentEnum extent)
        {
            switch (resource)
            {
                case BasicEventElementMetadata:
                case BasicEventElementValue:
                case BasicEventElement:
                case Capability:
                case Operation:
                    {
                        if (level == LevelEnum.Core)
                        {
                            throw new InvalidSerializationModifierException(level.ToString(), resource.GetType().Name);
                        }

                        if (extent == ExtentEnum.WithBlobValue)
                        {
                            throw new InvalidSerializationModifierException(extent.ToString(), resource.GetType().Name);
                        }
                        break;
                    }
                case BlobMetadata:
                case BlobValue:
                case Blob:
                    {
                        if (level == LevelEnum.Core)
                        {
                            throw new InvalidSerializationModifierException(level.ToString(), resource.GetType().Name);
                        }
                        break;
                    }
                case SubmodelElementListMetadata:
                case SubmodelElementListValue:
                case SubmodelElementCollectionMetadata:
                case SubmodelElementCollectionValue:
                case AnnotatedRelationshipElementMetadata:
                case AnnotatedRelationshipElementValue:
                case EntityMetadata:
                case EntityValue:
                case OperationMetadata:
                case OperationValue:
                    {
                        break;
                    }
                case ISubmodelElementMetadata:
                case ISubmodelElementValue:
                case IDataElement:
                    {
                        if (level == LevelEnum.Core)
                        {
                            throw new InvalidSerializationModifierException(level.ToString(), resource.GetType().Name);
                        }
                        if (extent == ExtentEnum.WithBlobValue)
                        {
                            throw new InvalidSerializationModifierException(extent.ToString(), resource.GetType().Name);
                        }
                        break;
                    }
            }
        }

        public static void Validate(List<IClass?> resources, LevelEnum level, ExtentEnum extent)
        {
            foreach (var resource in resources)
                Validate(resource, level, extent);
        }
    }
}
