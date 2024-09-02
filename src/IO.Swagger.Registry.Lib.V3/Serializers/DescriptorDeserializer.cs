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

using IO.Swagger.Registry.Lib.V3.Models;
using System.Text.Json.Nodes;

namespace IO.Swagger.Registry.Lib.V3.Serializers
{
    public static class DescriptorDeserializer
    {
        public static AssetAdministrationShellDescriptor AssetAdministrationShellDescriptorFrom(JsonNode node)
        {
            AssetAdministrationShellDescriptor? result = DescriptorDeserializeImplementation.AssetAdministrationShellDescriptorFrom(
                node,
                out Reporting.Error? error);
            if (error != null)
            {
                throw new Jsonization.Exception(
                    Reporting.GenerateJsonPath(error.PathSegments),
                    error.Cause);
            }

            return result
                   ?? throw new System.InvalidOperationException(
                       "Unexpected output null when error is null");
        }

        public static SubmodelDescriptor SubmodelDescriptorFrom(JsonNode node)
        {
            SubmodelDescriptor? result = DescriptorDeserializeImplementation.SubmodelDescriptorFrom(
                node,
                out Reporting.Error? error);
            if (error != null)
            {
                throw new Jsonization.Exception(
                    Reporting.GenerateJsonPath(error.PathSegments),
                    error.Cause);
            }

            return result
                   ?? throw new System.InvalidOperationException(
                       "Unexpected output null when error is null");
        }
    }
}